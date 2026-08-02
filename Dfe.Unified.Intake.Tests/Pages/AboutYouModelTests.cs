using System.Security.Claims;
using System.Text;
using Dfe.Unified.Intake.Pages;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Pages
{
    [TestFixture]
    public class AboutYouModelTests
    {
        private FakeSession _session = null!;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession(Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            SupportingDocuments.Clear(_session);
        }

        [Test]
        public void OnGet_populates_fields_from_session()
        {
            Session.SetAboutYouFullName(_session, "Jane Smith");
            Session.SetAboutYouEmailAddress(_session, "jane@example.com");
            Session.SetAboutYouRequestDetails(_session, "Please help");
            Session.SetAboutYouCanContact(_session, "yes");
            var model = new AboutYouModel().WithContext(_session);

            model.OnGet();

            Assert.Multiple(() =>
            {
                Assert.That(model.FullName, Is.EqualTo("Jane Smith"));
                Assert.That(model.EmailAddress, Is.EqualTo("jane@example.com"));
                Assert.That(model.RequestDetails, Is.EqualTo("Please help"));
                Assert.That(model.CanContact, Is.EqualTo("yes"));
            });
        }

        [Test]
        public void OnGet_populates_name_and_email_from_the_signed_in_user_when_not_in_session()
        {
            var model = new AboutYouModel().WithContext(_session);
            model.PageContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim("name", "Jane Smith"),
                    new Claim("preferred_username", "jane.smith@education.gov.uk")
                },
                authenticationType: "TestAuth"));

            model.OnGet();

            Assert.Multiple(() =>
            {
                Assert.That(model.FullName, Is.EqualTo("Jane Smith"));
                Assert.That(model.EmailAddress, Is.EqualTo("jane.smith@education.gov.uk"));
            });
        }

        [Test]
        public void OnGet_prefers_session_values_over_the_signed_in_user()
        {
            Session.SetAboutYouFullName(_session, "Saved Name");
            Session.SetAboutYouEmailAddress(_session, "saved@example.com");
            var model = new AboutYouModel().WithContext(_session);
            model.PageContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim("name", "Jane Smith"),
                    new Claim("preferred_username", "jane.smith@education.gov.uk")
                },
                authenticationType: "TestAuth"));

            model.OnGet();

            Assert.Multiple(() =>
            {
                Assert.That(model.FullName, Is.EqualTo("Saved Name"));
                Assert.That(model.EmailAddress, Is.EqualTo("saved@example.com"));
            });
        }

        [Test]
        public async Task OnGet_lists_the_names_of_documents_already_stored_in_session()
        {
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection
            {
                MakeFile("evidence.pdf", contentLength: 10),
                MakeFile("photo.png", contentLength: 10)
            });
            var model = new AboutYouModel().WithContext(_session);

            model.OnGet();

            Assert.That(model.UploadedDocuments.Select(d => d.FileName),
                Is.EqualTo(new[] { "evidence.pdf", "photo.png" }));
        }

        [Test]
        public async Task OnPostRemove_removes_the_named_file_and_keeps_the_rest()
        {
            await SupportingDocuments.SaveAsync(_session, new FormFileCollection
            {
                MakeFile("keep.pdf", contentLength: 10),
                MakeFile("drop.pdf", contentLength: 10)
            });
            var storedName = SupportingDocuments.GetAll(_session)
                .Single(d => d.FileName == "drop.pdf").StoredFileName;
            var model = new AboutYouModel().WithContext(_session);

            var result = model.OnPostRemove(storedName);

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.Null); // reloads the same page
            Assert.That(SupportingDocuments.GetAll(_session).Select(d => d.FileName),
                Is.EqualTo(new[] { "keep.pdf" }));
        }

        [Test]
        public async Task OnPostRemove_keeps_details_already_entered()
        {
            await SupportingDocuments.SaveAsync(_session,
                new FormFileCollection { MakeFile("drop.pdf", contentLength: 10) });
            var storedName = SupportingDocuments.GetAll(_session)[0].StoredFileName;
            var model = new AboutYouModel().WithContext(_session);
            model.FullName = "Jane Smith";
            model.EmailAddress = "jane@example.com";

            model.OnPostRemove(storedName);

            Assert.Multiple(() =>
            {
                Assert.That(Session.GetAboutYouFullName(_session), Is.EqualTo("Jane Smith"));
                Assert.That(Session.GetAboutYouEmailAddress(_session), Is.EqualTo("jane@example.com"));
                Assert.That(SupportingDocuments.GetAll(_session), Is.Empty);
            });
        }

        [Test]
        public async Task OnPost_returns_page_when_model_state_invalid()
        {
            var model = new AboutYouModel().WithContext(_session);
            model.ModelState.AddModelError("FullName", "Enter your full name");

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetAboutYouFullName(_session), Is.Null);
        }

        [Test]
        public async Task OnPost_saves_details_and_redirects_when_valid_without_files()
        {
            var model = new AboutYouModel().WithContext(_session);
            model.FullName = "Jane Smith";
            model.EmailAddress = "jane@education.gov.uk";
            model.RequestDetails = "Please help";
            model.CanContact = "no";

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(((RedirectToPageResult)result).PageName, Is.EqualTo("/CheckYourAnswers"));
            Assert.Multiple(() =>
            {
                Assert.That(Session.GetAboutYouFullName(_session), Is.EqualTo("Jane Smith"));
                Assert.That(Session.GetAboutYouEmailAddress(_session), Is.EqualTo("jane@education.gov.uk"));
                Assert.That(Session.GetAboutYouRequestDetails(_session), Is.EqualTo("Please help"));
                Assert.That(Session.GetAboutYouCanContact(_session), Is.EqualTo("no"));
            });
        }

        [Test]
        public async Task OnPost_stores_uploaded_documents_when_valid()
        {
            var model = new AboutYouModel().WithContext(_session);
            model.FullName = "Jane";
            model.EmailAddress = "jane@education.gov.uk";
            model.RequestDetails = "Details";
            model.CanContact = "yes";
            model.SupportingInformation = new FormFileCollection
            {
                MakeFile("evidence.pdf", contentLength: 10)
            };

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var documents = SupportingDocuments.GetAll(_session);
            Assert.That(documents, Has.Count.EqualTo(1));
            Assert.That(documents[0].FileName, Is.EqualTo("evidence.pdf"));
        }

        [Test]
        public async Task OnPost_keeps_and_lists_valid_files_when_another_field_is_invalid()
        {
            var model = new AboutYouModel().WithContext(_session);
            // Valid files, but the full name is missing, so the post fails validation.
            model.EmailAddress = "jane@example.com";
            model.RequestDetails = "Details";
            model.CanContact = "yes";
            model.SupportingInformation = new FormFileCollection
            {
                MakeFile("evidence.pdf", contentLength: 10)
            };
            model.ModelState.AddModelError(nameof(model.FullName), "Enter your full name");

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.Multiple(() =>
            {
                Assert.That(SupportingDocuments.GetAll(_session), Has.Count.EqualTo(1));
                Assert.That(model.UploadedDocuments.Select(d => d.FileName),
                    Is.EqualTo(new[] { "evidence.pdf" }));
            });
        }

        [Test]
        public async Task OnPost_does_not_store_files_that_fail_their_own_validation()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.SupportingInformation = new FormFileCollection
            {
                MakeFile("virus.exe", contentLength: 10)
            };

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.Multiple(() =>
            {
                Assert.That(SupportingDocuments.GetAll(_session), Is.Empty);
                Assert.That(model.UploadedDocuments, Is.Empty);
            });
        }

        [Test]
        public async Task OnPost_rejects_a_file_with_a_disallowed_extension()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.SupportingInformation = new FormFileCollection
            {
                MakeFile("virus.exe", contentLength: 10)
            };

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(model.ModelState[nameof(model.SupportingInformation)]!.Errors,
                Has.Some.Property("ErrorMessage").Contains("must be a PNG, JPG, PDF, DOCX or XLSX file"));
        }

        [Test]
        public async Task OnPost_rejects_a_file_larger_than_the_limit()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.SupportingInformation = new FormFileCollection
            {
                // 26MB reported length, over the 25MB cap. The backing stream stays tiny.
                MakeFile("big.pdf", contentLength: 26L * 1024 * 1024)
            };

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(model.ModelState[nameof(model.SupportingInformation)]!.Errors,
                Has.Some.Property("ErrorMessage").Contains("must be no larger than 25MB"));
        }

        [Test]
        public async Task OnPost_rejects_more_than_twenty_files()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            var files = new FormFileCollection();
            for (var i = 0; i < 21; i++)
                files.Add(MakeFile($"file{i}.pdf", contentLength: 5));
            model.SupportingInformation = files;

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(model.ModelState[nameof(model.SupportingInformation)]!.Errors,
                Has.Some.Property("ErrorMessage").Contains("You can upload up to 20 files"));
        }

        [Test]
        public void RequestDetails_character_limit_is_2000()
        {
            Assert.That(AboutYouModel.MaxRequestDetailsLength, Is.EqualTo(2000));
        }

        [Test]
        public async Task OnPost_rejects_request_details_over_the_character_limit()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.RequestDetails = new string('a', AboutYouModel.MaxRequestDetailsLength + 1);

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetAboutYouRequestDetails(_session), Is.Null);
            Assert.That(model.ModelState[nameof(model.RequestDetails)]!.Errors,
                Has.Some.Property("ErrorMessage").Contains("2000 characters or less"));
        }

        [Test]
        public async Task OnPost_accepts_request_details_at_the_character_limit()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.RequestDetails = new string('a', AboutYouModel.MaxRequestDetailsLength);

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(Session.GetAboutYouRequestDetails(_session),
                Has.Length.EqualTo(AboutYouModel.MaxRequestDetailsLength));
        }

        [Test]
        public async Task OnPost_counts_newlines_as_one_character_and_stores_them_normalised()
        {
            // 1999 chars plus a CRLF newline is 2001 characters as posted, but the browser's
            // character-count counts the newline once (2000). Normalising CRLF -> LF keeps the server
            // in step with what the user sees, so a value that looks within the limit is accepted.
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.RequestDetails = new string('a', AboutYouModel.MaxRequestDetailsLength - 1) + "\r\n";

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var stored = Session.GetAboutYouRequestDetails(_session);
            Assert.Multiple(() =>
            {
                Assert.That(stored, Does.Not.Contain("\r\n"));
                Assert.That(stored, Has.Length.EqualTo(AboutYouModel.MaxRequestDetailsLength));
            });
        }

        [Test]
        public async Task OnPost_rejects_an_email_address_outside_the_dfe_domain()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.EmailAddress = "alex.edwards@dxw.com";

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(Session.GetAboutYouEmailAddress(_session), Is.Null);
            Assert.That(model.ModelState[nameof(model.EmailAddress)]!.Errors,
                Has.Some.Property("ErrorMessage")
                    .Contains("Enter a DfE email address in the correct format"));
        }

        [Test]
        public async Task OnPost_accepts_a_dfe_email_address_regardless_of_case()
        {
            var model = new AboutYouModel().WithContext(_session);
            SetValidDetails(model);
            model.EmailAddress = "Joe.Bloggs@Education.Gov.UK";

            var result = await model.OnPost();

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            Assert.That(Session.GetAboutYouEmailAddress(_session), Is.EqualTo("Joe.Bloggs@Education.Gov.UK"));
        }

        private static void SetValidDetails(AboutYouModel model)
        {
            model.FullName = "Jane";
            model.EmailAddress = "jane@education.gov.uk";
            model.RequestDetails = "Details";
            model.CanContact = "yes";
        }

        private static IFormFile MakeFile(string fileName, long contentLength)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("x"));
            return new FormFile(stream, 0, contentLength, "SupportingInformation", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }
    }
}
