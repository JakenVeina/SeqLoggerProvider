using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class FakeHttpMessageHandler
        : HttpMessageHandler
    {
        public static FakeHttpMessageHandler FromResponse(HttpResponseMessage response)
            => new(_ => response);

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
            
            _receivedRequests = new();
        }

        public IReadOnlyList<HttpRequestMessage> ReceivedRequests
            => _receivedRequests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _receivedRequests.Add(request);
            return Task.FromResult(_responseFactory.Invoke(request));
        }

        private readonly List<HttpRequestMessage>                       _receivedRequests;
        private readonly Func<HttpRequestMessage, HttpResponseMessage>  _responseFactory;
    }
}
