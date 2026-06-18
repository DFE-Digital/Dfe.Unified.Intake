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
            SupportingInformationFileName = Session.GetAboutYouSupportingInformationFileName(HttpContext.Session);
            CanContact = FormatValue(Session.GetAboutYouCanContact(HttpContext.Session));
        }

        public IActionResult OnPost()
        {
            // TODO: submit request to backend

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
