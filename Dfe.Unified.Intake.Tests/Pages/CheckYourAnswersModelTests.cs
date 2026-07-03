using System.Net;
using System.Text;
using System.Text.Json;
using AutoFixture;
using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class CheckYourAnswersModelTests
    {
        private const string DefaultUrl = "https://example.test/flow";

        private FakeSession _session = null!;
        private Fixture _fixture = null!;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession(Guid.NewGuid().ToString("N"));
            _fixture = new Fixture();
        }

        [TearDown]
        public void TearDown()
        {
            SupportingDocuments.Clear(_session);
        }

        [Test]
        public void OnGet_maps_session_values_to_display_labels()
        {
            SeedAnswers();
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson()));

            model.OnGet();

            Assert.Multiple(() =>
            {
                Assert.That(model.Service, Is.EqualTo("Find Information about Schools and Trusts"));
                Assert.That(model.RequestType, Is.EqualTo("Suggest a change"));
                Assert.That(model.FullName, Is.EqualTo("Jane Smith"));
                Assert.That(model.EmailAddress, Is.EqualTo("jane@example.com"));
                Assert.That(model.CanContact, Is.EqualTo("Yes"));
            });
        }

        [Test]
        public async Task OnPost_stores_reference_number_and_redirects_on_success()
        {
            SeedAnswers();
            const long referenceNumber = 290212;
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson(referenceNumber)));

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/RequestSubmitted"));
            // The numeric reference number is stored as its string form for display.
            Assert.That(Session.GetReferenceNumber(_session), Is.EqualTo(referenceNumber.ToString()));
        }

        [Test]
        public async Task OnPost_sends_a_correctly_populated_payload()
        {
            SeedAnswers();
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler);

            await model.OnPost();

            using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
            var root = doc.RootElement;
            Assert.Multiple(() =>
            {
                Assert.That(root.GetProperty("requestType").GetString(), Is.EqualTo("Suggest a change"));
                Assert.That(root.GetProperty("service").GetString(),
                    Is.EqualTo("Find Information about Schools and Trusts"));
                Assert.That(root.GetProperty("submittedBy").GetProperty("name").GetString(), Is.EqualTo("Jane Smith"));
                Assert.That(root.GetProperty("submittedBy").GetProperty("email").GetString(),
                    Is.EqualTo("jane@example.com"));
                Assert.That(root.GetProperty("requestDetails").GetString(),
                    Is.EqualTo("Please improve the dashboard."));
                Assert.That(root.GetProperty("contactPermission").GetString(), Is.EqualTo("Yes"));
                Assert.That(root.GetProperty("attachments").GetArrayLength(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task OnPost_maps_contact_permission_no_to_string_no()
        {
            SeedAnswers();
            Session.SetAboutYouCanContact(_session, "no");
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler);

            await model.OnPost();

            using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
            Assert.That(doc.RootElement.GetProperty("contactPermission").GetString(), Is.EqualTo("No"));
        }

        [Test]
        public async Task OnPost_includes_base64_encoded_attachments()
        {
            SeedAnswers();
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection { MakeFile("a.txt", "hi") });
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler);

            await model.OnPost();

            using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
            var attachment = doc.RootElement.GetProperty("attachments")[0];
            Assert.Multiple(() =>
            {
                Assert.That(attachment.GetProperty("fileName").GetString(), Is.EqualTo("a.txt"));
                Assert.That(attachment.GetProperty("content").GetString(),
                    Is.EqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes("hi"))));
            });
        }

        [Test]
        public async Task OnPost_shows_the_server_message_on_application_failure()
        {
            SeedAnswers();
            var json = JsonSerializer.Serialize(new
            {
                success = false,
                message = "Unable to create Azure DevOps work item."
            });
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, json));

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetReferenceNumber(_session), Is.Null);
            Assert.That(PageErrors.From(model.HttpContext),
                Has.Some.EqualTo("Unable to create Azure DevOps work item."));
            // The answers are re-populated so the page still renders the summary lists.
            Assert.That(model.Service, Is.EqualTo("Find Information about Schools and Trusts"));
        }

        [Test]
        public async Task OnPost_shows_a_generic_message_when_the_server_reports_failure_without_a_message()
        {
            SeedAnswers();
            var model = BuildModel(StubHttpMessageHandler.RespondWith(
                HttpStatusCode.OK, JsonSerializer.Serialize(new { success = false })));

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(PageErrors.From(model.HttpContext),
                Has.Some.EqualTo("There was a problem submitting your request. Please try again."));
        }

        [Test]
        public async Task OnPost_shows_a_generic_message_on_a_transport_failure()
        {
            SeedAnswers();
            var model = BuildModel(StubHttpMessageHandler.Throws(new HttpRequestException("network down")));

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetReferenceNumber(_session), Is.Null);
            Assert.That(PageErrors.From(model.HttpContext),
                Has.Some.EqualTo("There was a problem submitting your request. Please try again."));
        }

        [Test]
        public async Task OnPost_shows_a_generic_message_on_an_error_status_code()
        {
            SeedAnswers();
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.InternalServerError, "{}"));

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(PageErrors.From(model.HttpContext), Is.Not.Empty);
        }

        [Test]
        public void OnPost_throws_when_power_automate_url_is_not_configured()
        {
            SeedAnswers();
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson()), url: null);

            Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPost());
        }

        private CheckYourAnswersModel BuildModel(StubHttpMessageHandler handler, string? url = DefaultUrl)
        {
            var httpClient = new HttpClient(handler);

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c[It.IsAny<string>()]).Returns(url);

            return new CheckYourAnswersModel(
                    NullLogger<CheckYourAnswersModel>.Instance, factory.Object, configuration.Object)
                .WithContext(_session);
        }

        private void SeedAnswers()
        {
            Session.SetTellUsWhatYouNeedService(_session, "find-information-about-schools-and-trusts");
            Session.SetTellUsWhatYouNeed(_session, "suggest-a-change");
            Session.SetAboutYouFullName(_session, "Jane Smith");
            Session.SetAboutYouEmailAddress(_session, "jane@example.com");
            Session.SetAboutYouRequestDetails(_session, "Please improve the dashboard.");
            Session.SetAboutYouCanContact(_session, "yes");
        }

        // The backend returns a numeric reference number (an Azure DevOps work item id).
        private static string SuccessJson(long referenceNumber = 290212) =>
            JsonSerializer.Serialize(new
            {
                success = true,
                referenceNumber,
                workItemId = referenceNumber
            });

        private static IFormFile MakeFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "SupportingInformation", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
        }
    }
}
