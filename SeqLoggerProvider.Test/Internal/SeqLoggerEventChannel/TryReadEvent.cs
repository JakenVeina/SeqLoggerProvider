using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEventChannel;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEventChannel
{
    [TestFixture]
    public class TryReadEvent
    {
        [Test]
        public void EventIsAvailable_ReturnsEvent()
        {
            var uut = new Uut();

            var @event = new SeqLoggerEvent<object?>(
                categoryName:       "SeqLoggerProvider.Test.SeqLoggerEventChannel.TryReadEvent.EventIsAvailable",
                eventId:            new(1, "EventIsAvailableExecuted"),
                exception:          null,
                formatter:          (_, _) => "This is a log event",
                logLevel:           LogLevel.Information,
                occurredUtc:        default,
                scopeStatesBuffer:  new List<object>(),
                state:              default);

            uut.WriteEvent(@event);

            var result1 = uut.TryReadEvent();

            result1.ShouldBeSameAs(@event);

            var result2 = uut.TryReadEvent();

            result2.ShouldBeNull();
        }

        [Test]
        public void EventIsNotAvailable_ReturnsNull()
        {
            var uut = new Uut();

            var result = uut.TryReadEvent();

            result.ShouldBeNull();
        }

        [Test]
        public void EventsAreAvailable_ReturnsEvents()
        {
            var uut = new Uut();

            var event1 = new SeqLoggerEvent<object?>(
                categoryName:       "SeqLoggerProvider.Test.SeqLoggerEventChannel.TryReadEvent.EventsAreAvailable",
                eventId:            new(1, "EventsAreAvailableExecuted"),
                exception:          null,
                formatter:          (_, _) => "This is a log event",
                logLevel:           LogLevel.Information,
                occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(1).UtcDateTime,
                scopeStatesBuffer:  new List<object>(),
                state:              default);

            var event2 = new SeqLoggerEvent<object?>(
                categoryName:       "SeqLoggerProvider.Test.SeqLoggerEventChannel.TryReadEvent.EventsAreAvailable",
                eventId:            new(2, "EventsAreAvailableExecuted"),
                exception:          null,
                formatter:          (_, _) => "This is another log event",
                logLevel:           LogLevel.Information,
                occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(2).UtcDateTime,
                scopeStatesBuffer:  new List<object>(),
                state:              default);

            var event3 = new SeqLoggerEvent<object?>(
                categoryName:       "SeqLoggerProvider.Test.SeqLoggerEventChannel.TryReadEvent.EventsAreAvailable",
                eventId:            new(3, "EventsAreAvailableExecuted"),
                exception:          null,
                formatter:          (_, _) => "This is a third log event",
                logLevel:           LogLevel.Information,
                occurredUtc:        DateTimeOffset.FromUnixTimeSeconds(3).UtcDateTime,
                scopeStatesBuffer:  new List<object>(),
                state:              default);

            uut.WriteEvent(event1);
            uut.WriteEvent(event2);
            uut.WriteEvent(event3);

            var result1 = uut.TryReadEvent();

            result1.ShouldBeSameAs(event1);

            var result2 = uut.TryReadEvent();

            result2.ShouldBeSameAs(event2);

            var result3 = uut.TryReadEvent();

            result3.ShouldBeSameAs(event3);

            var result4 = uut.TryReadEvent();

            result4.ShouldBeNull();
        }

    }
}
