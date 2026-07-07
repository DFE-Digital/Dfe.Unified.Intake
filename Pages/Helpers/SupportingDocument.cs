namespace Dfe.Unified.Intake.Pages.Helpers
{
    // Metadata about an uploaded supporting document. The contents are held separately in the session
    // (see SupportingDocuments), keyed by StoredFileName; this lightweight record is what GetAll returns.
    public sealed record SupportingDocument(
        string FileName,
        string StoredFileName,
        string? ContentType,
        long Length);
}
