namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Why a trigger cannot start its sequence.</summary>
    public enum Unwired
    {
        /// <summary>Not known.</summary>
        Unknown,

        /// <summary>The button's slot is empty: somebody cleared it, and Sprocket does not write it again.</summary>
        Cleared,

        /// <summary>The button's slot holds something else.</summary>
        Taken,

        /// <summary>The trigger names no block.</summary>
        NoBlock,

        /// <summary>The trigger names blocks, but nothing that happens to them.</summary>
        NoMoment
    }
}
