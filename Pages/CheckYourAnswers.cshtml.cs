using System.Text.Json;
using GovUk.Frontend.AspNetCore;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Unified.Intake.Pages
{
    public class CheckYourAnswersModel : PageModel
    {
        private static readonly Dictionary<string, string> ServiceNames = new()
        {
            ["find-information-about-schools-and-trusts"] = "Find Information about Schools and Trusts"
        };

        // camelCase to match the field names in docs/power-automate-request.json.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly ILogger<CheckYourAnswersModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IVirusScanner _scanner;
        private readonly string? _powerAutomateUrl;

        public CheckYourAnswersModel(
            ILogger<CheckYourAnswersModel> logger,
            IHttpClientFactory httpClientFactory,
            IVirusScanner scanner,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _scanner = scanner;
            _powerAutomateUrl = configuration["PowerAutomateUrl"];
            IsScanningEnabled = configuration.GetValue("ClamAv:IsEnabled", false);
            PollIntervalSeconds = configuration.GetValue("ClamAv:PollIntervalSeconds", 5);
            MaxPollAttempts = configuration.GetValue("ClamAv:MaxPollAttempts", 60);
        }

        // Feature flag (ClamAv:IsEnabled). When false, virus scanning is skipped entirely: no scan is run
        // on submission and the status widget is not shown. Defaults to false.
        public bool IsScanningEnabled { get; }

        // How often the browser should poll each file's scan status, in seconds. Surfaced to the view so
        // the client-side polling loop and the server share a single configured value.
        public int PollIntervalSeconds { get; }

        // How many times to poll before giving up on a scan. Shared by the client and the server fallback.
        public int MaxPollAttempts { get; }

        // Populated by JavaScript once every file has been scanned clean: a comma-separated list of the
        // ClamAV job ids. The final POST re-verifies these server-side before anything is sent on, so a
        // tampered or absent value simply triggers an authoritative re-scan rather than bypassing the gate.
        [BindProperty]
        public string? ScanJobIds { get; set; }

        public string? Service { get; private set; }
        public string? RequestType { get; private set; }
        public string? FullName { get; private set; }
        public string? EmailAddress { get; private set; }
        public string? RequestDetails { get; private set; }
        public string? SupportingInformationFileName { get; private set; }

        // The uploaded documents, used to render one virus-scan status row per file above the submit button.
        public IReadOnlyList<SupportingDocument> SupportingDocumentsList { get; private set; } =
            Array.Empty<SupportingDocument>();

        public string? CanContact { get; private set; }

        public void OnGet()
        {
            PopulateAnswers();
        }

        private void PopulateAnswers()
        {
            Service = FormatServiceValue(Session.GetTellUsWhatYouNeedService(HttpContext.Session));
            RequestType = FormatValue(Session.GetTellUsWhatYouNeed(HttpContext.Session));
            FullName = Session.GetAboutYouFullName(HttpContext.Session);
            EmailAddress = Session.GetAboutYouEmailAddress(HttpContext.Session);
            RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session);

            var documents = SupportingDocuments.GetAll(HttpContext.Session);
            SupportingDocumentsList = documents;
            SupportingInformationFileName = documents.Count > 0
                ? string.Join(", ", documents.Select(d => d.FileName))
                : null;

            CanContact = FormatValue(Session.GetAboutYouCanContact(HttpContext.Session));
        }

        // AJAX handler: submits every supporting document held in the session to ClamAV for asynchronous
        // scanning and returns the resulting job ids for the browser to poll. The files are read from the
        // server-side session, never re-uploaded by the browser, so the bytes that get scanned are exactly
        // the bytes that will be submitted.
        public async Task<IActionResult> OnPostStartScanAsync(CancellationToken cancellationToken)
        {
            if (!IsScanningEnabled)
                return NotFound();

            var documents = SupportingDocuments.GetAll(HttpContext.Session);

            var files = new List<object>();
            foreach (var document in documents)
            {
                await using var contents = SupportingDocuments.OpenRead(HttpContext.Session, document);
                var jobId = await _scanner.SubmitAsync(
                    document.FileName, document.ContentType, contents, cancellationToken);
                files.Add(new { storedName = document.StoredFileName, fileName = document.FileName, jobId });
            }

            return new JsonResult(new { files });
        }

        // AJAX handler: reports the current status of a single scan job so the browser can update its
        // progress display. Status values match the API (queued, downloading, scanning, clean, infected,
        // error).
        public async Task<IActionResult> OnGetScanStatusAsync(string jobId, CancellationToken cancellationToken)
        {
            if (!IsScanningEnabled)
                return NotFound();

            if (string.IsNullOrWhiteSpace(jobId))
                return BadRequest();

            var result = await _scanner.GetStatusAsync(jobId, cancellationToken);
            return new JsonResult(new
            {
                jobId,
                status = result.State.ToString().ToLowerInvariant(),
                malware = result.Malware
            });
        }

        public async Task<IActionResult> OnPost(CancellationToken cancellationToken = default)
        {
            var documents = SupportingDocuments.GetAll(HttpContext.Session);

            // Virus scanning is behind the ClamAv:IsEnabled feature flag; when off it is skipped entirely.
            if (IsScanningEnabled)
            {
                var blocked = await RunVirusGateAsync(documents, cancellationToken);
                if (blocked is not null)
                    return blocked;
            }

            var request = await BuildSubmissionRequestAsync(documents);
            LogSubmissionPayload(request);

            return await SubmitToPowerAutomateAsync(request);
        }

        private async Task<IActionResult?> RunVirusGateAsync(
            IReadOnlyList<SupportingDocument> documents, CancellationToken cancellationToken)
        {
            if (documents.Count == 0)
                return null;

            var gate = await EnsureFilesAreCleanAsync(documents, cancellationToken);
            if (gate == ScanGate.Clean)
                return null;

            _logger.LogWarning("Submission blocked by virus gate: {Gate}.", gate);
            return SubmissionError(GateMessage(gate));
        }

        // Builds the payload submitted to the backend from the current session. Shape mirrors
        // docs/power-automate-request.json; each supporting document is read and base64-encoded inline.
        private async Task<SubmissionRequest> BuildSubmissionRequestAsync(
            IReadOnlyList<SupportingDocument> documents)
        {
            var attachments = new List<SubmissionAttachment>();
            foreach (var document in documents)
            {
                await using var contents = SupportingDocuments.OpenRead(HttpContext.Session, document);
                using var buffer = new MemoryStream();
                await contents.CopyToAsync(buffer);
                attachments.Add(new SubmissionAttachment(
                    document.FileName,
                    document.ContentType,
                    Convert.ToBase64String(buffer.ToArray())));
            }

            return new SubmissionRequest
            {
                RequestType = FormatValue(Session.GetTellUsWhatYouNeed(HttpContext.Session)),
                Service = FormatServiceValue(Session.GetTellUsWhatYouNeedService(HttpContext.Session)),
                SubmittedBy = new SubmittedBy(
                    Session.GetAboutYouFullName(HttpContext.Session),
                    Session.GetAboutYouEmailAddress(HttpContext.Session)),
                RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session),
                ContactPermission = ToYesNo(ParseCanContact(Session.GetAboutYouCanContact(HttpContext.Session))),
                Attachments = attachments
            };
        }

        // Logs the payload for diagnostics. Attachment content (base64) is summarised to keep the log readable.
        private void LogSubmissionPayload(SubmissionRequest request)
        {
            _logger.LogInformation(
                "Submission request payload: {Payload}",
                JsonSerializer.Serialize(
                    new
                    {
                        request.RequestType,
                        request.Service,
                        request.SubmittedBy,
                        request.RequestDetails,
                        request.ContactPermission,
                        Attachments = request.Attachments
                            .Select(a => new { a.FileName, a.ContentType, ContentBytes = a.Content.Length })
                    },
                    SerializerOptions));
        }

        // Submits the request to Power Automate; the reference number comes back in the response (see
        // docs/power-automate-success-response.json). Transport-level failures (network error, non-success
        // status code, malformed response) and application failures are both surfaced as an error page
        // rather than bubbling up to the error page. On success the reference number is stored and the user
        // is redirected to the confirmation page.
        private async Task<IActionResult> SubmitToPowerAutomateAsync(SubmissionRequest request)
        {
            if (string.IsNullOrWhiteSpace(_powerAutomateUrl))
                throw new InvalidOperationException("PowerAutomateUrl is not configured.");

            SubmissionResponse? result;
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsJsonAsync(_powerAutomateUrl, request, SerializerOptions);
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadFromJsonAsync<SubmissionResponse>(SerializerOptions);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Submission to Power Automate failed to reach the server.");
                return SubmissionError("There was a problem submitting your request. Please try again.");
            }

            if (result is not { Success: true } || result.ReferenceNumber <= 0)
            {
                _logger.LogError("Submission to Power Automate failed: {Message}", result?.Message);
                return SubmissionError(
                    result?.Message ?? "There was a problem submitting your request. Please try again.");
            }

            Session.SetReferenceNumber(HttpContext.Session, result.ReferenceNumber.ToString());
            return RedirectToPage(Links.RequestSubmitted.PageName);
        }

        private enum ScanGate { Clean, Infected, Failed }

        // Confirms every supporting document is virus-free before submission. Prefers the fast path of
        // re-checking the job ids the browser already scanned; if that cannot be trusted (no ids, wrong
        // count, still pending, or an API error) it falls back to scanning the files from scratch here.
        private async Task<ScanGate> EnsureFilesAreCleanAsync(
            IReadOnlyList<SupportingDocument> documents, CancellationToken cancellationToken)
        {
            var jobIds = (ScanJobIds ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

            if (jobIds.Count >= documents.Count)
            {
                var results = await Task.WhenAll(
                    jobIds.Select(id => _scanner.GetStatusAsync(id, cancellationToken)));

                // A clean fast path is conclusive; an infected one blocks immediately. Anything else
                // (still pending, or an API error) drops through to an authoritative re-scan below.
                if (results.Any(r => r.State == ScanState.Infected))
                    return ScanGate.Infected;
                if (results.All(r => r.State == ScanState.Clean))
                    return ScanGate.Clean;
            }

            return await RescanAsync(documents, cancellationToken);
        }

        // Submits every document afresh and polls until all are clean, one is infected, or we give up.
        // This is the authoritative gate used when the client did not supply a trustworthy set of clean
        // job ids (e.g. JavaScript disabled, or the earlier scan was tampered with).
        private async Task<ScanGate> RescanAsync(
            IReadOnlyList<SupportingDocument> documents, CancellationToken cancellationToken)
        {
            var jobIds = new List<string>();
            foreach (var document in documents)
            {
                await using var contents = SupportingDocuments.OpenRead(HttpContext.Session, document);
                jobIds.Add(await _scanner.SubmitAsync(
                    document.FileName, document.ContentType, contents, cancellationToken));
            }

            for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
            {
                var results = await Task.WhenAll(
                    jobIds.Select(id => _scanner.GetStatusAsync(id, cancellationToken)));

                if (results.Any(r => r.State == ScanState.Infected))
                    return ScanGate.Infected;
                if (results.All(r => r.State == ScanState.Clean))
                    return ScanGate.Clean;
                if (results.Any(r => r.State == ScanState.Error))
                    return ScanGate.Failed; // a hard error — no point waiting any longer

                // Otherwise at least one file is still pending; wait and poll again.
                await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), cancellationToken);
            }

            return ScanGate.Failed; // timed out waiting for a verdict
        }

        private static string GateMessage(ScanGate gate) => gate switch
        {
            ScanGate.Infected =>
                "One or more of your files failed our virus check and cannot be submitted. " +
                "Remove the affected file on the previous page and try again.",
            _ =>
                "We could not confirm your files are safe to submit. Please try again."
        };

        // Re-renders Check your answers with an error summary at the top so the user can try again.
        // The GOV.UK error summary reads from the page error context (populated by field tag helpers
        // or AddPageError) rather than ModelState, so a plain ModelState error would not appear here —
        // this page has no bound field to surface it. href is null: the error is not tied to a field.
        private IActionResult SubmissionError(string message)
        {
            HttpContext.AddPageError(message, href: null);
            PopulateAnswers();
            return Page();
        }

        // The CanContact radio stores "yes"/"no"; parse it to a bool for internal use.
        private static bool ParseCanContact(string? value) =>
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

        // The backend expects contactPermission as a "yes"/"no" string, not a boolean.
        private static string ToYesNo(bool value) => value ? "Yes" : "No";

        private static string? FormatServiceValue(string? value) =>
            value is null ? null :
            ServiceNames.TryGetValue(value, out var name) ? name : FormatValue(value);

        private static string? FormatValue(string? value) =>
            string.IsNullOrEmpty(value) ? null :
            char.ToUpper(value[0]) + value[1..].Replace('-', ' ');
    }
}
