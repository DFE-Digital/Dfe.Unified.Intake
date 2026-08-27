namespace Dfe.Unified.Intake.Pages.Models
{
    /// <summary>
    /// A single service the user can raise a request against.
    /// </summary>
    public sealed record ServiceOption(string Code, string Label, string? Hint = null);

    /// <summary>
    /// The services offered by the "Which service is your request for?" question.
    /// </summary>
    public static class ServiceCatalogue
    {
        /// <summary>
        /// The code for the "Something new" option. It is exclusive: choosing it clears every other selection.
        /// </summary>
        public const string SomethingNewCode = "SE";

        /// <summary>
        /// Every service, in the order they are presented to the user. "Something new" comes last,
        /// behind an "or" divider, and is rendered separately by the page.
        /// </summary>
        public static readonly IReadOnlyList<ServiceOption> All =
        [
            new("REEP", "Record Engagement with Education Providers (REEP)"),
            new("Complete", "Complete Conversions and Transfers (Complete)"),
            new("EAT", "External Applications - Academy Transfers (EAT)"),
            new("VCC", "Vulnerable Children’s Casework (VCC)"),
            new("FAST", "Find Information about Schools and Trusts (FAST)"),
            new("MFSP", "Manage Free School Projects (MFSP)"),
            new("MSI", "Manage School Improvement (MSI)"),
            new("Prepare", "Prepare Conversions and Transfers (Prepare)"),
            new("RECAST", "Record Concerns and Supports for Trusts (RECAST)"),
            new(SomethingNewCode, "Something new", "None of the services above match your request")
        ];

        /// <summary>
        /// The named services, excluding "Something new".
        /// </summary>
        public static IEnumerable<ServiceOption> NamedServices =>
            All.Where(service => service.Code != SomethingNewCode);

        /// <summary>
        /// The exclusive "Something new" option.
        /// </summary>
        public static ServiceOption SomethingNew =>
            All.Single(service => service.Code == SomethingNewCode);

        /// <summary>
        /// Splits the comma-separated codes held in the session back into individual values.
        /// </summary>
        public static IReadOnlyList<string> Split(string? codes) =>
            (codes ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        /// <summary>
        /// Resolves a code to its canonical casing, or null when it is not a service we offer.
        /// </summary>
        public static string? NormaliseCode(string? code) =>
            string.IsNullOrWhiteSpace(code)
                ? null
                : All.FirstOrDefault(service =>
                    string.Equals(service.Code, code, StringComparison.OrdinalIgnoreCase))?.Code;

        /// <summary>
        /// Turns the comma-separated codes held in the session into their display labels, in catalogue order.
        /// </summary>
        public static IReadOnlyList<string> LabelsFor(string? codes)
        {
            var selected = Split(codes);
            if (selected.Count == 0)
                return [];

            var known = All
                .Where(service => selected.Contains(service.Code, StringComparer.OrdinalIgnoreCase))
                .Select(service => service.Label);

            var unknown = selected.Where(code => NormaliseCode(code) is null);

            return known.Concat(unknown).ToList();
        }
    }
}
