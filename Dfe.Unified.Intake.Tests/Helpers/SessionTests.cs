using AutoFixture;
using Dfe.Unified.Intake.Pages.Helpers;
using Dfe.Unified.Intake.Tests.Support;
using NUnit.Framework;

namespace Dfe.Unified.Intake.Tests.Helpers
{
    [TestFixture]
    public class SessionTests
    {
        private FakeSession _session = null!;
        private Fixture _fixture = null!;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession();
            _fixture = new Fixture();
        }

        [Test]
        public void TellUsWhatYouNeed_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetTellUsWhatYouNeed(_session, value);

            Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.EqualTo(value));
        }

        [Test]
        public void TellUsWhatYouNeedService_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetTellUsWhatYouNeedService(_session, value);

            Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.EqualTo(value));
        }

        [Test]
        public void AboutYouFullName_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetAboutYouFullName(_session, value);

            Assert.That(Session.GetAboutYouFullName(_session), Is.EqualTo(value));
        }

        [Test]
        public void AboutYouEmailAddress_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetAboutYouEmailAddress(_session, value);

            Assert.That(Session.GetAboutYouEmailAddress(_session), Is.EqualTo(value));
        }

        [Test]
        public void AboutYouRequestDetails_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetAboutYouRequestDetails(_session, value);

            Assert.That(Session.GetAboutYouRequestDetails(_session), Is.EqualTo(value));
        }

        [Test]
        public void AboutYouCanContact_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetAboutYouCanContact(_session, value);

            Assert.That(Session.GetAboutYouCanContact(_session), Is.EqualTo(value));
        }

        [Test]
        public void ReferenceNumber_round_trips()
        {
            var value = _fixture.Create<string>();

            Session.SetReferenceNumber(_session, value);

            Assert.That(Session.GetReferenceNumber(_session), Is.EqualTo(value));
        }

        [Test]
        public void Getters_return_null_when_nothing_stored()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.Null);
                Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.Null);
                Assert.That(Session.GetAboutYouFullName(_session), Is.Null);
                Assert.That(Session.GetAboutYouEmailAddress(_session), Is.Null);
                Assert.That(Session.GetAboutYouRequestDetails(_session), Is.Null);
                Assert.That(Session.GetAboutYouCanContact(_session), Is.Null);
                Assert.That(Session.GetReferenceNumber(_session), Is.Null);
            });
        }

        [Test]
        public void Reset_clears_every_stored_value()
        {
            Session.SetTellUsWhatYouNeed(_session, _fixture.Create<string>());
            Session.SetTellUsWhatYouNeedService(_session, _fixture.Create<string>());
            Session.SetAboutYouFullName(_session, _fixture.Create<string>());
            Session.SetAboutYouEmailAddress(_session, _fixture.Create<string>());
            Session.SetAboutYouRequestDetails(_session, _fixture.Create<string>());
            Session.SetAboutYouCanContact(_session, _fixture.Create<string>());
            Session.SetReferenceNumber(_session, _fixture.Create<string>());

            Session.Reset(_session);

            Assert.Multiple(() =>
            {
                Assert.That(Session.GetTellUsWhatYouNeed(_session), Is.Null);
                Assert.That(Session.GetTellUsWhatYouNeedService(_session), Is.Null);
                Assert.That(Session.GetAboutYouFullName(_session), Is.Null);
                Assert.That(Session.GetAboutYouEmailAddress(_session), Is.Null);
                Assert.That(Session.GetAboutYouRequestDetails(_session), Is.Null);
                Assert.That(Session.GetAboutYouCanContact(_session), Is.Null);
                Assert.That(Session.GetReferenceNumber(_session), Is.Null);
            });
        }
    }
}
