namespace Dfe.Unified.Intake.Pages.Models
{
    /// <summary>
    /// The payload sent to Power Automate when a request is submitted.
    /// Shape mirrors docs/power-automate-request.json. Note the reference number is NOT part of the request — the server returns it in the response (see SubmissionResponse).
    /// </summary>
    public sealed record SubmissionRequest
    {
        public required string? RequestType { get; init; }
        public required string? Service { get; init; }
        public required SubmittedBy SubmittedBy { get; init; }
        public required string? RequestDetails { get; init; }
        // "yes"/"no" — the backend expects a string, not a boolean.
        public required string ContactPermission { get; init; }
        public required IReadOnlyList<SubmissionAttachment> Attachments { get; init; }
    }

    public sealed record SubmittedBy(string? Name, string? Email);

    // A supporting document included in the submission. Content is the base64-encoded file bytes.
    public sealed record SubmissionAttachment(string FileName, string? ContentType, string Content);

    // The response returned by the backend. The reference number is generated server-side (a numeric work item identifier) and returned here.
    // Shape mirrors docs/power-automate-success-response.json.
    public sealed record SubmissionResponse
    {
        public bool Success { get; init; }
        public long ReferenceNumber { get; init; }
        public long WorkItemId { get; init; }
        public string? Message { get; init; }
    }
}
