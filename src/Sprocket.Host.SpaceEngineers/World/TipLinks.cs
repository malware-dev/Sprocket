namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Where a link inside a tip shows its page, the variants of the trial an admin chooses between.</summary>
    public enum TipLinks
    {
        /// <summary>A press on the link shows its page in the same tip, with steps back and forward.</summary>
        Replace,

        /// <summary>Resting the pointer on the link opens its page in a tip of its own beside the first.</summary>
        Beside
    }
}
