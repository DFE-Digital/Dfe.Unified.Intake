using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Dfe.Unified.Intake.Pages
{
    // Allow the supporting documentation upload through the default Kestrel body-size and multipart
    // limits. The cap is derived from the per-file rules below, with a little headroom for boundaries
    // and other form fields.
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
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

        // The documents already stored for this session, shown read-only with a Remove action. An HTML
        // file input cannot be pre-populated, so listing them is how a returning user sees what they have
        // already uploaded.
        public IReadOnlyList<SupportingDocument> UploadedDocuments { get; private set; } =
            Array.Empty<SupportingDocument>();

        // Constraints for "Supporting documentation". The improved file upload component cannot enforce
        // file count or size, so these are validated server-side; accept= only hints the file picker.
        private const int MaxFileCount = 20;
        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB

        // The most a valid submission could weigh: every file at the maximum size and count, plus
        // headroom for multipart boundaries, part headers and the other form fields. Used by the
        // request-size attributes so a within-rules upload reaches server-side validation rather than
        // being rejected by the framework first.
        private const long MaxUploadRequestHeadroomBytes = 10 * 1024 * 1024; // 10MB
        private const long MaxUploadRequestBytes = MaxFileCount * MaxFileSizeBytes + MaxUploadRequestHeadroomBytes;

        private static readonly string[] AllowedExtensions =
            { ".png", ".jpg", ".jpeg", ".pdf", ".docx", ".xlsx" };

        public void OnGet()
        {
            // Anything the user has already entered on this page takes precedence. Otherwise, fall back
            // to the signed-in user's details so a first visit arrives pre-filled with their name and email.
            FullName = Session.GetAboutYouFullName(HttpContext.Session) ?? CurrentUserFullName;
            EmailAddress = Session.GetAboutYouEmailAddress(HttpContext.Session) ?? CurrentUserEmailAddress;
            RequestDetails = Session.GetAboutYouRequestDetails(HttpContext.Session);
            CanContact = Session.GetAboutYouCanContact(HttpContext.Session);
            LoadUploadedDocuments();
        }

        // The application signs users in with their DfE account, so their details are carried on the
        // identity as claims. Azure AD issues the display name in the "name" claim and the email address
        // in the "preferred_username" claim (which is what the identity name maps to, so we read the
        // claims directly to keep the two values distinct).
        private string? CurrentUserFullName =>
            User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value;

        private string? CurrentUserEmailAddress =>
            User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Upn)?.Value;

        public async Task<IActionResult> OnPost()
        {
            ValidateRequestDetailsLength();
            ValidateSupportingInformation();

            // Persist the file contents (not just their names) as soon as the files themselves are valid,
            // before the model-state gate below. The browser discards the file input on every POST, so
            // saving here means a valid selection survives — and stays listed under "Files added" — even
            // when another field on the page is invalid. If the input is empty (nothing re-selected) we
            // keep whatever was already stored; files are only replaced by a fresh, valid selection.
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

        // Removes a single previously uploaded document, then reloads the page (POST-redirect-GET). The
        // Remove button uses formnovalidate so removing a file doesn't trip the page's other field
        // validation; any details already entered are kept so the user doesn't lose their other answers.
        public IActionResult OnPostRemove(string storedName)
        {
            PersistEnteredDetails();
            SupportingDocuments.Remove(HttpContext.Session, storedName);

            return RedirectToPage();
        }

        // Saves whatever has been entered in the form fields so far, without validating them. Only
        // non-null values are written so an unfilled field doesn't overwrite a stored value with a blank.
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

        // True unless the supplied files failed their own validation (extension, size or count). Used to
        // decide whether a selection is safe to store even when other fields on the page are invalid.
        private bool SupportingInformationIsValid =>
            !ModelState.TryGetValue(nameof(SupportingInformation), out var entry) || entry.Errors.Count == 0;

        private void LoadUploadedDocuments() =>
            UploadedDocuments = SupportingDocuments.GetAll(HttpContext.Session);

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
