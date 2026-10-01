namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What a block is doing, or what is stopping it, as shown beside it in a list of blocks.</summary>
    /// <remarks>What is stopping it comes first where there is anything: a vent that has stopped short goes on saying it is filling.</remarks>
    public struct Status
    {
        public static readonly Status None = new Status();

        Status(Condition condition, Trouble trouble) : this()
        {
            Condition = condition;
            Trouble = trouble;
        }

        /// <summary>What it is doing, where nothing stops it.</summary>
        public Condition Condition { get; private set; }

        /// <summary>What is stopping it, where anything is.</summary>
        public Trouble Trouble { get; private set; }

        /// <summary>Whether there is anything to say.</summary>
        public bool Exists => Condition != Condition.None || Trouble.Exists;

        /// <summary>A block doing <paramref name="condition" />.</summary>
        public static Status Of(Condition condition)
        {
            return new Status(condition, Trouble.None);
        }

        /// <summary>A block stopped by <paramref name="trouble" />.</summary>
        public static Status Of(Trouble trouble)
        {
            return new Status(Condition.None, trouble);
        }

        /// <summary>Whether <paramref name="other" /> says the same thing.</summary>
        public bool Same(Status other)
        {
            return Condition == other.Condition && Trouble.Same(other.Trouble);
        }
    }
}
