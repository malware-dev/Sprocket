namespace Reactor.Host.SpaceEngineers.Core.Sequences
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

        /// <summary>The number, within the system, of the sequence this starts.</summary>
        public int Runs { get; set; }

        /// <summary>
        /// What somebody wrote about this trigger, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// A trigger has no name, so this is what a player fitting the system reads to know which of
        /// their buttons it means: "the call button, by the inner door".
        /// </remarks>
        public string Annotation { get; set; }
    }
}
