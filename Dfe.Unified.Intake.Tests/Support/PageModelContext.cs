using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Unified.Intake.Tests.Support
{
    // Wires a Razor PageModel up with an HttpContext (optionally carrying an in-memory session) and a
    // fresh ModelState, which is everything the page handlers under test touch.
    public static class PageModelContext
    {
        public static TModel WithContext<TModel>(this TModel model, ISession? session = null)
            where TModel : PageModel
        {
            var httpContext = new DefaultHttpContext();
            if (session is not null)
                httpContext.Session = session;

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            return model;
        }
    }
}
