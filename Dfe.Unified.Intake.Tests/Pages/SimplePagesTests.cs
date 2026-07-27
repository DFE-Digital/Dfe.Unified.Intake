using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Tests.Support;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class SimplePagesTests
    {
        [Test]
        public void WeHaveReceivedYourRequestModel_OnGet_does_not_throw()
        {
            var model = new WeHaveReceivedYourRequestModel().WithContext();

            Assert.DoesNotThrow(() => model.OnGet());
        }

        [Test]
        public void ErrorModel_OnGet_falls_back_to_the_trace_identifier()
        {
            var model = new ErrorModel().WithContext();
            model.HttpContext.TraceIdentifier = "trace-42";

            model.OnGet();

            // Activity.Current takes precedence when the test host has an ambient activity; otherwise
            // the trace identifier is used. Either way a request id is surfaced.
            Assert.Multiple(() =>
            {
                Assert.That(model.RequestId, Is.Not.Null.And.Not.Empty);
                Assert.That(model.ShowRequestId, Is.True);
                if (System.Diagnostics.Activity.Current is null)
                    Assert.That(model.RequestId, Is.EqualTo("trace-42"));
            });
        }

        [Test]
        public void ErrorModel_ShowRequestId_is_false_when_request_id_is_empty()
        {
            var model = new ErrorModel { RequestId = string.Empty };

            Assert.That(model.ShowRequestId, Is.False);
        }
    }
}
