using Dfe.Unified.Intake.Pages.Helpers;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Helpers
{
    [TestFixture]
    public class VirusScannerTests
    {
        [TestCase("queued", ScanState.Queued)]
        [TestCase("downloading", ScanState.Downloading)]
        [TestCase("scanning", ScanState.Scanning)]
        [TestCase("clean", ScanState.Clean)]
        [TestCase("infected", ScanState.Infected)]
        [TestCase("error", ScanState.Error)]
        [TestCase("CLEAN", ScanState.Clean)]
        [TestCase("  clean  ", ScanState.Clean)]
        public void ParseScanState_maps_known_values(string status, ScanState expected)
        {
            Assert.That(ScanStateExtensions.ParseScanState(status), Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("something-unexpected")]
        [TestCase(null)]
        public void ParseScanState_treats_anything_unrecognised_as_error(string? status)
        {
            // Unknown values must never be read as Clean, or a file could slip through the gate.
            Assert.That(ScanStateExtensions.ParseScanState(status), Is.EqualTo(ScanState.Error));
        }

        [TestCase(ScanState.Queued, true)]
        [TestCase(ScanState.Downloading, true)]
        [TestCase(ScanState.Scanning, true)]
        [TestCase(ScanState.Clean, false)]
        [TestCase(ScanState.Infected, false)]
        [TestCase(ScanState.Error, false)]
        public void IsPending_is_true_only_for_in_flight_states(ScanState state, bool expected)
        {
            Assert.That(state.IsPending(), Is.EqualTo(expected));
        }
    }
}
