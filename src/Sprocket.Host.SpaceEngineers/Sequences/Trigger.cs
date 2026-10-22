
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Something that starts a step: runs or stops a sequence, or acts on some parts itself.
    /// </summary>
    public abstract class Trigger
    {
        /// <summary>A trigger with nothing written about it yet.</summary>
        protected Trigger()
        {
            Annotation = "";
        }

        /// <summary>The step it starts: a <see cref="Run" /> that runs or stops a sequence, or an <see cref="Act" /> of its own.</summary>
        public Step Does { get; set; }

        /// <summary>The number, within the system, of the sequence it runs or stops, or 0 where it acts itself.</summary>
        public int Runs
        {
            get
            {
                var run = Does as Run;
                return run?.Number ?? 0;
            }
        }

        /// <summary>
        /// Whether it stops that sequence rather than starting it.
        /// </summary>
        public bool Stops
        {
            get
            {
                var run = Does as Run;
                return run != null && run.Stops;
            }
        }

        /// <summary>The act it does itself, or null where it runs or stops a sequence.</summary>
        public Act Acts => Does as Act;

        /// <summary>The state it sets itself, or null where it does something else.</summary>
        public SetState Sets => Does as SetState;

        /// <summary>Whether this is about <paramref name="sequence" />: runs or stops it, or is the trigger whose own act it runs.</summary>
        public bool IsAbout(Sequence sequence)
        {
            if (!sequence.IsAlone)
                return Runs == sequence.Number;

            var act = Acts;
            return act != null && sequence.Steps.Count == 1 && sequence.Steps[0] == act;
        }

        /// <summary>
        /// What somebody wrote about this trigger, in their own words, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }
    }
}
