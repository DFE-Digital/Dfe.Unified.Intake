namespace Dfe.Unified.Intake.Pages.Models
{
    public class LinkItem
    {
        public string PageName { get; init; } = string.Empty;
        public string BackText { get; init; } = string.Empty;
        public string Urn { get; init; } = string.Empty;

        public LinkItem For(string urn)
        {
            return new LinkItem { BackText = BackText, PageName = PageName, Urn = urn };
        }

        public LinkItem OverrideFrom(IQueryCollection query)
        {
            return new LinkItem
            {
                BackText = query.TryGetValue("bt", out var bt) ? bt.ToString() ?? string.Empty : BackText,
                PageName = query.TryGetValue("bl", out var bl) ? bl.ToString() ?? string.Empty : PageName,
                Urn = query.TryGetValue("u", out var u) ? u.ToString() ?? string.Empty : Urn
            };
        }
    }
}
