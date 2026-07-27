using Dfe.Unified.Intake.Pages.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Models
{
    [TestFixture]
    public class LinkItemTests
    {
        [Test]
        public void For_returns_a_copy_with_the_urn_set_and_other_values_preserved()
        {
            var item = new LinkItem { PageName = "/Index", BackText = "Back" };

            var result = item.For("URN123");

            Assert.Multiple(() =>
            {
                Assert.That(result.Urn, Is.EqualTo("URN123"));
                Assert.That(result.PageName, Is.EqualTo("/Index"));
                Assert.That(result.BackText, Is.EqualTo("Back"));
                Assert.That(item.Urn, Is.Empty, "the original should be left unchanged");
            });
        }

        [Test]
        public void OverrideFrom_uses_query_values_when_present()
        {
            var item = new LinkItem { PageName = "/Index", BackText = "Back", Urn = "original" };
            var query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["bt"] = "Go back",
                ["bl"] = "/AboutYou",
                ["u"] = "URN999"
            });

            var result = item.OverrideFrom(query);

            Assert.Multiple(() =>
            {
                Assert.That(result.BackText, Is.EqualTo("Go back"));
                Assert.That(result.PageName, Is.EqualTo("/AboutYou"));
                Assert.That(result.Urn, Is.EqualTo("URN999"));
            });
        }

        [Test]
        public void OverrideFrom_keeps_defaults_when_query_is_empty()
        {
            var item = new LinkItem { PageName = "/Index", BackText = "Back", Urn = "original" };
            var query = new QueryCollection(new Dictionary<string, StringValues>());

            var result = item.OverrideFrom(query);

            Assert.Multiple(() =>
            {
                Assert.That(result.BackText, Is.EqualTo("Back"));
                Assert.That(result.PageName, Is.EqualTo("/Index"));
                Assert.That(result.Urn, Is.EqualTo("original"));
            });
        }
    }
}
