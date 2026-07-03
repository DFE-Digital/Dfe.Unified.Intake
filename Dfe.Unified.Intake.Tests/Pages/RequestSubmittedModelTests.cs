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
        public void OnPost_resets_the_session_and_redirects_to_index()
        {
            Session.SetReferenceNumber(_session, _fixture.Create<string>());
            Session.SetAboutYouFullName(_session, _fixture.Create<string>());
            var model = new RequestSubmittedModel().WithContext(_session);

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/Index"));
            Assert.Multiple(() =>
            {
                Assert.That(Session.GetReferenceNumber(_session), Is.Null);
                Assert.That(Session.GetAboutYouFullName(_session), Is.Null);
            });
        }
    }
}
