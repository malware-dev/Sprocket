namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A step that ends the sequence it is in, at once, saying why where it has a message.</summary>
    public sealed class Abort : Step
    {
        /// <summary>The reason the sequence shows, as the player wrote it, or empty for none.</summary>
        public string Message { get; set; } = "";

        /// <summary>How loudly the sequence says it ended.</summary>
        public AbortLevel Level { get; set; }
    }
}
