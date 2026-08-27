using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Unified.Intake.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public IList<string> Services { get; set; } = [];

        [BindProperty]
        public string? ServiceCode { get; set; }

        public void OnGet(string? serviceCode)
        {
            Services = ServiceCatalogue.Split(Session.GetTellUsWhatYouNeedService(HttpContext.Session))
                .Select(ServiceCatalogue.NormaliseCode)
                .OfType<string>()
                .ToList();

            // A deep-linked service code only pre-selects a service when the user has not chosen one already.
            if (Services.Count == 0 && ServiceCatalogue.NormaliseCode(serviceCode) is { } deepLinkedCode)
                Services = [deepLinkedCode];
        }

        public IActionResult OnPost()
        {
            var selected = SelectedCodes();

            if (selected.Count == 0)
                ModelState.AddModelError(nameof(Services), "Select at least one service");

            if (!ModelState.IsValid)
                return Page();

            Session.SetTellUsWhatYouNeedService(HttpContext.Session, string.Join(",", selected));

            return RedirectToPage(Links.RequestAbout.PageName);
        }

        public IActionResult OnPostCreateRequest()
        {
            Session.Reset(HttpContext.Session);

            var routeValues = string.IsNullOrWhiteSpace(ServiceCode)
                ? null
                : new { serviceCode = ServiceCode };

            return RedirectToPage("/Index", routeValues);
        }

        private IReadOnlyList<string> SelectedCodes()
        {
            var codes = Services
                .Select(ServiceCatalogue.NormaliseCode)
                .OfType<string>()
                .ToHashSet();

            if (codes.Contains(ServiceCatalogue.SomethingNewCode))
                return [ServiceCatalogue.SomethingNewCode];

            return ServiceCatalogue.All
                .Where(service => codes.Contains(service.Code))
                .Select(service => service.Code)
                .ToList();
        }
    }
}
