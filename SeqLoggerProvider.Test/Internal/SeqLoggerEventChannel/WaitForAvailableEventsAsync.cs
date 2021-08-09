using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEventChannel;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEventChannel
{
    [TestFixture]
    public class WaitForAvailableEventsAsync
    {
        [Test]
        public async Task EventIsAvailable_CompletesImmediately()
        {
            var uut = new Uut();

            var @event = new SeqLoggerEvent<object?>(
                categoryName:       "SeqLoggerProvider.Test.SeqLoggerEventChannel.WaitForAvailableEventsAsync.EventIsAvailable",
                eventId:            new(1, "EventIsAvailableExecuted"),
                exception:          null,
                formatter:          (_, _) => "This is a log event",
                logLevel:           LogLevel.Information,
                occurredUtc:        default,
                scopeStatesBuffer:  new List<object>(),
                state:              default);

            uut.WriteEvent(@event);

            var result = uut.WaitForAvailableEventsAsync(CancellationToken.None);

            result.IsCompletedSuccessfully.ShouldBeTrue();

            await result;
        }

        [Test]
        public async Task EventIsNotAvailable_CompletesAsynchronouslyAfterWriteEvent()
        {
            var uut = new Uut();

            var result = uut.WaitForAvailableEventsAsync(CancellationToken.None);
            
            result.IsCompleted.ShouldBeFalse();
            
            var @event = new SeqLoggerEvent<object?>(
                categoryName:       "SeqLoggerProvider.Test.SeqLoggerEventChannel.WaitForAvailableEventsAsync.EventIsNotAvailable",
                eventId:            new(1, "EventIsNotAvailableExecuted"),
                exception:          null,
                formatter:          (_, _) => "This is a log event",
                logLevel:           LogLevel.Information,
                occurredUtc:        default,
                scopeStatesBuffer:  new List<object>(),
                state:              default);

            uut.WriteEvent(@event);

            result.IsCompleted.ShouldBeFalse();

            await result;
        }
    }
}
