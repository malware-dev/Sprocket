
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Which of the three ways a step reaches a block.</summary>
    /// <remarks>Part of what names a member, with its id: a switch's value and the game's act that flips it share one id.</remarks>
    public enum ReachKind
    {
        /// <summary>One of Sprocket's own actions, written qualified by what adds it: <c>ext.close</c>.</summary>
        Native,

        /// <summary>One of the game's actions, written by the game's own id alone.</summary>
        Action,

        /// <summary>One of the block's values, by the game's id, written with <c>set</c>.</summary>
        Value
    }
}
