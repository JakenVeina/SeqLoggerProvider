using System.Collections.Generic;

namespace System.Net.Http
{
    public class FakeHttpClientFactory
        : IHttpClientFactory
    {
        public static FakeHttpClientFactory FromMessageHandler(HttpMessageHandler messageHandler)
            => new(() => messageHandler);

        public FakeHttpClientFactory(Func<HttpMessageHandler> messageHandlerFactory)
        {
            _messageHandlerFactory = messageHandlerFactory;

            _messageHandlers = new();
        }

        public IReadOnlyDictionary<string, HttpMessageHandler> MessageHandlers
            => _messageHandlers;

        public HttpClient CreateClient(string name)
        {
            if (!_messageHandlers.TryGetValue(name, out var messageHandler))
            {
                messageHandler = _messageHandlerFactory.Invoke();
                _messageHandlers.Add(name, messageHandler);
            }

            return new HttpClient(messageHandler, disposeHandler: false);
        }

        private readonly Dictionary<string, HttpMessageHandler> _messageHandlers;
        private readonly Func<HttpMessageHandler>               _messageHandlerFactory;
    }
}
