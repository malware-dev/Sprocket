namespace Sprocket.ModSupport
{
    /// <summary>Sprocket stopping a task, and why.</summary>
    /// <remarks>A stop is never optional: Sprocket has let go, and nothing the task reports after it reaches the run.</remarks>
    public sealed class SprocketCancel
    {
        /// <summary>Sprocket stopping a task for <paramref name="cause" />.</summary>
        public SprocketCancel(SprocketCancelCause cause)
        {
            Cause = cause;
        }

        /// <summary>Why Sprocket stops the task, so the mod can stop it the way that fits, such as holding still rather than dropping.</summary>
        public SprocketCancelCause Cause { get; }
    }
}
