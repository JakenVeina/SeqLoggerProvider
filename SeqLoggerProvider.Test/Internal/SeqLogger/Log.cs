using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLogger
{
    [TestFixture]
    public class Log
    {
        public static TestCaseData CreateAlwaysTestCase<TState>(
                string                  testName,
                int                     eventId,
                string?                 exceptionMessage,
                LogLevel                logLevel,
                string                  message,
                IReadOnlyList<object?>  scopeStates,
                TState                  state,
                DateTimeOffset          now)
            => new TestCaseData(
                    /*categoryName*/    $"SeqLoggerProvider.Test.{testName}",
                    /*eventId*/         new EventId(eventId, $"{testName}Executed"),
                    /*exception*/       (exceptionMessage is not null)
                        ? TestException.Create()
                        : null,
                    /*logLevel*/        logLevel,
                    /*message*/         message,
                    /*scopeStates*/     scopeStates,
                    /*state*/           state,
                    /*now*/             now)
                .SetName($"{{m}}({testName})");

        public static IReadOnlyList<TestCaseData> Always_TestCaseData
            => new[]
            {
                /*                      testName,               eventId,        exceptionMessage,       logLevel,               message,                            scopeStates,                                                        state,                      now                         */
                CreateAlwaysTestCase(   "DefaultValues",        default,        null,                   default,                string.Empty,                       Array.Empty<object?>(),                                             default(object?),           default                     ),
                CreateAlwaysTestCase(   "MinValues",            int.MinValue,   null,                   LogLevel.Trace,         string.Empty,                       new object?[] { int.MinValue },                                     int.MinValue,               DateTimeOffset.MinValue     ),
                CreateAlwaysTestCase(   "MaxValues",            int.MaxValue,   null,                   LogLevel.Critical,      string.Empty,                       new object?[] { int.MaxValue },                                     int.MaxValue,               DateTimeOffset.MaxValue     ),
                CreateAlwaysTestCase(   "UniqueValues",         1,              "This is an exception", LogLevel.Information,   "This is a test message",           new object?[] { "This is a scope state string", 2, new object() },  "This is a state string",   DateTimeOffset.UnixEpoch    ),
                CreateAlwaysTestCase(   "NullScope",            2,              null,                   LogLevel.Information,   "This is another test message",     new object?[] { null },                                             "This is a state string",   DateTimeOffset.UnixEpoch    )
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_CreatesAndWritesEvent(
            string                  categoryName,
            EventId                 eventId,
            Exception?              exception,
            LogLevel                logLevel,
            string                  message,
            IReadOnlyList<object>   scopeStates,
            object?                 state,
            DateTimeOffset          now)
        {
            Expression<Action> expression = () => Always_CreatesAndWritesEvent<object?>(default!, default, default, default, default!, default!, default, default);
            ((MethodCallExpression)expression.Body)
                .Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(state?.GetType() ?? typeof(object))
                .Invoke(null, new[] { categoryName, eventId, exception, logLevel, message, scopeStates, state, now });
        }

        private static void Always_CreatesAndWritesEvent<TState>(
            string                  categoryName,
            EventId                 eventId,
            Exception?              exception,
            LogLevel                logLevel,
            string                  message,
            IReadOnlyList<object?>  scopeStates,
            TState                  state,
            DateTimeOffset          now)
        {
            var externalScopeProvider = new FakeExternalScopeProvider();
            foreach (var scopeState in scopeStates)
                externalScopeProvider.Push(scopeState);

            var mockOnLog = new Mock<Action>();

            var seqLoggerEventChannel = new FakeSeqLoggerEventChannel();
            
            var systemClock = new FakeSystemClock()
            {
                Now = now
            };

            var uut = new Uut(
                categoryName:           categoryName,
                externalScopeProvider:  externalScopeProvider,
                onLog:                  mockOnLog.Object,
                seqLoggerEventChannel:  seqLoggerEventChannel,
                systemClock:            systemClock);

            var mockFormatter = new Mock<Func<TState, Exception?, string>>();
            mockFormatter
                .Setup(x => x(It.IsAny<TState>(), It.IsAny<Exception?>()))
                .Returns(message);

            uut.Log(
                logLevel:   logLevel,
                eventId:    eventId,
                state:      state,
                exception:  exception,
                formatter:  mockFormatter.Object);

            var @event = (SeqLoggerEvent<TState>)seqLoggerEventChannel.Events.ShouldHaveSingleItem();

            @event.CategoryName.ShouldBe(categoryName);
            @event.EventId.Id.ShouldBe(eventId.Id);
            @event.EventId.Name.ShouldBe(eventId.Name);
            @event.Exception.ShouldBe(exception);
            @event.Formatter.ShouldBe(mockFormatter.Object);
            @event.LogLevel.ShouldBe(logLevel);
            @event.OccurredUtc.ShouldBe(now.UtcDateTime);
            @event.ScopeStatesBuffer.ShouldBe(scopeStates.Where(scopeState => scopeState is not null), ignoreOrder: true);
            @event.State.ShouldBe(state);

            mockOnLog.Verify(
                x => x(),
                Times.Once);

            mockFormatter.Invocations.ShouldBeEmpty();

            var result = @event.BuildMessage();

            mockFormatter.Verify(
                x => x(state, exception),
                Times.Once);

            result.ShouldBe(message);

            seqLoggerEventChannel.ReturnedScopeStateBuffers.ShouldBeEmpty();
        }
    }
}
