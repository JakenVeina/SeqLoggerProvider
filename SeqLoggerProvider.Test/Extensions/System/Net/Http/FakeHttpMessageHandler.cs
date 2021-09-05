using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class FakeHttpMessageHandler
        : HttpMessageHandler
    {
        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handleAsync)
            => _handleAsync = handleAsync;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handleAsync.Invoke(request);

        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handleAsync;
    }
}
