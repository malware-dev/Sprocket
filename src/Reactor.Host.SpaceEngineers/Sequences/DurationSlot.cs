namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A length of time, in ticks.
    /// </summary>
    /// <remarks>
    /// Ticks, because that is what the runtime counts. Nobody says "wait three hundred ticks", so a
    /// target says it in whatever units its language says time in - and which units those are, and
    /// where the plural falls, is not the same question twice.
    /// </remarks>
    public sealed class DurationSlot : SentenceSlot
    {
        public DurationSlot(int ticks, int command) : base(command)
        {
            Ticks = ticks;
        }

        public int Ticks { get; private set; }

        /// <summary>Never. A duration always has a value, even if it is none.</summary>
        public override bool IsEmpty => false;
    }
}
