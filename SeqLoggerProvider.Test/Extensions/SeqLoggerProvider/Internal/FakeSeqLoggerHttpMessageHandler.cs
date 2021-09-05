using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Internal
{
    internal class FakeSeqLoggerHttpMessageHandler
        : HttpMessageHandler
    {
        public FakeSeqLoggerHttpMessageHandler()
        {
            _receivedRequests = new();
            
            _requestCompletionSource    = new();
            _responseStatusCode         = HttpStatusCode.OK;
        }

        public string? ResponseMessage
        {
            get => _responseMessage;
            set => _responseMessage = value;
        }

        public HttpStatusCode ResponseStatusCode
        {
            get => _responseStatusCode;
            set => _responseStatusCode = value;
        }

        public IReadOnlyList<RequestMessageInfo> ReceivedRequests
            => _receivedRequests;

        public void CompleteRequests()
        {
            _requestCompletionSource.SetResult();
            _requestCompletionSource = new();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _receivedRequests.Add(new()
            {
                ApiKeys             = (request.Content is not null) && request.Content.Headers.TryGetValues(SeqLoggerConstants.ApiKeyHeaderName, out var apiKeys)
                    ? apiKeys.ToArray()
                    : Array.Empty<string>(),
                Content             = (request.Content is not null)
                    ? await request.Content.ReadAsStringAsync(cancellationToken)
                    : null,
                ContentMediaType    = request.Content?.Headers.ContentType?.MediaType,
                RequestUri          = request.RequestUri
            });

            await _requestCompletionSource.Task;

            return new HttpResponseMessage(ResponseStatusCode)
            {
                Content = (_responseMessage is not null)
                    ? new StringContent(_responseMessage)
                    : null
            };
        }

        private readonly List<RequestMessageInfo> _receivedRequests;

        private TaskCompletionSource    _requestCompletionSource;
        private string?                  _responseMessage;
        private HttpStatusCode          _responseStatusCode;

        public struct RequestMessageInfo
        {
            public IReadOnlyList<string> ApiKeys { get; init; }
            public string? Content { get; init; }
            public string? ContentMediaType { get; init; }
            public Uri? RequestUri { get; init; }
        }
    }
}
