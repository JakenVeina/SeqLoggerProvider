using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Utilities
{
    public class FakeSystemClockSequencer
    {
        public FakeSystemClockSequencer(DateTimeOffset start)
        {
            _actionRegistrations = new();

            _systemClock = new()
            {
                Now = start
            };
        }

        public ISystemClock SystemClock
            => _systemClock;

        public void AddAction(TimeSpan delay, Func<ValueTask> action)
            => _actionRegistrations.Add(new()
            {
                Action  = action,
                Delay   = delay
            });

        public async Task RunAsync()
        {
            foreach (var registration in _actionRegistrations)
            {
                _systemClock.Now += registration.Delay;
                await Task.Yield();

                await registration.Action.Invoke();
            }
        }

        private readonly List<ActionRegistration>   _actionRegistrations;
        private readonly FakeSystemClock            _systemClock;

        private struct ActionRegistration
        {
            public Func<ValueTask> Action { get; init; }
            
            public TimeSpan Delay { get; init; }
        }
    }
}
