using System.Net.Http.Json;
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
        private readonly string? _powerAutomateUrl;

        public CheckYourAnswersModel(
            ILogger<CheckYourAnswersModel> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _powerAutomateUrl = configuration["PowerAutomateUrl"];
        }

        public string? Service { get; private set; }
        public string? RequestType { get; private set; }
        public string? FullName { get; private set; }
        public string? EmailAddress { get; private set; }
        public string? RequestDetails { get; private set; }
        public string? SupportingInformationFileName { get; private set; }
        public string? CanContact { get; private set; }

        public void OnGet()
        {
            PopulateAnswers();
        }

        // Populates the display properties shown in the summary lists from session. Called on GET and
        // again when re-rendering the page after a failed submission.
        private void PopulateAnswers()
        {
            Service = FormatServiceValue(Session.GetTellUsWhatYouNeedService(HttpContext.Session));
            RequestType = FormatValue(Session.GetTellUsWhatYouNeed(HttpContext.Session));
            FullName = Session.GetAboutYouFullName(HttpContext.Session);
            EmailAddress = Session.GetAboutYouEmailAddress(HttpContext.Session);
            RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session);

            var documents = SupportingDocuments.GetAll(HttpContext.Session);
            SupportingInformationFileName = documents.Count > 0
                ? string.Join(", ", documents.Select(d => d.FileName))
                : null;

            CanContact = FormatValue(Session.GetAboutYouCanContact(HttpContext.Session));
        }

        public async Task<IActionResult> OnPost()
        {
            var documents = SupportingDocuments.GetAll(HttpContext.Session);

            // Read and base64-encode each supporting document so it can be embedded in the payload.
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

            // Build the payload that will be submitted to the backend. Shape mirrors docs/power-automate-request.json.
            var request = new SubmissionRequest
            {
                // RequestType = "",
                RequestType = FormatValue(Session.GetTellUsWhatYouNeed(HttpContext.Session)),
                Service = FormatServiceValue(Session.GetTellUsWhatYouNeedService(HttpContext.Session)),
                SubmittedBy = new SubmittedBy(
                    Session.GetAboutYouFullName(HttpContext.Session),
                    Session.GetAboutYouEmailAddress(HttpContext.Session)),
                RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session),
                ContactPermission = ToYesNo(ParseCanContact(Session.GetAboutYouCanContact(HttpContext.Session))),
                Attachments = attachments
            };

            // Log the payload for diagnostics. Attachment content (base64) is summarised to keep the log readable.
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

            // TODO: The Power Automate backend is under construction and does not yet return a valid
            // response. The real call is commented out below and a successful response is mocked instead
            // (values mirror docs/power-automate-success-response.json). Restore this block once the
            // backend is ready.
            //
            if (string.IsNullOrWhiteSpace(_powerAutomateUrl))
                throw new InvalidOperationException("PowerAutomateUrl is not configured.");

            // Submit the request to Power Automate; the reference number comes back in the response
            // (see docs/power-automate-success-response.json). Transport-level failures (network error,
            // non-success status code, malformed response) are surfaced the same way as an application
            // failure below, rather than bubbling up to the error page.
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

            // Mocked response while the backend is under construction.
            //SubmissionResponse? result = new()
            //{
            //    Success = true,
            //    ReferenceNumber = 290212,
            //    WorkItemId = 290212,
            //    Message = "Your request has been received and logged."
            //};
            //_logger.LogWarning("Power Automate submission is mocked; no request was sent to the backend.");

            if (result is not { Success: true } || result.ReferenceNumber <= 0)
            {
                _logger.LogError("Submission to Power Automate failed: {Message}", result?.Message);
                return SubmissionError(
                    result?.Message ?? "There was a problem submitting your request. Please try again.");
            }

            Session.SetReferenceNumber(HttpContext.Session, result.ReferenceNumber.ToString());
            return RedirectToPage(Links.RequestSubmitted.PageName);
        }

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
