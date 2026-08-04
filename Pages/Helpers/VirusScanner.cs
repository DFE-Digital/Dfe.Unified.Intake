using System.Text.RegularExpressions;
using GovUK.Dfe.ClamAV.Api.Client.Contracts;

namespace Dfe.Unified.Intake.Pages.Helpers
{
    // The terminal and in-progress states a ClamAV scan job can report. Mirrors the string values
    // returned by GET /scan/async/{jobId} (queued, downloading, scanning, clean, infected, error).
    public enum ScanState
    {
        Queued,
        Downloading,
        Scanning,
        Clean,
        Infected,
        Error
    }

    public static class ScanStateExtensions
    {
        // A job is "in flight" until it reaches a terminal state; polling should continue while true.
        public static bool IsPending(this ScanState state) =>
            state is ScanState.Queued or ScanState.Downloading or ScanState.Scanning;

        // Maps the raw status string from the API onto ScanState.
        public static ScanState ParseScanState(string? status) => status?.Trim().ToLowerInvariant() switch
        {
            "queued" => ScanState.Queued,
            "downloading" => ScanState.Downloading,
            "scanning" => ScanState.Scanning,
            "clean" => ScanState.Clean,
            "infected" => ScanState.Infected,
            _ => ScanState.Error
        };
    }

    // The outcome of polling a single scan job.
    public sealed record ScanStatusResult(ScanState State, string? Malware, string? Error);

    // Wraps the generated ClamAV API client with the two operations the intake journey needs
    public interface IVirusScanner
    {
        // Submits a file for asynchronous scanning and returns the job id to poll.
        Task<string> SubmitAsync(string fileName, string? contentType, Stream content, CancellationToken cancellationToken = default);

        // Polls the current status of a previously submitted scan job.
        Task<ScanStatusResult> GetStatusAsync(string jobId, CancellationToken cancellationToken = default);
    }

    public sealed class VirusScanner : IVirusScanner
    {
        private readonly IClamAvApiClient _client;
        private readonly ILogger<VirusScanner> _logger;

        // Any run of characters other than letters, digits, dot, hyphen or underscore. These (apostrophes,
        // quotes, spaces, other punctuation) can break the multipart upload to the ClamAV API.
        private static readonly Regex UnsafeFileNameCharacters = new(@"[^A-Za-z0-9._-]+", RegexOptions.Compiled);

        public VirusScanner(IClamAvApiClient client, ILogger<VirusScanner> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Produces a file name safe to send to the ClamAV API. The browser-supplied name can contain
        /// characters (an apostrophe, for example) that break the upload, so any run of characters outside
        /// a conservative whitelist is collapsed to a single underscore and the extension is preserved. Used
        /// only for the scan upload; the original name is kept elsewhere for display and submission.
        /// </summary>
        public static string SanitizeFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "file";

            // Drop any directory components a browser might include (handling both separators regardless of
            // the host OS) before sanitising what remains.
            var lastSeparator = fileName.LastIndexOfAny(new[] { '/', '\\' });
            var name = lastSeparator >= 0 ? fileName[(lastSeparator + 1)..] : fileName;

            var sanitised = UnsafeFileNameCharacters.Replace(name, "_").Trim('_');

            return sanitised.Length == 0 ? "file" : sanitised;
        }

        public async Task<string> SubmitAsync(
            string fileName, string? contentType, Stream content, CancellationToken cancellationToken = default)
        {
            var safeFileName = SanitizeFileName(fileName);
            var file = new FileParameter(content, safeFileName, contentType ?? "application/octet-stream");
            var response = await _client.ScanAsync(file, cancellationToken);

            if (string.IsNullOrEmpty(response.JobId))
                throw new InvalidOperationException($"ClamAV did not return a job id for '{fileName}'.");

            _logger.LogInformation(
                "Submitted '{FileName}' for virus scanning; job {JobId}.", fileName, response.JobId);
            return response.JobId;
        }

        public async Task<ScanStatusResult> GetStatusAsync(
            string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await _client.GetScanStatusAsync(jobId, cancellationToken);
                return new ScanStatusResult(
                    ScanStateExtensions.ParseScanState(status.Status), status.Malware, status.Error);
            }
            catch (ClamAvApiException ex)
            {
                // A missing job (404) or any other API-level failure must not be read as Clean; surface
                // it as an Error so the caller blocks the submission.
                _logger.LogError(
                    ex, "Failed to read scan status for job {JobId} (status {StatusCode}).", jobId, ex.StatusCode);
                return new ScanStatusResult(ScanState.Error, null, ex.Message);
            }
        }
    }
}
