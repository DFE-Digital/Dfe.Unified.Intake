using System.Text;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Helpers
{
    [TestFixture]
    public class SupportingDocumentsTests
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
        public async Task GetAll_returns_empty_when_nothing_saved()
        {
            var documents = SupportingDocuments.GetAll(_session);

            Assert.That(documents, Is.Empty);
            await Task.CompletedTask;
        }

        [Test]
        public async Task SaveAsync_persists_metadata_for_each_file()
        {
            var files = new FormFileCollection
            {
                MakeFile("first.pdf", "application/pdf", "one"),
                MakeFile("second.png", "image/png", "two")
            };

            await SupportingDocuments.SaveAsync(_session, files);

            var documents = SupportingDocuments.GetAll(_session);
            Assert.That(documents, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(documents[0].FileName, Is.EqualTo("first.pdf"));
                Assert.That(documents[0].ContentType, Is.EqualTo("application/pdf"));
                Assert.That(documents[0].Length, Is.EqualTo(Encoding.UTF8.GetByteCount("one")));
                Assert.That(documents[1].FileName, Is.EqualTo("second.png"));
            });
        }

        [Test]
        public async Task OpenRead_returns_the_stored_contents()
        {
            var files = new FormFileCollection { MakeFile("note.txt", "text/plain", "hello world") };
            await SupportingDocuments.SaveAsync(_session, files);
            var document = SupportingDocuments.GetAll(_session)[0];

            await using var stream = SupportingDocuments.OpenRead(_session, document);
            using var reader = new StreamReader(stream);
            var contents = await reader.ReadToEndAsync();

            Assert.That(contents, Is.EqualTo("hello world"));
        }

        [Test]
        public async Task SaveAsync_appends_to_previously_stored_documents()
        {
            await SupportingDocuments.SaveAsync(_session,
                new FormFileCollection { MakeFile("old.pdf", "application/pdf", "old") });

            await SupportingDocuments.SaveAsync(_session,
                new FormFileCollection { MakeFile("new.pdf", "application/pdf", "new") });

            var documents = SupportingDocuments.GetAll(_session);
            Assert.That(documents, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(documents[0].FileName, Is.EqualTo("old.pdf"));
                Assert.That(documents[1].FileName, Is.EqualTo("new.pdf"));
            });
        }

        [Test]
        public async Task SaveAsync_skips_files_already_stored_in_the_session()
        {
            await SupportingDocuments.SaveAsync(_session,
                new FormFileCollection { MakeFile("report.pdf", "application/pdf", "data") });

            await SupportingDocuments.SaveAsync(_session,
                new FormFileCollection { MakeFile("report.pdf", "application/pdf", "data") });

            var documents = SupportingDocuments.GetAll(_session);
            Assert.That(documents, Has.Count.EqualTo(1));
            Assert.That(documents[0].FileName, Is.EqualTo("report.pdf"));
        }

        [Test]
        public async Task SaveAsync_skips_duplicate_files_with_the_same_name_and_size()
        {
            var files = new FormFileCollection
            {
                MakeFile("report.pdf", "application/pdf", "same"),
                MakeFile("report.pdf", "application/pdf", "same"),
                MakeFile("REPORT.PDF", "application/pdf", "same"),
                MakeFile("other.pdf", "application/pdf", "different")
            };

            await SupportingDocuments.SaveAsync(_session, files);

            var documents = SupportingDocuments.GetAll(_session);
            Assert.That(documents, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(documents[0].FileName, Is.EqualTo("report.pdf"));
                Assert.That(documents[1].FileName, Is.EqualTo("other.pdf"));
            });
        }

        [Test]
        public async Task Clear_removes_documents_and_metadata()
        {
            await SupportingDocuments.SaveAsync(_session,
                new FormFileCollection { MakeFile("doc.pdf", "application/pdf", "data") });

            SupportingDocuments.Clear(_session);

            Assert.That(SupportingDocuments.GetAll(_session), Is.Empty);
        }

        private static IFormFile MakeFile(string fileName, string contentType, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "SupportingInformation", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
