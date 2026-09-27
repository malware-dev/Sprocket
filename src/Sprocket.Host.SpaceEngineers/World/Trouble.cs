
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What a block says is stopping it, and whether waiting will help.
    /// </summary>
    /// <remarks>
    /// Two answers rather than one, because the two demand opposite things of a sequence. A room
    /// with no oxygen is worth waiting on: somebody reattaches a tank and the airlock finishes by
    /// itself. A vent another sequence has just sent the other way is not, because nothing about
    /// waiting makes the conflict go away and the step it is waiting for is no longer being
    /// pursued by anything.
    /// </remarks>
    public struct Trouble
    {
        public static readonly Trouble None = new Trouble();

        /// <summary>Something to wait through: said, and still worth holding for.</summary>
        public static Trouble Waiting(string said)
        {
            return new Trouble(said, false);
        }

        /// <summary>Something to give up on: said, and the run ends here.</summary>
        public static Trouble Fatal(string said)
        {
            return new Trouble(said, true);
        }

        Trouble(string said, bool fatal) : this()
        {
            Said = said;
            Ends = fatal;
        }

        public string Said { get; private set; }

        /// <summary>Whether the run should end rather than go on waiting.</summary>
        public bool Ends { get; private set; }

        public bool Exists
        {
            get { return !string.IsNullOrEmpty(Said); }
        }
    }
}
