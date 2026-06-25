using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Unified.Intake.Pages
{
    public class TellUsWhatYouNeedModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Select a service")]
        public string? Service { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Select what your request is about")]
        public string? RequestType { get; set; }

        // Valid service codes, matching the "Which service is your request for?" dropdown values.
        private static readonly string[] ValidServiceCodes =
            { "MSI", "Prepare", "Complete", "FAST", "RECAST", "MFSP" };

        // When a valid serviceCode query string is supplied, the dropdown is pre-selected and locked.
        public bool ServiceLocked { get; private set; }

        public void OnGet(string? serviceCode)
        {
            Service = Session.GetTellUsWhatYouNeedService(HttpContext.Session);
            RequestType = Session.GetTellUsWhatYouNeed(HttpContext.Session);

            if (!string.IsNullOrWhiteSpace(serviceCode))
            {
                var match = ValidServiceCodes.FirstOrDefault(
                    code => string.Equals(code, serviceCode, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    Service = match;
                    ServiceLocked = true;
                }
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            Session.SetTellUsWhatYouNeedService(HttpContext.Session, Service!);
            Session.SetTellUsWhatYouNeed(HttpContext.Session, RequestType!);

            return RedirectToPage(Links.AboutYou.PageName);
        }
    }
}
