using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Unified.Intake.Pages
{
    public class AiInitiativeModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Select yes if your request is related to an AI initiative")]
        public string? AiInitiative { get; set; }

        public void OnGet()
        {
            AiInitiative = Session.GetAiInitiative(HttpContext.Session);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            Session.SetAiInitiative(HttpContext.Session, AiInitiative!);

            return RedirectToPage(Links.AboutYou.PageName);
        }
    }
}
