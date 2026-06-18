using Microsoft.AspNetCore.Http;

namespace Dfe.Unified.Intake.Pages.Helpers
{
    public static class Session
    {
        private const string TellUsWhatYouNeedKey = "TellUsWhatYouNeed";
        private const string TellUsWhatYouNeedServiceKey = "TellUsWhatYouNeed_Service";

        private const string AboutYouFullNameKey = "AboutYou_FullName";
        private const string AboutYouEmailAddressKey = "AboutYou_EmailAddress";
        private const string AboutYouRequestDetailsKey = "AboutYou_RequestDetails";
        private const string AboutYouCanContactKey = "AboutYou_CanContact";
        private const string AboutYouSupportingInformationFileNameKey = "AboutYou_SupportingInformationFileName";

        public static void Reset(ISession session)
        {
            session.Remove(TellUsWhatYouNeedKey);
            session.Remove(TellUsWhatYouNeedServiceKey);
            session.Remove(AboutYouFullNameKey);
            session.Remove(AboutYouEmailAddressKey);
            session.Remove(AboutYouRequestDetailsKey);
            session.Remove(AboutYouCanContactKey);
            session.Remove(AboutYouSupportingInformationFileNameKey);
            session.Remove(ReferenceNumberKey);
        }

        // TellUsWhatYouNeed

        public static void SetTellUsWhatYouNeed(ISession session, string value) =>
            session.SetString(TellUsWhatYouNeedKey, value);

        public static string? GetTellUsWhatYouNeed(ISession session) =>
            session.GetString(TellUsWhatYouNeedKey);

        public static void SetTellUsWhatYouNeedService(ISession session, string value) =>
            session.SetString(TellUsWhatYouNeedServiceKey, value);

        public static string? GetTellUsWhatYouNeedService(ISession session) =>
            session.GetString(TellUsWhatYouNeedServiceKey);

        // AboutYou

        public static void SetAboutYouFullName(ISession session, string value) =>
            session.SetString(AboutYouFullNameKey, value);

        public static string? GetAboutYouFullName(ISession session) =>
            session.GetString(AboutYouFullNameKey);

        public static void SetAboutYouEmailAddress(ISession session, string value) =>
            session.SetString(AboutYouEmailAddressKey, value);

        public static string? GetAboutYouEmailAddress(ISession session) =>
            session.GetString(AboutYouEmailAddressKey);

        public static void SetAboutYouRequestDetails(ISession session, string value) =>
            session.SetString(AboutYouRequestDetailsKey, value);

        public static string? GetAboutYouRequestDetails(ISession session) =>
            session.GetString(AboutYouRequestDetailsKey);

        public static void SetAboutYouCanContact(ISession session, string value) =>
            session.SetString(AboutYouCanContactKey, value);

        public static string? GetAboutYouCanContact(ISession session) =>
            session.GetString(AboutYouCanContactKey);

        public static void SetAboutYouSupportingInformationFileName(ISession session, string value) =>
            session.SetString(AboutYouSupportingInformationFileNameKey, value);

        public static string? GetAboutYouSupportingInformationFileName(ISession session) =>
            session.GetString(AboutYouSupportingInformationFileNameKey);

        // ReferenceNumber

        private const string ReferenceNumberKey = "ReferenceNumber";

        public static void SetReferenceNumber(ISession session, string value) =>
            session.SetString(ReferenceNumberKey, value);

        public static string? GetReferenceNumber(ISession session) =>
            session.GetString(ReferenceNumberKey);
    }
}
