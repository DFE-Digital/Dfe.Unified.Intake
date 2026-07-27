using Dfe.Unified.Intake.Pages.Models;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Models
{
    [TestFixture]
    public class LinksTests
    {
        [Test]
        public void Well_known_links_point_at_the_expected_pages()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Links.Index.PageName, Is.EqualTo("/Index"));
                Assert.That(Links.AboutYou.PageName, Is.EqualTo("/AboutYou"));
                Assert.That(Links.CheckYourAnswers.PageName, Is.EqualTo("/CheckYourAnswers"));
                Assert.That(Links.RequestSubmitted.PageName, Is.EqualTo("/RequestSubmitted"));
            });
        }

        [Test]
        public void Well_known_links_default_the_back_text()
        {
            Assert.That(Links.Index.BackText, Is.EqualTo("Back"));
            Assert.That(Links.AboutYou.BackText, Is.EqualTo("Back"));
        }
    }
}
