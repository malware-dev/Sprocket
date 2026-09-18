namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Something that starts a sequence.
    /// </summary>
    /// <remarks>
    /// A trigger belongs to the system rather than to the sequence it runs, because a sequence is a
    /// procedure and a procedure is not a reason for anything to happen. Two triggers may run one
    /// sequence, which is what a trigger belonging to a sequence could never say.
    /// </remarks>
    public abstract class Trigger
    {
        protected Trigger()
        {
            Annotation = "";
        }

        /// <summary>The number, within the system, of the sequence this is about.</summary>
        public int Runs { get; set; }

        /// <summary>
        /// Whether this stops that sequence rather than starting it.
        /// </summary>
        /// <remarks>
        /// The other thing a person wants a button for, and the one a sequence cannot do for itself:
        /// a cycle half way through has no line in it that says "unless somebody says otherwise".
        /// Stopping abandons what it was doing, which is what the switch already does - a button is
        /// simply the reachable version of it.
        /// </remarks>
        public bool Stops { get; set; }

        /// <summary>
        /// What somebody wrote about this trigger, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// A trigger has no name, so this is what a player setting the system up reads to know which of
        /// their buttons it means: "the call button, by the inner door".
        /// </remarks>
        public string Annotation { get; set; }
    }
}
