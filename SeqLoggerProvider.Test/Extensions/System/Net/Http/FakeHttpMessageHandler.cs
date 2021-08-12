using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class FakeHttpMessageHandler
        : HttpMessageHandler
    {
        public static FakeHttpMessageHandler Create(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            => new(request => Task.FromResult(responseFactory.Invoke(request)));

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
            
            _receivedRequests = new();
        }

        public IReadOnlyList<HttpRequestMessage> ReceivedRequests
            => _receivedRequests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _receivedRequests.Add(request);
            return _responseFactory.Invoke(request);
        }

        private readonly List<HttpRequestMessage>                               _receivedRequests;
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>>    _responseFactory;
    }
}
