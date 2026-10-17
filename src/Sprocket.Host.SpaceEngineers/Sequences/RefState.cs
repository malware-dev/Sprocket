
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// How well a reference is holding.
    /// </summary>
    public enum RefState
    {
        /// <summary>The strongest key found it.</summary>
        Bound,

        /// <summary>
        /// The strongest key failed and a weaker one found something.
        /// </summary>
        /// <remarks>Never rebinds by itself: it is a proposal a person accepts, and a sequence holding one does not run.</remarks>
        Adrift,

        /// <summary>Nothing matched.</summary>
        Lost
    }
}
