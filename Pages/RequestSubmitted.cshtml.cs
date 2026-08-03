using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Unified.Intake.Pages
{
    public class RequestSubmittedModel : PageModel
    {
        [TempData]
        public string? ReferenceNumber { get; set; }

        public void OnGet()
        {
            var fromSession = Session.GetReferenceNumber(HttpContext.Session);
            if (fromSession is not null)
                ReferenceNumber = fromSession;

            // Clear it completely.
            Session.Clear(HttpContext.Session);
        }

        public IActionResult OnPost()
        {
            Session.Clear(HttpContext.Session);
            return RedirectToPage(Links.Index.PageName);
        }
    }
}
