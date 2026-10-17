
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that starts another sequence of the same system, or stops it.
    /// </summary>
    /// <remarks>
    /// By number, because that is what a sequence is. A script may name one instead - a person writing
    /// one by hand has the name in front of them - and the reader turns that into the number.
    /// <para>
    /// It starts the sequence and carries on, and an <see cref="Await" /> below it waits for that sequence
    /// to finish. A sequence that is already running is not started twice; it records the miss instead.
    /// </para>
    /// </remarks>
    public sealed class Run : Step
    {
        /// <summary>A step that runs the sequence <paramref name="number" /> of the same system.</summary>
        public Run(int number)
        {
            Number = number;
        }

        /// <summary>A step that stops sequence <paramref name="number" /> where <paramref name="stops" /> says so, and starts it otherwise.</summary>
        public Run(int number, bool stops)
        {
            Number = number;
            Stops = stops;
        }

        /// <summary>The number, within the system, of the sequence to start or stop.</summary>
        public int Number { get; set; }

        /// <summary>Whether it stops the sequence where it is running, rather than starting it.</summary>
        public bool Stops { get; set; }

        /// <summary>
        /// The name a script used instead of a number, until the system has been read.
        /// </summary>
        /// <remarks>
        /// A sequence may name one written below it, so a name cannot be turned into a number while
        /// the system is still being read. Empty everywhere else, including in everything Sprocket
        /// writes, because the writer writes the number.
        /// </remarks>
        public string Named { get; set; }
    }
}
