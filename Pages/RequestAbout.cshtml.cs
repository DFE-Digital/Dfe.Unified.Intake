using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Unified.Intake.Pages
{
    public class RequestAboutModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Select what your request is about")]
        public string? RequestType { get; set; }

        public void OnGet()
        {
            RequestType = Session.GetTellUsWhatYouNeed(HttpContext.Session);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            Session.SetTellUsWhatYouNeed(HttpContext.Session, RequestType!);

            return RedirectToPage(Links.AiInitiative.PageName);
        }
    }
}
