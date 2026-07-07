using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Dfe.Unified.Intake.Pages.Helpers
{
    // Persists uploaded supporting documents in the session so their contents are still available when
    // the request is finally submitted on the Check your answers page. The browser discards the file
    // input after each POST, so without this the bytes would be lost between pages. The metadata (file
    // names etc.) is held under one key and each file's contents under its own key, so listing the
    // documents does not need to load the binaries.
    public static class SupportingDocuments
    {
        private const string MetadataKey = "AboutYou_SupportingDocuments";
        private const string ContentKeyPrefix = "AboutYou_SupportingDocument_";

        private static string ContentKey(string storedName) => ContentKeyPrefix + storedName;

        // Replaces any previously stored documents for this session with the supplied files.
        public static async Task SaveAsync(ISession session, IFormFileCollection files)
        {
            Clear(session);

            var documents = new List<SupportingDocument>();
            foreach (var file in files)
            {
                // Store the contents under an opaque key, keeping the (untrusted) original file name as
                // display metadata only.
                var storedFileName = Guid.NewGuid().ToString("N");

                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer);
                session.SetString(ContentKey(storedFileName), Convert.ToBase64String(buffer.ToArray()));

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
        public static Stream OpenRead(ISession session, SupportingDocument document)
        {
            var base64 = session.GetString(ContentKey(document.StoredFileName));
            var bytes = string.IsNullOrEmpty(base64)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(base64);

            return new MemoryStream(bytes);
        }

        // Removes all stored files for this session and forgets their metadata.
        public static void Clear(ISession session)
        {
            foreach (var document in GetAll(session))
                session.Remove(ContentKey(document.StoredFileName));

            session.Remove(MetadataKey);
        }
    }
}
