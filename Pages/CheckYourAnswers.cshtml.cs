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

        public string? Service { get; private set; }
        public string? RequestType { get; private set; }
        public string? FullName { get; private set; }
        public string? EmailAddress { get; private set; }
        public string? RequestDetails { get; private set; }
        public string? SupportingInformationFileName { get; private set; }
        public string? CanContact { get; private set; }

        public void OnGet()
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
            // TODO: submit request (including the supporting documents below) to the backend.
            // The uploaded file contents are available here for the duration of the session, e.g.:
            //
            foreach (var document in SupportingDocuments.GetAll(HttpContext.Session))
            {
                await using var contents = SupportingDocuments.OpenRead(HttpContext.Session, document);
                // ... send `contents` (document.FileName, document.ContentType, document.Length) ...
            }

            Session.SetReferenceNumber(HttpContext.Session, GenerateReferenceNumber());
            return RedirectToPage(Links.RequestSubmitted.PageName);
        }

        private static string GenerateReferenceNumber()
        {
            const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "0123456789";
            var rng = Random.Shared;
            return new string([
                letters[rng.Next(letters.Length)],
                letters[rng.Next(letters.Length)],
                letters[rng.Next(letters.Length)],
                digits[rng.Next(digits.Length)],
                digits[rng.Next(digits.Length)],
                digits[rng.Next(digits.Length)],
                digits[rng.Next(digits.Length)],
                letters[rng.Next(letters.Length)]
            ]);
        }

        private static string? FormatServiceValue(string? value) =>
            value is null ? null :
            ServiceNames.TryGetValue(value, out var name) ? name : FormatValue(value);

        private static string? FormatValue(string? value) =>
            string.IsNullOrEmpty(value) ? null :
            char.ToUpper(value[0]) + value[1..].Replace('-', ' ');
    }
}
