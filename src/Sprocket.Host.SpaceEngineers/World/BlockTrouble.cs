
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What a block says is stopping it, and whether waiting will help.
    /// </summary>
    public struct BlockTrouble
    {
        /// <summary>No trouble.</summary>
        public static readonly BlockTrouble None = new BlockTrouble();

        /// <summary>Something to wait through: <paramref name="cause" />, about <paramref name="about" /> where it names something, and still worth holding for.</summary>
        public static BlockTrouble Waiting(Cause cause, string about = null)
        {
            return new BlockTrouble(cause, about, false);
        }

        /// <summary>Something to give up on: <paramref name="cause" />, and the run ends here.</summary>
        public static BlockTrouble Fatal(Cause cause)
        {
            return new BlockTrouble(cause, null, true);
        }

        /// <summary>Trouble read back from a save: <paramref name="cause" />, about <paramref name="about" />, whose run has already ended.</summary>
        public static BlockTrouble Kept(Cause cause, string about)
        {
            return new BlockTrouble(cause, about, true);
        }

        BlockTrouble(Cause cause, string about, bool fatal) : this()
        {
            Cause = cause;
            About = about;
            Ends = fatal;
        }

        /// <summary>What is stopping it.</summary>
        public Cause Cause { get; private set; }

        /// <summary>The thing the cause names, as the game names it, such as the gas a tank lacks; null where it names nothing.</summary>
        public string About { get; private set; }

        /// <summary>Whether the run should end rather than go on waiting.</summary>
        public bool Ends { get; private set; }

        /// <summary>Whether there is any trouble.</summary>
        public bool Exists => Cause != Cause.None;

        /// <summary>Whether <paramref name="other" /> says the same thing.</summary>
        public bool Same(BlockTrouble other)
        {
            return Cause == other.Cause && About == other.About;
        }
    }
}
