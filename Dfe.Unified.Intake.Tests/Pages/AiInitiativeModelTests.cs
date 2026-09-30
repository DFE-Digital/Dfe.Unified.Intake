using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class AiInitiativeModelTests
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
            Session.SetAiInitiative(_session, "yes");
            var model = new AiInitiativeModel().WithContext(_session);

            model.OnGet();

            Assert.That(model.AiInitiative, Is.EqualTo("yes"));
        }

        [Test]
        public void OnGet_leaves_the_answer_unset_when_the_session_is_empty()
        {
            var model = new AiInitiativeModel().WithContext(_session);

            model.OnGet();

            Assert.That(model.AiInitiative, Is.Null);
        }

        [Test]
        public void OnPost_returns_the_page_when_model_state_is_invalid()
        {
            var model = new AiInitiativeModel().WithContext(_session);
            model.ModelState.AddModelError(
                "AiInitiative", "Select yes if your request is related to an AI initiative");

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetAiInitiative(_session), Is.Null);
        }

        [Test]
        public void OnPost_saves_to_session_and_redirects_when_valid()
        {
            var model = new AiInitiativeModel().WithContext(_session);
            model.AiInitiative = "no";

            var result = model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/AboutYou"));
            Assert.That(Session.GetAiInitiative(_session), Is.EqualTo("no"));
        }
    }
}
