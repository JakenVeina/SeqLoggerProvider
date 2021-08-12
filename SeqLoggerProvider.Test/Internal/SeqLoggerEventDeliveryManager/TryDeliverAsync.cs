using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Utilities;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEventDeliveryManager;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEventDeliveryManager
{
    [TestFixture]
    public class TryDeliverAsync
    {
        public static IReadOnlyList<TestCaseData> ResponseIsSuccess_TestCaseData
            => new[]
            {
                /*                  serverUrl,                  apiKey,             payloadSize,    eventCount,     utcNow,                     responseDelay,              responseStatusCode                          responseMessage,                                    expectedResult  */
                new TestCaseData(   "http://localhost",         default(string?),   default(int),   default(uint),  default(DateTimeOffset),    default(TimeSpan),          HttpStatusCode.OK,                          "Default Message",                                  true            ).SetName("{m}(Default Values)"),
                new TestCaseData(   "http://localhost:1",       "API Key",          5,              1U,             DateTimeOffset.MinValue,    TimeSpan.FromSeconds(1),    HttpStatusCode.OK,                          "Response Message from API Key",                    true            ).SetName("{m}(With API Key)"),
                new TestCaseData(   "http://localhost:5341",    null,               10,             5U,             DateTimeOffset.MinValue,    TimeSpan.FromSeconds(2),    HttpStatusCode.OK,                          "Response Message from no API Key",                 true            ).SetName("{m}(Without API Key)"),
                new TestCaseData(   "http://localhost:5341",    null,               15,             10U,            DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(3),    HttpStatusCode.Created,                     "Created Response Message",                         true            ).SetName("{m}(Success: Created)"),
                new TestCaseData(   "http://localhost:5341",    null,               20,             50U,            DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(4),    HttpStatusCode.Accepted,                    "Accepted Response Message",                        true            ).SetName("{m}(Success: Accepted)"),
                new TestCaseData(   "http://localhost:5341",    null,               25,             100U,           DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(5),    HttpStatusCode.NoContent,                   "No Content Response Message",                      true            ).SetName("{m}(Success: NoContent)"),
                new TestCaseData(   "http://localhost:5341",    null,               30,             500U,           DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(6),    HttpStatusCode.BadRequest,                  "Bad Request Response Message",                     false           ).SetName("{m}(Failure: BadRequest)"),
                new TestCaseData(   "http://localhost:5341",    null,               35,             1000U,          DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(7),    HttpStatusCode.Unauthorized,                "Unauthorized Response Message",                    false           ).SetName("{m}(Failure: Unauthorized)"),
                new TestCaseData(   "http://localhost:5341",    null,               40,             5000U,          DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(8),    HttpStatusCode.PaymentRequired,             "Payment Required Response Message",                false           ).SetName("{m}(Failure: PaymentRequired)"),
                new TestCaseData(   "http://localhost:5341",    null,               45,             10000U,         DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(9),    HttpStatusCode.Forbidden,                   "Forbidden Response Message",                       false           ).SetName("{m}(Failure: Forbidden)"),
                new TestCaseData(   "http://localhost:5341",    null,               50,             50000U,         DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(10),   HttpStatusCode.NotFound,                    "Not Found Response Message",                       false           ).SetName("{m}(Failure: NotFound)"),
                new TestCaseData(   "http://localhost:5341",    null,               55,             100000U,        DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(11),   HttpStatusCode.MethodNotAllowed,            "Method Not Allowed Response Message",              false           ).SetName("{m}(Failure: MethodNotAllowed)"),
                new TestCaseData(   "http://localhost:5341",    null,               60,             500000U,        DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(12),   HttpStatusCode.NotAcceptable,               "Not Acceptable Response Message",                  false           ).SetName("{m}(Failure: NotAcceptable)"),
                new TestCaseData(   "http://localhost:5341",    null,               65,             1000000U,       DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(13),   HttpStatusCode.ProxyAuthenticationRequired, "Proxy Authentication Required Response Message",   false           ).SetName("{m}(Failure: ProxyAuthenticationRequired)"),
                new TestCaseData(   "http://localhost:5341",    null,               70,             5000000U,       DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(14),   HttpStatusCode.RequestTimeout,              "Request Timeout Response Message",                 false           ).SetName("{m}(Failure: RequestTimeout)"),
                new TestCaseData(   "http://localhost:5341",    null,               75,             10000000U,      DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(15),   HttpStatusCode.InternalServerError,         "Internal Server Error Response Message",           false           ).SetName("{m}(Failure: InternalServerError)"),
                new TestCaseData(   "http://localhost:5341",    null,               80,             50000000U,      DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(16),   HttpStatusCode.NotImplemented,              "Not Implemented Response Message",                 false           ).SetName("{m}(Failure: NotImplemented)"),
                new TestCaseData(   "http://localhost:5341",    null,               85,             100000000U,     DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(17),   HttpStatusCode.BadGateway,                  "Bad Gateway Response Message",                     false           ).SetName("{m}(Failure: BadGateway)"),
                new TestCaseData(   "http://localhost:5341",    null,               90,             500000000U,     DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(18),   HttpStatusCode.ServiceUnavailable,          "Service Unavailable Response Message",             false           ).SetName("{m}(Failure: ServiceUnavailable)"),
                new TestCaseData(   "http://localhost:32767",   null,               95,             1000000000U,    DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(19),   HttpStatusCode.GatewayTimeout,              "Gateway Timeout Response Message",                 false           ).SetName("{m}(Failure: GatewayTimeout)")
            };

        [TestCaseSource(nameof(ResponseIsSuccess_TestCaseData))]
        public async Task Always_ReturnsExpected(
            string          serverUrl,
            string?         apiKey,
            int             payloadSize,
            uint            eventCount,
            DateTimeOffset  utcNow,
            TimeSpan        responseDelay,
            HttpStatusCode  responseStatusCode,
            string          responseMessage,
            bool            expectedResult)
        {
            var systemClock = new FakeSystemClock()
            {
                Now = utcNow
            };

            using var httpMessageHandler = FakeHttpMessageHandler.Create(_ =>
            {
                systemClock.Now += responseDelay;
                
                return new HttpResponseMessage(responseStatusCode)
                {
                    Content = new StringContent(responseMessage)
                };
            });

            using var loggerFactory = TestLogger.CreateFactory();
            var uut = new Uut(
                httpClientFactory:      FakeHttpClientFactory.FromMessageHandler(httpMessageHandler),
                logger:                 loggerFactory.CreateLogger<Uut>(),
                seqLoggerConfiguration: new FakeOptions<SeqLoggerConfiguration>(new()
                {
                    ApiKey      = apiKey,
                    ServerUrl   = serverUrl
                }),
                systemClock:            systemClock);

            var payloadData = Enumerable.Range(0, payloadSize)
                .Select(x => (byte)x)
                .ToArray();
            using var payloadBuffer = new MemoryStream(payloadData);

            var result = await uut.TryDeliverAsync(
                payloadBuffer,
                eventCount);

            var request = httpMessageHandler.ReceivedRequests.ShouldHaveSingleItem();

            request.RequestUri.ShouldBe(new Uri(new Uri(serverUrl), SeqLoggerConstants.EventIngestionApiPath));
            request.Method.ShouldBe(HttpMethod.Post);

            var content = request.Content.ShouldBeOfType<StreamContent>();
            if (apiKey is null)
                content.Headers.Select(x => x.Key).ShouldNotContain(SeqLoggerConstants.ApiKeyHeaderName);
            else
                content.Headers.ShouldContain(x => (x.Key == SeqLoggerConstants.ApiKeyHeaderName) && x.Value.SequenceEqual(new[] { apiKey }));
            (await content.ReadAsByteArrayAsync()).ShouldBe(payloadData, ignoreOrder: false);

            result.ShouldBe(expectedResult);
        }
    }
}
