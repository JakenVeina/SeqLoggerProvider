using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerDeliveryManager;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerDeliveryManager
{
    [TestFixture]
    public class DeliverAsync
    {
        public static readonly IReadOnlyList<TestCaseData> Always_TestCaseData
            = new[]
            {
                /*                  apiKey,             entryCount,     now,                                    payloadData,        responseDelay,              responseMessage                                     responseStatusCode                          serverUrl,                  */
                new TestCaseData(   default(string?),   default(int),   default(DateTimeOffset),                string.Empty,       default(TimeSpan),          "Default Message",                                  HttpStatusCode.OK,                          "http://localhost"          ).SetName("{m}(Default Values)"),
                new TestCaseData(   "API Key",          5,              DateTimeOffset.MinValue,                "Test Payload #1",  TimeSpan.FromSeconds(1),    "Response Message from API Key",                    HttpStatusCode.OK,                          "http://localhost:1"        ).SetName("{m}(With API Key)"),
                new TestCaseData(   null,               10,             DateTimeOffset.FromUnixTimeSeconds(0),  "Test Payload #2",  TimeSpan.FromSeconds(2),    "Response Message from no API Key",                 HttpStatusCode.OK,                          "http://localhost:5341"     ).SetName("{m}(Without API Key)"),
                new TestCaseData(   null,               15,             DateTimeOffset.FromUnixTimeSeconds(1),  "Test Payload #3",  TimeSpan.FromSeconds(3),    "Created Response Message",                         HttpStatusCode.Created,                     "http://localhost:5341"     ).SetName("{m}(Success: Created)"),
                new TestCaseData(   null,               20,             DateTimeOffset.FromUnixTimeSeconds(2),  "Test Payload #4",  TimeSpan.FromSeconds(4),    "Accepted Response Message",                        HttpStatusCode.Accepted,                    "http://localhost:5341"     ).SetName("{m}(Success: Accepted)"),
                new TestCaseData(   null,               25,             DateTimeOffset.FromUnixTimeSeconds(3),  "Test Payload #5",  TimeSpan.FromSeconds(5),    "No Content Response Message",                      HttpStatusCode.NoContent,                   "http://localhost:5341"     ).SetName("{m}(Success: NoContent)"),
                new TestCaseData(   null,               30,             DateTimeOffset.FromUnixTimeSeconds(4),  "Test Payload #6",  TimeSpan.FromSeconds(6),    "Bad Request Response Message",                     HttpStatusCode.BadRequest,                  "http://localhost:5341"     ).SetName("{m}(Failure: BadRequest)"),
                new TestCaseData(   null,               35,             DateTimeOffset.FromUnixTimeSeconds(5),  "Test Payload #7",  TimeSpan.FromSeconds(7),    "Unauthorized Response Message",                    HttpStatusCode.Unauthorized,                "http://localhost:5341"     ).SetName("{m}(Failure: Unauthorized)"),
                new TestCaseData(   null,               40,             DateTimeOffset.FromUnixTimeSeconds(6),  "Test Payload #8",  TimeSpan.FromSeconds(8),    "Payment Required Response Message",                HttpStatusCode.PaymentRequired,             "http://localhost:5341"     ).SetName("{m}(Failure: PaymentRequired)"),
                new TestCaseData(   null,               45,             DateTimeOffset.FromUnixTimeSeconds(7),  "Test Payload #9",  TimeSpan.FromSeconds(9),    "Forbidden Response Message",                       HttpStatusCode.Forbidden,                   "http://localhost:5341"     ).SetName("{m}(Failure: Forbidden)"),
                new TestCaseData(   null,               50,             DateTimeOffset.FromUnixTimeSeconds(8),  "Test Payload #10", TimeSpan.FromSeconds(10),   "Not Found Response Message",                       HttpStatusCode.NotFound,                    "http://localhost:5341"     ).SetName("{m}(Failure: NotFound)"),
                new TestCaseData(   null,               55,             DateTimeOffset.FromUnixTimeSeconds(9),  "Test Payload #11", TimeSpan.FromSeconds(11),   "Method Not Allowed Response Message",              HttpStatusCode.MethodNotAllowed,            "http://localhost:5341"     ).SetName("{m}(Failure: MethodNotAllowed)"),
                new TestCaseData(   null,               60,             DateTimeOffset.FromUnixTimeSeconds(10), "Test Payload #12", TimeSpan.FromSeconds(12),   "Not Acceptable Response Message",                  HttpStatusCode.NotAcceptable,               "http://localhost:5341"     ).SetName("{m}(Failure: NotAcceptable)"),
                new TestCaseData(   null,               65,             DateTimeOffset.FromUnixTimeSeconds(11), "Test Payload #13", TimeSpan.FromSeconds(13),   "Proxy Authentication Required Response Message",   HttpStatusCode.ProxyAuthenticationRequired, "http://localhost:5341"     ).SetName("{m}(Failure: ProxyAuthenticationRequired)"),
                new TestCaseData(   null,               70,             DateTimeOffset.FromUnixTimeSeconds(12), "Test Payload #14", TimeSpan.FromSeconds(14),   "Request Timeout Response Message",                 HttpStatusCode.RequestTimeout,              "http://localhost:5341"     ).SetName("{m}(Failure: RequestTimeout)"),
                new TestCaseData(   null,               75,             DateTimeOffset.FromUnixTimeSeconds(13), "Test Payload #15", TimeSpan.FromSeconds(15),   "Internal Server Error Response Message",           HttpStatusCode.InternalServerError,         "http://localhost:5341"     ).SetName("{m}(Failure: InternalServerError)"),
                new TestCaseData(   null,               80,             DateTimeOffset.FromUnixTimeSeconds(14), "Test Payload #16", TimeSpan.FromSeconds(16),   "Not Implemented Response Message",                 HttpStatusCode.NotImplemented,              "http://localhost:5341"     ).SetName("{m}(Failure: NotImplemented)"),
                new TestCaseData(   null,               85,             DateTimeOffset.FromUnixTimeSeconds(15), "Test Payload #17", TimeSpan.FromSeconds(17),   "Bad Gateway Response Message",                     HttpStatusCode.BadGateway,                  "http://localhost:5341"     ).SetName("{m}(Failure: BadGateway)"),
                new TestCaseData(   null,               90,             DateTimeOffset.FromUnixTimeSeconds(16), "Test Payload #18", TimeSpan.FromSeconds(18),   "Service Unavailable Response Message",             HttpStatusCode.ServiceUnavailable,          "http://localhost:5341"     ).SetName("{m}(Failure: ServiceUnavailable)"),
                new TestCaseData(   null,               95,             DateTimeOffset.FromUnixTimeSeconds(17), "Test Payload #19", TimeSpan.FromSeconds(19),   "Gateway Timeout Response Message",                 HttpStatusCode.GatewayTimeout,              "http://localhost:32767"    ).SetName("{m}(Failure: GatewayTimeout)")
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public async Task Always_AttemptsToDeliverPayload(
            string?         apiKey,
            int             entryCount,
            DateTimeOffset  now,
            string          payloadData,
            TimeSpan        responseDelay,
            string          responseMessage,
            HttpStatusCode  responseStatusCode,
            string          serverUrl)
        {
            using var httpMessageHandler = new FakeSeqLoggerHttpMessageHandler()
            {
                ResponseMessage     = responseMessage,
                ResponseStatusCode  = responseStatusCode
            };

            using var logger = new TestSeqLoggerSelfLogger();

            var systemClock = new FakeSystemClock()
            {
                Now = now
            };

            var uut = new Uut(
                httpClientFactory:  FakeHttpClientFactory.FromMessageHandler(httpMessageHandler),
                logger:             logger,
                options:            new FakeOptions<SeqLoggerOptions>(new()
                {
                    ApiKey      = apiKey,
                    ServerUrl   = serverUrl
                }),
                systemClock:        new FakeSystemClock());

            using var payload = new FakeSeqLoggerPayload()
            {
                EntryCount = entryCount
            };
            payload.Buffer.Write(Encoding.UTF8.GetBytes(payloadData));
            payload.Buffer.Position = 0;

            var result = uut.DeliverAsync(payload);

            result.IsCompleted.ShouldBeFalse();

            var request = httpMessageHandler.ReceivedRequests.ShouldHaveSingleItem();

            if (apiKey is not null)
                request.ApiKeys.ShouldHaveSingleItem().ShouldBe(apiKey);
            else
                request.ApiKeys.ShouldBeEmpty();

            request.Content.ShouldBe(payloadData);
            request.ContentMediaType.ShouldBe(SeqLoggerConstants.PayloadMediaType);
            request.RequestUri.ShouldBe(new Uri(new Uri(serverUrl), SeqLoggerConstants.EventIngestionApiPath));

            systemClock.Now += responseDelay;
            httpMessageHandler.CompleteRequests();

            result.IsCompleted.ShouldBeTrue();

            await result;
        }
    }
}
