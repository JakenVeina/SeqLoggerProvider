using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerSelfLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerSelfLogger
{
    [TestFixture]
    public class Log
    {
        [Test]
        public void LoggerHasNotMaterialized_ThrowsException()
        {
            var logger = Mock.Of<ILogger>();

            var uut = new Uut(() => logger);

            Should.Throw<InvalidOperationException>(() =>
            {
                uut.Log(
                    logLevel:   default,
                    eventId:    new(default, string.Empty),
                    state:      default(object?),
                    exception:  null,
                    formatter:  (_, _) => string.Empty);
            });
        }

        public static readonly IReadOnlyList<TestCaseData> LoggerHasMaterialized_TestCaseData
            = new[]
            {
                /*                  logLevel,               eventId,                            state,          exception               */
                new TestCaseData(   default(LogLevel),      new EventId(default, string.Empty), default,        default                 ).SetName("{m}(Default Values)"),
                new TestCaseData(   LogLevel.Debug,         new EventId(1, "Event #1"),         new object(),   null                    ).SetName("{m}(Object state)"),
                new TestCaseData(   LogLevel.Information,   new EventId(2, "Event #2"),         "String state", null                    ).SetName("{m}(String state)"),
                new TestCaseData(   LogLevel.Warning,       new EventId(3, "Event #3"),         int.MaxValue,   null                    ).SetName("{m}(Numeric state)"),
                new TestCaseData(   LogLevel.Error,         new EventId(4, "Event #4"),         null,           TestException.Create()  ).SetName("{m}(With exception)")
            };

        [TestCaseSource(nameof(LoggerHasMaterialized_TestCaseData))]
        public void LoggerHasMaterialized_InvokesLoggerLog(
                LogLevel    logLevel,
                EventId     eventId,
                object?     state,
                Exception?  exception)
            => GetType()
                .GetMethod(nameof(LoggerHasMaterialized_InvokesLoggerLog), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(state?.GetType() ?? typeof(object))
                .Invoke(null, new[] { logLevel, eventId, state, exception });

        private static void LoggerHasMaterialized_InvokesLoggerLog<TState>(
            LogLevel    logLevel,
            EventId     eventId,
            TState      state,
            Exception?  exception)
        {
            var mockLogger = new Mock<ILogger>();

            var uut = new Uut(() => mockLogger.Object);

            uut.EnsureMaterialized();

            var formatter = new Func<TState, Exception?, string>((_, _) => string.Empty);

            uut.Log(
                logLevel:   logLevel,
                eventId:    eventId,
                state:      state,
                exception:  exception,
                formatter:  formatter);

            mockLogger.Verify(
                x => x.Log(logLevel, eventId, state, exception, formatter),
                Times.Once);
        }
    }
}
