
namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// One part of what somebody said an action's parameters are.
    /// </summary>
    public abstract class ParameterPiece
    {
        /// <summary>How many of the action's parameters this accounts for.</summary>
        public abstract int Span { get; }

        /// <summary>
        /// What sort of thing it is, or empty for a plain value typed as it is written.
        /// </summary>
        public abstract string Kind { get; }
    }
}
