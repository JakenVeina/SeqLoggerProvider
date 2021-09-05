using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLogger
{
    [TestFixture]
    public class Log
    {
        [Test]
        public void EntryLoadThrowsException_ReturnsEntryToPool()
        {
            using var testContext = new TestContext()
            {
                EventLoadException = new TestException()
            };

            var uut = testContext.BuildUut();

            var mockOnCreatingLogEntry = new Mock<Action>();
            uut.CreatingLogEntry += mockOnCreatingLogEntry.Object;

            var formatter = Mock.Of<Func<object, Exception?, string>>();

            uut.Log(
                logLevel:   LogLevel.Debug,
                eventId:    new EventId(1, "Event"),
                state:      new object(),
                exception:  null,
                formatter:  formatter);

            mockOnCreatingLogEntry.Verify(
                x => x.Invoke(),
                Times.Once);

            var @event = testContext.EntryPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerEntry>();

            testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(@event);
            testContext.EntryChannelWriter.Items.ShouldBeEmpty();
        }

        public static readonly IReadOnlyList<TestCaseData> Otherwise_TestCaseData
            = new[]
            {
                /*                  categoryName,   now,                                    logLevel,               eventId,                                    state,          exception               */
                new TestCaseData(   string.Empty,   DateTimeOffset.MinValue,                LogLevel.Trace,         new EventId(int.MinValue,   string.Empty),  null,           null                    ).SetName("{m}(Min Values)"),
                new TestCaseData(   string.Empty,   DateTimeOffset.MaxValue,                LogLevel.Critical,      new EventId(int.MaxValue,   string.Empty),  null,           null                    ).SetName("{m}(Max Values)"),
                new TestCaseData(   "Category #1",  DateTimeOffset.FromUnixTimeSeconds(1),  LogLevel.Debug,         new EventId(3,              "Event #4"),    "String State", null                    ).SetName("{m}(String State)"),
                new TestCaseData(   "Category #5",  DateTimeOffset.FromUnixTimeSeconds(6),  LogLevel.Information,   new EventId(7,              "Event #8"),    9,              null                    ).SetName("{m}(Numeric State)"),
                new TestCaseData(   "Category #10", DateTimeOffset.FromUnixTimeSeconds(11), LogLevel.Warning,       new EventId(12,             "Event #13"),   new object(),   null                    ).SetName("{m}(Object State)"),
                new TestCaseData(   "Category #14", DateTimeOffset.FromUnixTimeSeconds(15), LogLevel.Error,         new EventId(16,             "Event #17"),   null,           TestException.Create()  ).SetName("{m}(With Exception)")
            };

        [TestCaseSource(nameof(Otherwise_TestCaseData))]
        public void Otherwise_WritesEntryToChannel(
                string          categoryName,
                DateTimeOffset  now,
                LogLevel        logLevel,
                EventId         eventId,
                object?         state,
                Exception?      exception)
            => GetType()
                .GetMethod(nameof(Otherwise_WritesEntryToChannel), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(state?.GetType() ?? typeof(object))
                .Invoke(null, new[] { categoryName, now, logLevel, eventId, state, exception });

        private static void Otherwise_WritesEntryToChannel<TState>(
            string          categoryName,
            DateTimeOffset  now,
            LogLevel        logLevel,
            EventId         eventId,
            TState          state,
            Exception?      exception)
        {
            using var testContext = new TestContext()
            {
                CategoryName = categoryName
            };

            testContext.Options.Value.GlobalFields = new Dictionary<string, string>();

            testContext.SystemClock.Now = now;

            var uut = testContext.BuildUut();

            var mockOnCreatingLogEntry = new Mock<Action>();
            uut.CreatingLogEntry += mockOnCreatingLogEntry.Object;

            var formatter = Mock.Of<Func<TState, Exception?, string>>();

            uut.Log(
                logLevel:   logLevel,
                eventId:    eventId,
                state:      state,
                exception:  exception,
                formatter:  formatter);

            mockOnCreatingLogEntry.Verify(
                x => x.Invoke(),
                Times.Once);

            var entry = testContext.EntryPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerEntry>();

            var invocation = entry.LoadInvocations.ShouldHaveSingleItem();
            invocation.CategoryName.ShouldBe(categoryName);
            invocation.EventId.Id.ShouldBe(eventId.Id);
            invocation.EventId.Name.ShouldBe(eventId.Name);
            invocation.Exception.ShouldBeSameAs(exception);
            invocation.Formatter.ShouldBeSameAs(formatter);
            invocation.GlobalFields.ShouldBeSameAs(testContext.Options.Value.GlobalFields);
            invocation.LogLevel.ShouldBe(logLevel);
            invocation.OccurredUtc.ShouldBe(now.UtcDateTime);
            invocation.Options.ShouldBeSameAs(testContext.JsonSerializerOptions.Value);
            invocation.ScopeProvider.ShouldBeSameAs(testContext.ScopeProvider);
            invocation.State.ShouldBe(state);

            testContext.EntryPool.ReturnedObjects.ShouldBeEmpty();
            testContext.EntryChannelWriter.Items.ShouldHaveSingleItem().ShouldBeSameAs(entry);
        }
    }
}
