using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test
{
    [TestFixture]
    public class IntegrationTests
    {
        private static ServiceProvider CreateServiceProvider(Action onEntryDelivered)
        {
            var httpMessageHandler = new FakeHttpMessageHandler(async request =>
            {
                var content = request.Content.ShouldNotBeNull();

                var payload = await content.ReadAsStringAsync();
                foreach (var encodedEntry in payload.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    onEntryDelivered.Invoke();

                    Console.WriteLine(encodedEntry);

                    using var document = JsonDocument.Parse(encodedEntry);

                    document.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            return new ServiceCollection()
                .AddSingleton(httpMessageHandler)
                .AddLogging(builder => builder
                    .AddConsole()
                    .AddFilter<ConsoleLoggerProvider>("", LogLevel.None)
                    .AddFilter<ConsoleLoggerProvider>("SeqLoggerProvider", LogLevel.Trace)
                    .AddFilter<ConsoleLoggerProvider>("SeqLoggerProvider.Test.IntegrationTests", LogLevel.None)
                    .AddSeq(
                        configure: builder => builder.Configure(options =>
                        {
                            options.MaxPayloadSize = 10 * 1024;
                            options.ServerUrl = "http://localhost";
                        }),
                        configureHttpClient: builder => builder
                         .ConfigureHttpMessageHandlerBuilder(builder => builder
                             .PrimaryHandler = httpMessageHandler))
                    .AddFilter<global::SeqLoggerProvider.SeqLoggerProvider>("", LogLevel.Trace)
                    .AddFilter<global::SeqLoggerProvider.SeqLoggerProvider>("SeqLoggerProvider", LogLevel.None)
                    .AddFilter<global::SeqLoggerProvider.SeqLoggerProvider>("SeqLoggerProvider.Test.IntegrationTests", LogLevel.Trace)
                    )
                .BuildServiceProvider(new ServiceProviderOptions()
                {
                    ValidateOnBuild = true,
                    ValidateScopes  = true
                });
        }

        [Test]
        public async Task AllLogsAreSuccessfullyDeliveredOverBriefTime()
        {
            var deliveredEntryCount = 0;

            await using (var serviceProvider = CreateServiceProvider(() => ++deliveredEntryCount))
            {
                var logger = serviceProvider.GetRequiredService<ILogger<IntegrationTests>>();

                logger.Log(LogLevel.Debug, "This is a test");
            }

            deliveredEntryCount.ShouldBe(1);
        }

        [Test]
        public async Task AllLogsAreSuccessfullyDeliveredOverLongTime()
        {
            var generatedEntryCount = 0;
            var deliveredEntryCount = 0;

            await using (var serviceProvider = CreateServiceProvider(() => ++deliveredEntryCount))
            {
                var logger = serviceProvider.GetRequiredService<ILogger<IntegrationTests>>();

                var random = new Random(42);

                var eventIds = new[]
                {
                    new EventId(random.Next(), "Test Event #1 Occurred"),
                    new EventId(random.Next(), "Test Event #2 Occurred"),
                    new EventId(random.Next(), "Test Event #3 Occurred"),
                    new EventId(random.Next(), "Test Event #4 Occurred"),
                    new EventId(random.Next(), "Test Event #5 Occurred"),
                };

                using var stopTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                while (!stopTokenSource.IsCancellationRequested)
                {
                    logger.Log(
                        logLevel:   random.Next(1, 101) switch
                        {
                            int x when x <= 1   => LogLevel.Critical,
                            int x when x <= 2   => LogLevel.Error,
                            int x when x <= 5   => LogLevel.Warning,
                            int x when x <= 20  => LogLevel.Information,
                            int x when x <= 50  => LogLevel.Debug,
                            _                   => LogLevel.Trace,
                        },
                        eventId:    eventIds[random.Next(0, eventIds.Length)],
                        state:      Enumerable.Range(1, random.Next(0, 6))
                            .Select(x => new KeyValuePair<string, object?>($"StateField{x}", random.Next(1, 6) switch
                            {
                                1 => random.Next(),
                                2 => random.Next().ToString(),
                                3 => DateTimeOffset.FromUnixTimeMilliseconds(random.Next()),
                                4 => typeof(IntegrationTests),
                                _ => typeof(IntegrationTests).GetMethods()[0]
                            }))
                            .ToArray(),
                        exception:  random.Next(1, 101) switch
                        {
                            int x when x < 5    => TestException.Create(),
                            _                   => null
                        },
                        formatter:  (_, _) => "This is a test.");
                    ++generatedEntryCount;

                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(random.Next(0, 101)));
                    }
                    catch (OperationCanceledException) { }
                }
            }

            deliveredEntryCount.ShouldBe(generatedEntryCount);
        }
    }
}
