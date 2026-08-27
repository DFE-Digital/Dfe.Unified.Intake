using AutoFixture;
using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class IndexModelTests
    {
        private FakeSession _session = null!;
        private Fixture _fixture = null!;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession();
            _fixture = new Fixture();
        }

        [Test]
        public void OnGet_splits_the_stored_services_back_into_the_checkbox_list()
        {
            Session.SetTellUsWhatYouNeedService(_session, "MSI,FAST");
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: null);

            Assert.That(model.Services, Is.EqualTo(new[] { "MSI", "FAST" }));
        }

        [Test]
        public void OnGet_drops_stored_codes_that_are_no_longer_offered()
        {
            Session.SetTellUsWhatYouNeedService(_session, "MSI,not-a-real-code");
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: null);

            Assert.That(model.Services, Is.EqualTo(new[] { "MSI" }));
        }

        [Test]
        public void OnGet_falls_back_to_serviceCode_when_session_empty()
        {
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "prepare");

            // Case-insensitive match resolves to the canonical casing.
            Assert.That(model.Services, Is.EqualTo(new[] { "Prepare" }));
        }

        [Test]
        public void OnGet_ignores_serviceCode_when_session_already_has_a_service()
        {
            Session.SetTellUsWhatYouNeedService(_session, "MSI");
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "prepare");

            Assert.That(model.Services, Is.EqualTo(new[] { "MSI" }));
        }

        [Test]
        public void OnGet_ignores_an_unknown_serviceCode()
        {
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "not-a-real-code");

            Assert.That(model.Services, Is.Empty);
        }

        [Test]
        public void OnGet_accepts_a_deep_link_to_something_new()
        {
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "SE");

            Assert.That(model.Services, Is.EqualTo(new[] { "SE" }));
        }

        [Test]
        public void OnPost_returns_the_page_when_nothing_is_selected()
        {
            var model = new IndexModel().WithContext(_session);

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(model.ModelState["Services"]!.Errors[0].ErrorMessage,
                Is.EqualTo("Select at least one service"));
            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.Null);
        }

        [Test]
        public void OnPost_returns_the_page_when_only_unknown_codes_are_posted()
        {
            var model = new IndexModel().WithContext(_session);
            model.Services = ["not-a-real-code"];

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.Null);
        }

        [Test]
        public void OnPost_returns_the_page_when_model_state_is_invalid()
        {
            var model = new IndexModel().WithContext(_session);
            model.Services = ["MSI"];
            model.ModelState.AddModelError("Services", "Select at least one service");

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.Null);
        }

        [Test]
        public void OnPost_saves_the_selection_and_redirects_when_valid()
        {
            var model = new IndexModel().WithContext(_session);
            model.Services = ["MSI"];

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/RequestAbout"));
            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo("MSI"));
        }

        [Test]
        public void OnPost_stores_several_services_comma_separated_in_catalogue_order()
        {
            var model = new IndexModel().WithContext(_session);
            // Posted in an arbitrary order; stored in the order the page presents them.
            model.Services = ["MSI", "REEP", "FAST"];

            model.OnPost();

            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo("REEP,FAST,MSI"));
        }

        [Test]
        public void OnPost_normalises_the_casing_of_posted_codes()
        {
            var model = new IndexModel().WithContext(_session);
            model.Services = ["msi"];

            model.OnPost();

            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo("MSI"));
        }

        [Test]
        public void OnPost_discards_unknown_codes_but_keeps_the_recognised_ones()
        {
            var model = new IndexModel().WithContext(_session);
            model.Services = ["MSI", "not-a-real-code"];

            model.OnPost();

            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo("MSI"));
        }

        [Test]
        public void OnPost_treats_something_new_as_an_exclusive_choice()
        {
            var model = new IndexModel().WithContext(_session);
            // Possible with JavaScript off, where the browser cannot untick the other boxes for us.
            model.Services = ["MSI", "SE", "FAST"];

            model.OnPost();

            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo("SE"));
        }

        [Test]
        public void OnPostCreateRequest_resets_session_and_redirects_to_index()
        {
            Session.SetTellUsWhatYouNeed(_session, _fixture.Create<string>());
            var model = new IndexModel().WithContext(_session);

            var result = model.OnPostCreateRequest();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/Index"));
            Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.Null);
        }

        [Test]
        public void OnPostCreateRequest_carries_service_code_when_present()
        {
            var model = new IndexModel().WithContext(_session);
            model.ServiceCode = "FAST";

            var result = (RedirectToPageResult)model.OnPostCreateRequest();

            Assert.That(result.RouteValues, Is.Not.Null);
            Assert.That(result.RouteValues!["serviceCode"], Is.EqualTo("FAST"));
        }

        [Test]
        public void OnPostCreateRequest_has_no_route_values_when_service_code_absent()
        {
            var model = new IndexModel().WithContext(_session);

            var result = (RedirectToPageResult)model.OnPostCreateRequest();

            Assert.That(result.RouteValues, Is.Null);
        }
    }
}
