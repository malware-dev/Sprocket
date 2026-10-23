namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>How a tip stays open, the variants of the trial an admin chooses between.</summary>
    public enum TipKeeping
    {
        /// <summary>The pointer can move from the element onto its tip, which stays while the pointer is on either.</summary>
        Stay,

        /// <summary>A tip is an ordinary tooltip, and stays for the pointer to move onto it only while Alt is held.</summary>
        Alt
    }
}
