namespace Dfe.Unified.Intake.Pages.Helpers
{
    // Metadata about an uploaded supporting document. The contents live on disk (see
    // SupportingDocuments); only this lightweight record is held in session.
    public sealed record SupportingDocument(
        string FileName,
        string StoredFileName,
        string? ContentType,
        long Length);
}
