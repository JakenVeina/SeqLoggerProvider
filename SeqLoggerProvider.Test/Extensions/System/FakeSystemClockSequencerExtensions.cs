namespace System
{
    internal static class FakeSystemClockSequencerExtensions
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
