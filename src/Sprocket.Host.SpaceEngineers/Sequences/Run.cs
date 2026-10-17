
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that starts another sequence of the same system, or stops it.
    /// </summary>
    /// <remarks>A sequence that is already running is not started twice; the miss is recorded instead.</remarks>
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
        /// <remarks>Empty once the system has been read; Sprocket always writes the number.</remarks>
        public string Named { get; set; }
    }
}
