namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>What an installation still has to do about one of its blueprint's slots.</summary>
    public enum SlotMark
    {
        /// <summary>Nothing: its blocks fill the slot.</summary>
        None = 0,

        /// <summary>It has not filled the slot.</summary>
        New,

        /// <summary>The slot's kind changed, and a block it filled the slot with no longer fits it.</summary>
        Changed,

        /// <summary>A block it filled the slot with is gone from the construct.</summary>
        Lost,

        /// <summary>A block it filled the slot with was found by its name alone, so it may be another block.</summary>
        Replaced
    }
}
