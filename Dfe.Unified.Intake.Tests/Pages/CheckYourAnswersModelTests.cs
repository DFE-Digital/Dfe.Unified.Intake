using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        [Test]
        public async Task OnPost_blocks_submission_when_a_file_is_infected()
        {
            SeedAnswers();
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection { MakeFile("virus.txt", "x") });
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler, scanner: ScannerReporting(ScanState.Infected));

            var result = await model.OnPost();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                // The request was never sent to Power Automate.
                Assert.That(handler.CapturedRequestBody, Is.Null);
                Assert.That(Session.GetReferenceNumber(_session), Is.Null);
                Assert.That(PageErrors.From(model.HttpContext), Has.Some.Contains("failed our virus check"));
            });
        }

        [Test]
        public async Task OnPost_skips_the_virus_check_when_the_feature_is_disabled()
        {
            SeedAnswers();
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection { MakeFile("virus.txt", "x") });
            var scanner = new Mock<IVirusScanner>();
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler, scanner: scanner.Object, scanningEnabled: false);

            var result = await model.OnPost();

            Assert.Multiple(() =>
            {
                // The file is submitted without being scanned at all.
                Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
                Assert.That(handler.CapturedRequestBody, Is.Not.Null);
            });
            scanner.VerifyNoOtherCalls();
        }

        [Test]
        public async Task OnPostStartScan_is_not_found_when_the_feature_is_disabled()
        {
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection { MakeFile("a.txt", "hi") });
            var model = BuildModel(
                StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, "{}"), scanningEnabled: false);

            var result = await model.OnPostStartScanAsync(CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnGetScanStatus_is_not_found_when_the_feature_is_disabled()
        {
            var model = BuildModel(
                StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, "{}"), scanningEnabled: false);

            var result = await model.OnGetScanStatusAsync("job-1", CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_blocks_submission_when_a_scan_cannot_be_completed()
        {
            SeedAnswers();
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection { MakeFile("a.txt", "x") });
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler, scanner: ScannerReporting(ScanState.Error));

            var result = await model.OnPost();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(handler.CapturedRequestBody, Is.Null);
                Assert.That(PageErrors.From(model.HttpContext),
                    Has.Some.Contains("could not confirm your files are safe"));
            });
        }

        [Test]
        public async Task OnPost_uses_supplied_clean_job_ids_without_rescanning()
        {
            SeedAnswers();
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection { MakeFile("a.txt", "hi") });
            var scanner = new Mock<IVirusScanner>();
            scanner
                .Setup(s => s.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScanStatusResult(ScanState.Clean, null, null));
            var handler = StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, SuccessJson());
            var model = BuildModel(handler, scanner: scanner.Object);
            // The client already scanned this file and supplied its clean job id.
            model.ScanJobIds = "job-abc";

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            // The fast path re-checks the job id rather than submitting the file again.
            scanner.Verify(
                s => s.SubmitAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task OnPostStartScan_returns_a_job_id_per_uploaded_file()
        {
            await SupportingDocuments.SaveAsync(
                _session, new FormFileCollection { MakeFile("a.txt", "hi"), MakeFile("b.txt", "yo") });
            var scanner = new Mock<IVirusScanner>();
            scanner
                .Setup(s => s.SubmitAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("job-xyz");
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, "{}"), scanner: scanner.Object);

            var result = await model.OnPostStartScanAsync(CancellationToken.None);

            var files = JsonElementOf(result).GetProperty("files");
            Assert.That(files.GetArrayLength(), Is.EqualTo(2));
            Assert.That(files[0].GetProperty("jobId").GetString(), Is.EqualTo("job-xyz"));
            Assert.That(files[0].GetProperty("fileName").GetString(), Is.EqualTo("a.txt"));
        }

        [Test]
        public async Task OnGetScanStatus_reports_the_status_as_a_lowercase_string()
        {
            var scanner = new Mock<IVirusScanner>();
            scanner
                .Setup(s => s.GetStatusAsync("job-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScanStatusResult(ScanState.Infected, "Eicar-Test-Signature", null));
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, "{}"), scanner: scanner.Object);

            var result = await model.OnGetScanStatusAsync("job-1", CancellationToken.None);

            var root = JsonElementOf(result);
            Assert.Multiple(() =>
            {
                Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("infected"));
                Assert.That(root.GetProperty("malware").GetString(), Is.EqualTo("Eicar-Test-Signature"));
            });
        }

        [Test]
        public async Task OnGetScanStatus_returns_bad_request_when_no_job_id_is_given()
        {
            var model = BuildModel(StubHttpMessageHandler.RespondWith(HttpStatusCode.OK, "{}"));

            var result = await model.OnGetScanStatusAsync("", CancellationToken.None);

            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        // Serializes the anonymous payload of a JsonResult so its properties can be asserted on.
        private static JsonElement JsonElementOf(IActionResult result)
        {
            var value = ((JsonResult)result).Value;
            return JsonSerializer.SerializeToDocument(value).RootElement;
        }

        private CheckYourAnswersModel BuildModel(
            StubHttpMessageHandler handler,
            string? url = DefaultUrl,
            IVirusScanner? scanner = null,
            bool scanningEnabled = true)
        {
            var httpClient = new HttpClient(handler);

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Poll interval 0 keeps the synchronous re-scan fallback instant in tests.
            var settings = new Dictionary<string, string?>
            {
                ["ClamAv:IsEnabled"] = scanningEnabled ? "true" : "false",
                ["ClamAv:PollIntervalSeconds"] = "0",
                ["ClamAv:MaxPollAttempts"] = "3"
            };
            if (url is not null)
                settings["PowerAutomateUrl"] = url;
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            return new CheckYourAnswersModel(
                    NullLogger<CheckYourAnswersModel>.Instance, factory.Object, scanner ?? CleanScanner(), configuration)
                .WithContext(_session);
        }

        // A scanner that reports every file clean, so tests that are not about the virus gate can ignore it.
        private static IVirusScanner CleanScanner() => ScannerReporting(ScanState.Clean);

        private static IVirusScanner ScannerReporting(ScanState state)
        {
            var scanner = new Mock<IVirusScanner>();
            scanner
                .Setup(s => s.SubmitAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("job-1");
            scanner
                .Setup(s => s.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScanStatusResult(state, null, null));
            return scanner.Object;
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
