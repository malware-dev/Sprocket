namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that starts another sequence of the same system.
    /// </summary>
    /// <remarks>
    /// By number, because that is what a sequence is. A script may name one instead - a person writing
    /// one by hand has the name in front of them - and the reader turns that into the number.
    /// <para>
    /// It starts the sequence and carries on. <see cref="Await" /> waits for the actions started above
    /// it, not for a sequence somebody called, so waiting for one is undesigned - see the concurrency
    /// work. A sequence that is already running is not started twice; it records the miss instead.
    /// </para>
    /// </remarks>
    public sealed class Run : Step
    {
        public Run(int number)
        {
            Number = number;
        }

        /// <summary>The number, within the system, of the sequence to start.</summary>
        public int Number { get; set; }

        /// <summary>
        /// The name a script used instead of a number, until the system has been read.
        /// </summary>
        /// <remarks>
        /// A sequence may name one written below it, so a name cannot be turned into a number while
        /// the system is still being read. Empty everywhere else, including in everything Reactor
        /// writes, because the writer writes the number.
        /// </remarks>
        internal string Named { get; set; }
    }
}
