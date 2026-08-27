using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class RequestAboutModelTests
    {
        private FakeSession _session = null!;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession();
        }

        [Test]
        public void OnGet_populates_from_session()
        {
            Session.SetTellUsWhatYouNeed(_session, "suggest-a-change");
            var model = new RequestAboutModel().WithContext(_session);

            model.OnGet();

            Assert.That(model.RequestType, Is.EqualTo("suggest-a-change"));
        }

        [Test]
        public void OnGet_leaves_the_answer_unset_when_the_session_is_empty()
        {
            var model = new RequestAboutModel().WithContext(_session);

            model.OnGet();

            Assert.That(model.RequestType, Is.Null);
        }

        [Test]
        public void OnPost_returns_the_page_when_model_state_is_invalid()
        {
            var model = new RequestAboutModel().WithContext(_session);
            model.ModelState.AddModelError("RequestType", "Select what your request is about");

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.Null);
        }

        [Test]
        public void OnPost_saves_to_session_and_redirects_when_valid()
        {
            var model = new RequestAboutModel().WithContext(_session);
            model.RequestType = "report-a-problem";

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/AiInitiative"));
            Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.EqualTo("report-a-problem"));
        }
    }
}
