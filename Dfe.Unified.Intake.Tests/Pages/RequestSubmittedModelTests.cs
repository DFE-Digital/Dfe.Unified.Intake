using AutoFixture;
using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class RequestSubmittedModelTests
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
        public void OnGet_reads_the_reference_number_from_session()
        {
            var referenceNumber = _fixture.Create<string>();
            Session.SetReferenceNumber(_session, referenceNumber);
            var model = new RequestSubmittedModel().WithContext(_session);

            model.OnGet();

            Assert.That(model.ReferenceNumber, Is.EqualTo(referenceNumber));
        }

        [Test]
        public void OnGet_clears_the_whole_session()
        {
            Session.SetReferenceNumber(_session, _fixture.Create<string>());
            Session.SetAboutYouFullName(_session, _fixture.Create<string>());
            Session.SetAboutYouEmailAddress(_session, _fixture.Create<string>());
            var model = new RequestSubmittedModel().WithContext(_session);

            model.OnGet();

            Assert.That(_session.Keys, Is.Empty);
        }

        [Test]
        public void OnGet_keeps_the_reference_number_on_refresh_after_the_session_is_cleared()
        {
            // First display: the number comes from the session, which is then cleared.
            var referenceNumber = _fixture.Create<string>();
            Session.SetReferenceNumber(_session, referenceNumber);
            var model = new RequestSubmittedModel().WithContext(_session);
            model.OnGet();

            // Refresh: the session is empty, but TempData still carries the number into the reloaded model.
            var refreshed = new RequestSubmittedModel { ReferenceNumber = model.ReferenceNumber }.WithContext(_session);
            refreshed.OnGet();

            Assert.That(refreshed.ReferenceNumber, Is.EqualTo(referenceNumber));
        }

        [Test]
        public void OnPost_clears_the_session_and_redirects_to_index()
        {
            Session.SetReferenceNumber(_session, _fixture.Create<string>());
            Session.SetAboutYouFullName(_session, _fixture.Create<string>());
            var model = new RequestSubmittedModel().WithContext(_session);

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/Index"));
            Assert.That(_session.Keys, Is.Empty);
        }
    }
}
