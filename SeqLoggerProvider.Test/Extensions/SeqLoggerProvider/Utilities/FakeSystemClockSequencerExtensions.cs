using System;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Utilities
{
    public static class FakeSystemClockSequencerExtensions
    {
        public static FakeSystemClockSequencer DoAfter(this FakeSystemClockSequencer sequencer, TimeSpan delay, Action action)
        {
            sequencer.AddAction(delay, () =>
            {
                action.Invoke();
                return default;
            });

            return sequencer;
        }
    }
}
