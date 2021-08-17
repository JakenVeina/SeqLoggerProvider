using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadManager
{
    [TestFixture]
    public class TryDeliverPayloadAsync
    {
        public static IReadOnlyList<TestCaseData> ResponseIsSuccess_TestCaseData
            => new[]
            {
                /*                  serverUrl,                  apiKey,             eventCount,     now,                        responseDelay,              responseStatusCode                          responseMessage                                     */
                new TestCaseData(   "http://localhost",         default(string?),   default(int),   default(DateTimeOffset),    default(TimeSpan),          HttpStatusCode.OK,                          "Default Message"                                   ).SetName("{m}(Default Values)"),
                new TestCaseData(   "http://localhost:1",       "API Key",          5,              DateTimeOffset.MinValue,    TimeSpan.FromSeconds(1),    HttpStatusCode.OK,                          "Response Message from API Key"                     ).SetName("{m}(With API Key)"),
                new TestCaseData(   "http://localhost:5341",    null,               10,             DateTimeOffset.MinValue,    TimeSpan.FromSeconds(2),    HttpStatusCode.OK,                          "Response Message from no API Key"                  ).SetName("{m}(Without API Key)"),
                new TestCaseData(   "http://localhost:5341",    null,               15,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(3),    HttpStatusCode.Created,                     "Created Response Message"                          ).SetName("{m}(Success: Created)"),
                new TestCaseData(   "http://localhost:5341",    null,               20,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(4),    HttpStatusCode.Accepted,                    "Accepted Response Message"                         ).SetName("{m}(Success: Accepted)"),
                new TestCaseData(   "http://localhost:5341",    null,               25,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(5),    HttpStatusCode.NoContent,                   "No Content Response Message"                       ).SetName("{m}(Success: NoContent)"),
                new TestCaseData(   "http://localhost:5341",    null,               30,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(6),    HttpStatusCode.BadRequest,                  "Bad Request Response Message"                      ).SetName("{m}(Failure: BadRequest)"),
                new TestCaseData(   "http://localhost:5341",    null,               35,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(7),    HttpStatusCode.Unauthorized,                "Unauthorized Response Message"                     ).SetName("{m}(Failure: Unauthorized)"),
                new TestCaseData(   "http://localhost:5341",    null,               40,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(8),    HttpStatusCode.PaymentRequired,             "Payment Required Response Message"                 ).SetName("{m}(Failure: PaymentRequired)"),
                new TestCaseData(   "http://localhost:5341",    null,               45,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(9),    HttpStatusCode.Forbidden,                   "Forbidden Response Message"                        ).SetName("{m}(Failure: Forbidden)"),
                new TestCaseData(   "http://localhost:5341",    null,               50,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(10),   HttpStatusCode.NotFound,                    "Not Found Response Message"                        ).SetName("{m}(Failure: NotFound)"),
                new TestCaseData(   "http://localhost:5341",    null,               55,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(11),   HttpStatusCode.MethodNotAllowed,            "Method Not Allowed Response Message"               ).SetName("{m}(Failure: MethodNotAllowed)"),
                new TestCaseData(   "http://localhost:5341",    null,               60,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(12),   HttpStatusCode.NotAcceptable,               "Not Acceptable Response Message"                   ).SetName("{m}(Failure: NotAcceptable)"),
                new TestCaseData(   "http://localhost:5341",    null,               65,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(13),   HttpStatusCode.ProxyAuthenticationRequired, "Proxy Authentication Required Response Message"    ).SetName("{m}(Failure: ProxyAuthenticationRequired)"),
                new TestCaseData(   "http://localhost:5341",    null,               70,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(14),   HttpStatusCode.RequestTimeout,              "Request Timeout Response Message"                  ).SetName("{m}(Failure: RequestTimeout)"),
                new TestCaseData(   "http://localhost:5341",    null,               75,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(15),   HttpStatusCode.InternalServerError,         "Internal Server Error Response Message"            ).SetName("{m}(Failure: InternalServerError)"),
                new TestCaseData(   "http://localhost:5341",    null,               80,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(16),   HttpStatusCode.NotImplemented,              "Not Implemented Response Message"                  ).SetName("{m}(Failure: NotImplemented)"),
                new TestCaseData(   "http://localhost:5341",    null,               85,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(17),   HttpStatusCode.BadGateway,                  "Bad Gateway Response Message"                      ).SetName("{m}(Failure: BadGateway)"),
                new TestCaseData(   "http://localhost:5341",    null,               90,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(18),   HttpStatusCode.ServiceUnavailable,          "Service Unavailable Response Message"              ).SetName("{m}(Failure: ServiceUnavailable)"),
                new TestCaseData(   "http://localhost:32767",   null,               95,             DateTimeOffset.UnixEpoch,   TimeSpan.FromSeconds(19),   HttpStatusCode.GatewayTimeout,              "Gateway Timeout Response Message"                  ).SetName("{m}(Failure: GatewayTimeout)")
            };

        [TestCaseSource(nameof(ResponseIsSuccess_TestCaseData))]
        public async Task Always_AttemptsToDeliverPayload(
            string          serverUrl,
            string?         apiKey,
            int             eventCount,
            DateTimeOffset  now,
            TimeSpan        responseDelay,
            HttpStatusCode  responseStatusCode,
            string          responseMessage)
        {
            using var testContext = new TestContext()
            {
                HttpResponseDelay       = responseDelay,
                HttpResponseStatusCode  = responseStatusCode,
                HttpResponseMessage     = responseMessage
            };

            testContext.Options.Value = new()
            {
                ServerUrl   = serverUrl,
                ApiKey      = apiKey
            };

            testContext.SystemClock.Now = now;

            using var uut = testContext.BuildUut();

            foreach(var i in Enumerable.Range(0, eventCount))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(state: i));
            uut.TryAppendAvailableDataToPayload();

            await uut.TryDeliverPayloadAsync();

            var request = testContext.HttpMessageHandler.ReceivedRequests.ShouldHaveSingleItem();

            request.RequestUri.ShouldBe(new Uri(new Uri(serverUrl), SeqLoggerConstants.EventIngestionApiPath));
            request.Method.ShouldBe(HttpMethod.Post);

            var content = request.Content.ShouldNotBeNull();
            if (apiKey is null)
                content.Headers.Select(x => x.Key).ShouldNotContain(SeqLoggerConstants.ApiKeyHeaderName);
            else
                content.Headers.ShouldContain(x => (x.Key == SeqLoggerConstants.ApiKeyHeaderName) && x.Value.SequenceEqual(new[] { apiKey }));

            var payloadText = testContext.ReceivedRequestContents.ShouldHaveSingleItem().ShouldNotBeNull();

            var payloadTextEvents = payloadText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            payloadTextEvents.Length.ShouldBe(eventCount);

            foreach(var textEvent in payloadTextEvents)
            {
                var eventDocument = JsonDocument.Parse(textEvent);

                eventDocument.RootElement.ValueKind.ShouldBe(JsonValueKind.String);
            }
        }
    }
}
