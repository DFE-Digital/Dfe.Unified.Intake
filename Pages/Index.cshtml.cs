using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Unified.Intake.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Select a service")]
        public string? Service { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Select what your request is about")]
        public string? RequestType { get; set; }

        // Carried through from the footer "Create a request" so a start-over can keep the service lock.
        [BindProperty]
        public string? ServiceCode { get; set; }

        // Valid service codes, matching the "Which service is your request for?" dropdown values.
        private static readonly string[] ValidServiceCodes =
            { "MSI", "Prepare", "Complete", "FAST", "RECAST", "MFSP" };

        public void OnGet(string? serviceCode)
        {
            Service = Session.GetTellUsWhatYouNeedService(HttpContext.Session);
            RequestType = Session.GetTellUsWhatYouNeed(HttpContext.Session);

            // The session value takes precedence; the serviceCode query string is only a fallback
            // pre-selection when nothing has been chosen yet. The user can always change it.
            if (string.IsNullOrWhiteSpace(Service) && !string.IsNullOrWhiteSpace(serviceCode))
            {
                Service = ValidServiceCodes.FirstOrDefault(
                    code => string.Equals(code, serviceCode, StringComparison.OrdinalIgnoreCase));
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

        // Footer "Create a request" — start over: reset the session and return to this page.
        public IActionResult OnPostCreateRequest()
        {
            Session.Reset(HttpContext.Session);

            var routeValues = string.IsNullOrWhiteSpace(ServiceCode)
                ? null
                : new { serviceCode = ServiceCode };

            return RedirectToPage("/Index", routeValues);
        }
    }
}
