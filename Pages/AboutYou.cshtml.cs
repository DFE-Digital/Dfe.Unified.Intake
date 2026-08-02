using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Dfe.Unified.Intake.Pages
{
    // Allow the supporting documentation upload through the default Kestrel body-size and multipart limits
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    public class AboutYouModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Enter your full name")]
        public string? FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter your email address")]
        [EmailAddress(ErrorMessage = DfeEmailErrorMessage)]
        public string? EmailAddress { get; set; }

        /// <summary>The domain every accepted DfE email address must end with.</summary>
        private const string DfeEmailDomain = "@education.gov.uk";

        /// <summary>Shown when the supplied email is not a correctly formatted DfE address.</summary>
        private const string DfeEmailErrorMessage =
            "Enter a DfE email address in the correct format, e.g. joe.bloggs@education.gov.uk";

        public const int MaxRequestDetailsLength = 2000;

        [BindProperty]
        [Required(ErrorMessage = "Enter details about your request")]
        public string? RequestDetails { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Select whether we can contact you")]
        public string? CanContact { get; set; }

        [BindProperty]
        public IFormFileCollection? SupportingInformation { get; set; }

        public IReadOnlyList<SupportingDocument> UploadedDocuments { get; private set; } =
            Array.Empty<SupportingDocument>();

        private const int MaxFileCount = 20;

        // Per-file size cap, in megabytes. Capped at 25MB by the Power Automate backend
        public const int MaxFileSizeMb = 25;
        private const long MaxFileSizeBytes = MaxFileSizeMb * 1024L * 1024L;

        private const long MaxUploadRequestHeadroomBytes = 10 * 1024 * 1024; // 10MB
        private const long MaxUploadRequestBytes = MaxFileCount * MaxFileSizeBytes + MaxUploadRequestHeadroomBytes;

        private static readonly string[] AllowedExtensions =
            { ".png", ".jpg", ".jpeg", ".pdf", ".docx", ".xlsx" };

        public void OnGet()
        {
            // Prefills the form with the user 's DfE account details, or whatever was previously entered in the session if they returned to the page
            FullName = Session.GetAboutYouFullName(HttpContext.Session) ?? CurrentUserFullName;
            EmailAddress = Session.GetAboutYouEmailAddress(HttpContext.Session) ?? CurrentUserEmailAddress;
            RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session);
            CanContact = Session.GetAboutYouCanContact(HttpContext.Session);

            LoadUploadedDocuments();
        }

        private string? CurrentUserFullName =>
            User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value;

        private string? CurrentUserEmailAddress =>
            User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Upn)?.Value;

        public async Task<IActionResult> OnPost()
        {
            ValidateEmailAddress();
            ValidateRequestDetailsLength();
            ValidateSupportingInformation();

            if (SupportingInformation is { Count: > 0 } && SupportingInformationIsValid)
                await SupportingDocuments.SaveAsync(HttpContext.Session, SupportingInformation);

            if (!ModelState.IsValid)
            {
                LoadUploadedDocuments();
                return Page();
            }

            Session.SetAboutYouFullName(HttpContext.Session, FullName!);
            Session.SetAboutYouEmailAddress(HttpContext.Session, EmailAddress!);
            Session.SetAboutYouRequestDetails(HttpContext.Session, RequestDetails!);
            Session.SetAboutYouCanContact(HttpContext.Session, CanContact!);

            return RedirectToPage(Links.CheckYourAnswers.PageName);
        }

        // Removes a single previously uploaded document, then reloads the page (POST-redirect-GET).
        public IActionResult OnPostRemove(string storedName)
        {
            PersistEnteredDetails();
            SupportingDocuments.Remove(HttpContext.Session, storedName);

            return RedirectToPage();
        }

        // Saves whatever has been entered in the form fields so far, without validating them
        private void PersistEnteredDetails()
        {
            if (FullName is not null)
                Session.SetAboutYouFullName(HttpContext.Session, FullName);
            if (EmailAddress is not null)
                Session.SetAboutYouEmailAddress(HttpContext.Session, EmailAddress);
            if (RequestDetails is not null)
                Session.SetAboutYouRequestDetails(HttpContext.Session, RequestDetails);
            if (CanContact is not null)
                Session.SetAboutYouCanContact(HttpContext.Session, CanContact);
        }

        /// <summary>
        /// True unless the supplied files failed their own validation (extension, size or count). Used to
        /// decide whether a selection is safe to store even when other fields on the page are invalid.
        /// </summary>
        private bool SupportingInformationIsValid =>
            !ModelState.TryGetValue(nameof(SupportingInformation), out var entry) || entry.Errors.Count == 0;

        private void LoadUploadedDocuments() =>
            UploadedDocuments = SupportingDocuments.GetAll(HttpContext.Session);

        /// <summary>
        /// Only DfE email addresses are accepted, so anything supplied must end with the DfE domain
        /// (case-insensitive). The format check is left to the <see cref="EmailAddressAttribute"/>.
        /// </summary>
        private void ValidateEmailAddress()
        {
            if (string.IsNullOrWhiteSpace(EmailAddress))
                return;

            // The format check already reported this field, so a further error would just duplicate it
            if (ModelState.TryGetValue(nameof(EmailAddress), out var entry) && entry.Errors.Count > 0)
                return;

            if (!EmailAddress.EndsWith(DfeEmailDomain, StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(EmailAddress), DfeEmailErrorMessage);
        }

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

        /// <summary>
        /// Uploading supporting documentation is optional, but anything provided must satisfy the
        /// stated limits: up to 20 files, each no larger than 200MB, of an accepted file type.
        /// </summary>
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
                        $"{file.FileName} must be no larger than {MaxFileSizeMb}MB");
            }
        }
    }
}
