using Dfe.Unified.Intake.Configuration;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Configuration
{
    [TestFixture]
    public class ApplicationInsightsOptionsTests
    {
        [Test]
        public void Configuration_section_should_be_ApplicationInsights()
        {
            Assert.That(ApplicationInsightsOptions.ConfigurationSection, Is.EqualTo("ApplicationInsights"));
        }

        [Test]
        public void Properties_default_to_null()
        {
            var options = new ApplicationInsightsOptions();

            Assert.Multiple(() =>
            {
                Assert.That(options.ConnectionString, Is.Null);
                Assert.That(options.EnableBrowserAnalytics, Is.Null);
            });
        }
    }
}
