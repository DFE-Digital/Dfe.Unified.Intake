using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Unified.Intake.Pages
{
    // Allow the supporting documentation upload (up to 20 files at 25MB each) through the default
    // Kestrel body-size and multipart limits, with a little headroom for boundaries and other fields.
    [RequestSizeLimit(20L * 25 * 1024 * 1024 + 10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20L * 25 * 1024 * 1024 + 10 * 1024 * 1024)]
    public class AboutYouModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Enter your full name")]
        public string? FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string? EmailAddress { get; set; }

        // Single source of truth for the request-details limit, shared with the character-count
        // component in AboutYou.cshtml. Update here to change it everywhere.
        public const int MaxRequestDetailsLength = 2000;

        [BindProperty]
        [Required(ErrorMessage = "Enter details about your request")]
        public string? RequestDetails { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Select whether we can contact you")]
        public string? CanContact { get; set; }

        [BindProperty]
        public IFormFileCollection? SupportingInformation { get; set; }

        // Constraints for "Supporting documentation". The improved file upload component cannot enforce
        // file count or size, so these are validated server-side; accept= only hints the file picker.
        private const int MaxFileCount = 20;
        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB

        private static readonly string[] AllowedExtensions =
            { ".png", ".jpg", ".jpeg", ".pdf", ".docx", ".xlsx" };

        public void OnGet()
        {
            FullName = Session.GetAboutYouFullName(HttpContext.Session);
            EmailAddress = Session.GetAboutYouEmailAddress(HttpContext.Session);
            RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session);
            CanContact = Session.GetAboutYouCanContact(HttpContext.Session);
        }

        public async Task<IActionResult> OnPost()
        {
            ValidateRequestDetailsLength();
            ValidateSupportingInformation();

            if (!ModelState.IsValid)
                return Page();

            Session.SetAboutYouFullName(HttpContext.Session, FullName!);
            Session.SetAboutYouEmailAddress(HttpContext.Session, EmailAddress!);
            Session.SetAboutYouRequestDetails(HttpContext.Session, RequestDetails!);
            Session.SetAboutYouCanContact(HttpContext.Session, CanContact!);

            // Persist the file contents (not just their names) so they're still available when the
            // request is submitted. If the user returns and submits without re-selecting files, the
            // input is empty and we keep whatever was already stored.
            if (SupportingInformation is { Count: > 0 })
                await SupportingDocuments.SaveAsync(HttpContext.Session, SupportingInformation);

            return RedirectToPage(Links.CheckYourAnswers.PageName);
        }

        // Enforce the request-details character limit server-side. The browser's character-count
        // component counts a newline as a single character (LF), but the form posts each newline as
        // CRLF (2 chars). Normalise to LF first so the server counts exactly what the user sees —
        // otherwise text that looks within the limit could be rejected early.
        private void ValidateRequestDetailsLength()
        {
            if (RequestDetails is null)
                return;

            RequestDetails = RequestDetails.Replace("\r\n", "\n");

            if (RequestDetails.Length > MaxRequestDetailsLength)
                ModelState.AddModelError(
                    nameof(RequestDetails),
                    $"Request details must be {MaxRequestDetailsLength} characters or less");
        }

        // Uploading supporting documentation is optional, but anything provided must satisfy the
        // stated limits: up to 20 files, each no larger than 25MB, of an accepted file type.
        private void ValidateSupportingInformation()
        {
            if (SupportingInformation is not { Count: > 0 })
                return;

            if (SupportingInformation.Count > MaxFileCount)
                ModelState.AddModelError(
                    nameof(SupportingInformation),
                    $"You can upload up to {MaxFileCount} files");

            foreach (var file in SupportingInformation)
            {
                var extension = Path.GetExtension(file.FileName);

                if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    ModelState.AddModelError(
                        nameof(SupportingInformation),
                        $"{file.FileName} must be a PNG, JPG, PDF, DOCX or XLSX file");

                if (file.Length > MaxFileSizeBytes)
                    ModelState.AddModelError(
                        nameof(SupportingInformation),
                        $"{file.FileName} must be no larger than 25MB");
            }
        }
    }
}
