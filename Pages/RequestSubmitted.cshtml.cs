using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Unified.Intake.Pages
{
    public class RequestSubmittedModel : PageModel
    {
        public string? ReferenceNumber { get; private set; }

        public void OnGet()
        {
            ReferenceNumber = Session.GetReferenceNumber(HttpContext.Session);
        }

        public IActionResult OnPost()
        {
            Session.Reset(HttpContext.Session);
            return RedirectToPage(Links.Index.PageName);
        }
    }
}
