
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Which of the three ways a step reaches a block.</summary>
    /// <remarks>
    /// Part of what names a member, alongside its id. The id alone can name two or three things: a
    /// switch's value and the game's act that flips it share one, and nothing stops one of Sprocket's
    /// own words from being spelled like one of the game's.
    /// </remarks>
    public enum Form
    {
        /// <summary>One of Sprocket's own actions, written qualified by what adds it: <c>sprocket.close</c>.</summary>
        Native,

        /// <summary>One of the game's actions, written by the game's own id alone.</summary>
        Action,

        /// <summary>One of the block's values, by the game's id, written with <c>set</c>.</summary>
        Value
    }
}
