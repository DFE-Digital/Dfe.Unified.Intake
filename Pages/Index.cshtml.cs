using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Unified.Intake.Pages
{
    public class IndexModel : PageModel
    {
        // Carried through from the query string so "Start now" can forward it to the next page.
        [BindProperty]
        public string? ServiceCode { get; set; }

        public void OnGet(string? serviceCode)
        {
            ServiceCode = serviceCode;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Session.Reset(HttpContext.Session);

            var routeValues = string.IsNullOrWhiteSpace(ServiceCode)
                ? null
                : new { serviceCode = ServiceCode };

            return RedirectToPage(Links.TellUsWhatYouNeed.PageName, routeValues);
        }
    }
}
