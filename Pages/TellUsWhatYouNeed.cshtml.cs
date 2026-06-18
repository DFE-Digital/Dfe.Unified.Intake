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

        public void OnGet()
        {
            Service = Session.GetTellUsWhatYouNeedService(HttpContext.Session);
            RequestType = Session.GetTellUsWhatYouNeed(HttpContext.Session);
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
