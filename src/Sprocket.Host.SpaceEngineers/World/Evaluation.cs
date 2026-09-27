
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>How far Sprocket has checked a kind of block.</summary>
    /// <remarks>
    /// What a block offers is read from the game and tidied by rules that know nothing of the block,
    /// so a block nobody has looked at can offer something doubled, missing or oddly named. This is
    /// what says so, and it is declared per kind: nothing is ever concluded to have been checked.
    /// </remarks>
    public enum Evaluation
    {
        /// <summary>Nobody has checked it, which is where every block starts, a modded one included.</summary>
        Pending,

        /// <summary>Checked in part: some of what it offers has been gone through and some not.</summary>
        Partial,

        /// <summary>Checked through.</summary>
        Completed
    }
}
