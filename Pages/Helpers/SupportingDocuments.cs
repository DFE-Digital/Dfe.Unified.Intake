using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Dfe.Unified.Intake.Pages.Helpers
{
    // Persists uploaded supporting documents to a per-session temp folder so their contents are still
    // available when the request is finally submitted on the Check your answers page. The browser
    // discards the file input after each POST, so without this the bytes would be lost between pages.
    public static class SupportingDocuments
    {
        private const string MetadataKey = "AboutYou_SupportingDocuments";

        private static string StorageRoot(ISession session) =>
            Path.Combine(Path.GetTempPath(), "Dfe.Unified.Intake", "uploads", session.Id);

        // Replaces any previously stored documents for this session with the supplied files.
        public static async Task SaveAsync(ISession session, IFormFileCollection files)
        {
            Clear(session);

            var root = StorageRoot(session);
            Directory.CreateDirectory(root);

            var documents = new List<SupportingDocument>();
            foreach (var file in files)
            {
                // Store under an opaque name so a hostile original file name can't escape the folder.
                var storedFileName = Guid.NewGuid().ToString("N");
                var path = Path.Combine(root, storedFileName);

                await using (var stream = File.Create(path))
                    await file.CopyToAsync(stream);

                documents.Add(new SupportingDocument(file.FileName, storedFileName, file.ContentType, file.Length));
            }

            session.SetString(MetadataKey, JsonSerializer.Serialize(documents));
        }

        // The documents currently held for this session (empty when none were uploaded).
        public static IReadOnlyList<SupportingDocument> GetAll(ISession session)
        {
            var json = session.GetString(MetadataKey);
            return string.IsNullOrEmpty(json)
                ? Array.Empty<SupportingDocument>()
                : JsonSerializer.Deserialize<List<SupportingDocument>>(json) ?? new List<SupportingDocument>();
        }

        // Opens the stored contents of a document for reading (e.g. to forward it to the backend).
        public static Stream OpenRead(ISession session, SupportingDocument document) =>
            File.OpenRead(Path.Combine(StorageRoot(session), document.StoredFileName));

        // Deletes all stored files for this session and forgets their metadata.
        public static void Clear(ISession session)
        {
            var root = StorageRoot(session);
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);

            session.Remove(MetadataKey);
        }
    }
}
