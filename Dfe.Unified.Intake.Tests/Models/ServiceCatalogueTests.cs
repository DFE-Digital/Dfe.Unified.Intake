using Dfe.Unified.Intake.Pages.Models;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Models
{
    [TestFixture]
    public class ServiceCatalogueTests
    {
        [Test]
        public void Every_service_has_a_unique_code()
        {
            var codes = ServiceCatalogue.All.Select(service => service.Code).ToList();

            Assert.That(codes, Is.Unique);
        }

        [Test]
        public void Something_new_is_listed_last_and_is_the_only_option_with_a_hint()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ServiceCatalogue.All[^1].Code, Is.EqualTo(ServiceCatalogue.SomethingNewCode));
                Assert.That(ServiceCatalogue.NamedServices.Select(service => service.Code),
                    Has.No.Member(ServiceCatalogue.SomethingNewCode));
                Assert.That(ServiceCatalogue.SomethingNew.Hint,
                    Is.EqualTo("None of the services above match your request"));
            });
        }

        [Test]
        public void Named_services_are_listed_alphabetically_by_label()
        {
            var labels = ServiceCatalogue.NamedServices.Select(service => service.Label).ToList();

            Assert.That(labels, Is.EqualTo(labels.OrderBy(label => label, StringComparer.Ordinal)));
        }

        [Test]
        public void NormaliseCode_resolves_a_code_to_its_canonical_casing()
        {
            Assert.That(ServiceCatalogue.NormaliseCode("prepare"), Is.EqualTo("Prepare"));
        }

        [TestCase("not-a-real-code")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void NormaliseCode_returns_null_for_anything_we_do_not_offer(string? code)
        {
            Assert.That(ServiceCatalogue.NormaliseCode(code), Is.Null);
        }

        [Test]
        public void Split_ignores_empty_entries_and_surrounding_whitespace()
        {
            Assert.That(ServiceCatalogue.Split(" MSI , ,FAST "), Is.EqualTo(new[] { "MSI", "FAST" }));
        }

        [Test]
        public void LabelsFor_returns_display_labels_in_catalogue_order()
        {
            Assert.That(ServiceCatalogue.LabelsFor("REEP,MSI"), Is.EqualTo(new[]
            {
                "Manage School Improvement (MSI)",
                "Record Engagement with Education Providers (REEP)"
            }));
        }

        [Test]
        public void LabelsFor_returns_nothing_when_no_service_is_stored()
        {
            Assert.That(ServiceCatalogue.LabelsFor(null), Is.Empty);
        }

        [Test]
        public void LabelsFor_passes_an_unrecognised_code_through_rather_than_dropping_it()
        {
            // A code retired from the catalogue should still show on Check your answers, not vanish.
            Assert.That(ServiceCatalogue.LabelsFor("MSI,retired-service"), Is.EqualTo(new[]
            {
                "Manage School Improvement (MSI)",
                "retired-service"
            }));
        }
    }
}
