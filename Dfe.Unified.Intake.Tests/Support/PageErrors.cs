using System.Text.Encodings.Web;
using GovUk.Frontend.AspNetCore.ComponentGeneration;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace Dfe.Unified.Intake.Tests.Support
{
    // Reads the GOV.UK "page errors" that pages add via HttpContext.AddPageError and that the auto
    // error summary renders. The library's PageErrorContext is internal, so it can't be referenced
    // directly: we reach the instance through HttpContext.Items and call its public
    // GetErrorSummaryItems() method by reflection. The error item type itself is public.
    public static class PageErrors
    {
        public static IReadOnlyList<string> From(HttpContext httpContext)
        {
            var context = httpContext.Items.Values.FirstOrDefault(
                v => v?.GetType().Name == "PageErrorContext");

            if (context is null)
                return Array.Empty<string>();

            var items = (IEnumerable<ErrorSummaryOptionsErrorItem>)context.GetType()
                .GetMethod("GetErrorSummaryItems")!
                .Invoke(context, null)!;

            return items.Select(item => Render(item.Html)).ToList();
        }

        private static string Render(IHtmlContent? content)
        {
            if (content is null)
                return string.Empty;

            using var writer = new StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }
    }
}
