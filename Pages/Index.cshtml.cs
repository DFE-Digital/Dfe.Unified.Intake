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

        [BindProperty]
        public string? ServiceCode { get; set; }

        private static readonly string[] ValidServiceCodes =
            { "EAT", "VCC", "REEP", "MSI", "Prepare", "Complete", "FAST", "RECAST", "MFSP" };

        public void OnGet(string? serviceCode)
        {
            Service = Session.GetTellUsWhatYouNeedService(HttpContext.Session);
            RequestType = Session.GetTellUsWhatYouNeed(HttpContext.Session);

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
