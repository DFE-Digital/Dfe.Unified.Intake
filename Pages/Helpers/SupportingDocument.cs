namespace Dfe.Unified.Intake.Pages.Helpers
{
    public sealed record SupportingDocument(
        string FileName,
        string StoredFileName,
        string? ContentType,
        long Length);
}
