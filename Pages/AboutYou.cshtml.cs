using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Unified.Intake.Pages
{
    public class AboutYouModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Enter your full name")]
        public string? FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string? EmailAddress { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter details about your request")]
        [MaxLength(1000, ErrorMessage = "Request details must be 1000 characters or less")]
        public string? RequestDetails { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Select whether we can contact you")]
        public string? CanContact { get; set; }

        [BindProperty]
        public IFormFileCollection? SupportingInformation { get; set; }

        public void OnGet()
        {
            FullName = Session.GetAboutYouFullName(HttpContext.Session);
            EmailAddress = Session.GetAboutYouEmailAddress(HttpContext.Session);
            RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session);
            CanContact = Session.GetAboutYouCanContact(HttpContext.Session);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            Session.SetAboutYouFullName(HttpContext.Session, FullName!);
            Session.SetAboutYouEmailAddress(HttpContext.Session, EmailAddress!);
            Session.SetAboutYouRequestDetails(HttpContext.Session, RequestDetails!);
            Session.SetAboutYouCanContact(HttpContext.Session, CanContact!);

            if (SupportingInformation is { Count: > 0 })
                Session.SetAboutYouSupportingInformationFileName(HttpContext.Session,
                    string.Join(", ", SupportingInformation.Select(f => f.FileName)));

            return RedirectToPage(Links.CheckYourAnswers.PageName);
        }
    }
}
