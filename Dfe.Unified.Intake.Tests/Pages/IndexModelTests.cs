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
        public void OnGet_populates_from_session()
        {
            Session.SetTellUsWhatYouNeedService(_session, "MSI");
            Session.SetTellUsWhatYouNeed(_session, "suggest-a-change");
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: null);

            Assert.Multiple(() =>
            {
                Assert.That(model.Service, Is.EqualTo("MSI"));
                Assert.That(model.RequestType, Is.EqualTo("suggest-a-change"));
            });
        }

        [Test]
        public void OnGet_falls_back_to_serviceCode_when_session_empty()
        {
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "prepare");

            // Case-insensitive match resolves to the canonical casing.
            Assert.That(model.Service, Is.EqualTo("Prepare"));
        }

        [Test]
        public void OnGet_ignores_serviceCode_when_session_already_has_a_service()
        {
            Session.SetTellUsWhatYouNeedService(_session, "MSI");
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "prepare");

            Assert.That(model.Service, Is.EqualTo("MSI"));
        }

        [Test]
        public void OnGet_ignores_an_unknown_serviceCode()
        {
            var model = new IndexModel().WithContext(_session);

            model.OnGet(serviceCode: "not-a-real-code");

            Assert.That(model.Service, Is.Null);
        }

        [Test]
        public void OnPost_returns_the_page_when_model_state_is_invalid()
        {
            var model = new IndexModel().WithContext(_session);
            model.ModelState.AddModelError("Service", "Select a service");

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.Null);
        }

        [Test]
        public void OnPost_saves_to_session_and_redirects_when_valid()
        {
            var model = new IndexModel().WithContext(_session);
            model.Service = "MSI";
            model.RequestType = "suggest-a-change";

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/AboutYou"));
            Assert.Multiple(() =>
            {
                Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo("MSI"));
                Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.EqualTo("suggest-a-change"));
            });
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
