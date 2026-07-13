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

        // Maps the raw status string from the API onto ScanState. Anything unrecognised is treated as
        // Error so an unexpected value can never be mistaken for Clean and let a file through.
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

    // Wraps the generated ClamAV API client with the two operations the intake journey needs: submit a
    // file for asynchronous scanning, and poll a job's status. Keeping this behind an interface lets the
    // page handlers and tests work against a small, intention-revealing surface.
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

        public VirusScanner(IClamAvApiClient client, ILogger<VirusScanner> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<string> SubmitAsync(
            string fileName, string? contentType, Stream content, CancellationToken cancellationToken = default)
        {
            var file = new FileParameter(content, fileName, contentType ?? "application/octet-stream");
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
