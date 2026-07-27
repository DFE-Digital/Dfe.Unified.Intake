using System.Net;
using System.Text;

namespace Dfe.Unified.Intake.Tests.Support
{
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        private StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        public string? CapturedRequestBody { get; private set; }

        public static StubHttpMessageHandler RespondWith(HttpStatusCode status, string json) =>
            new(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        public static StubHttpMessageHandler Throws(Exception exception) =>
            new(_ => throw exception);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return _responder(request);
        }
    }
}
