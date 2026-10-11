namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>What an installation still has to do about one of its blueprint's slots.</summary>
    public enum SlotMark
    {
        /// <summary>Nothing: its blocks fill the slot.</summary>
        None = 0,

        /// <summary>It has not filled the slot.</summary>
        New,

        /// <summary>The slot was split or merged, and the blocks it had for the slot are set aside.</summary>
        Changed,

        /// <summary>A block it filled the slot with is gone from the construct.</summary>
        Lost
    }
}
