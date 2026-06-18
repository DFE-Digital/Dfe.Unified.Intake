using System.Collections.Concurrent;

namespace Dfe.Unified.Intake.Pages.Models
{
    public static class Links
    {
        private static readonly IDictionary<string, LinkItem> LinkCache = new ConcurrentDictionary<string, LinkItem>();

        private static LinkItem Create(string page, string text = "Back")
        {
            var item = new LinkItem { PageName = page, BackText = text };
            LinkCache.Add(page, item);
            return item;
        }

        public static readonly LinkItem Index = Create(page: "/Index");
        public static readonly LinkItem TellUsWhatYouNeed = Create(page: "/TellUsWhatYouNeed");
        public static readonly LinkItem AboutYou = Create(page: "/AboutYou");
        public static readonly LinkItem CheckYourAnswers = Create(page: "/CheckYourAnswers");
        public static readonly LinkItem RequestSubmitted = Create(page: "/RequestSubmitted");
    }
}
