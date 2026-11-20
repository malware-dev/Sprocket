namespace Sprocket.ModSupport
{
    /// <summary>Why Sprocket wants a task stopped.</summary>
    public enum SprocketCancelCause
    {
        /// <summary>The sequence was stopped: a player pressed stop, or the program stopped it with a stop step or a stopping trigger.</summary>
        Stopped,

        /// <summary>A player switched the logic system off.</summary>
        SwitchedOff,

        /// <summary>The block is gone.</summary>
        BlockGone,

        /// <summary>The world is unloading.</summary>
        Unloaded,

        /// <summary>The program ended the sequence with <c>abort</c>.</summary>
        Aborted,

        /// <summary>Something else in the sequence failed.</summary>
        Failed
    }
}
