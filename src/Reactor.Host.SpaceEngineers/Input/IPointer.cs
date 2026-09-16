using VRageMath;

namespace Reactor.Host.SpaceEngineers.Input
{
    /// <summary>Something that points at a place on the screen and can press it.</summary>
    /// <remarks>
    /// The two targets produce this in completely different ways and nothing above this line should know
    /// which. On screen the mouse drives it, and the game's look controls have to be taken away for the
    /// duration; on a text panel the player's own gaze drives it, by pointing their head at the block, so
    /// taking look away would remove the only means of aiming. That is why suspending look is an argument
    /// somewhere rather than a rule everywhere.
    /// </remarks>
    public interface IPointer
    {
        /// <summary>Whether there is a pointer on the surface at all this frame.</summary>
        bool IsAvailable { get; }

        /// <summary>Where it is, in the same UI units everything else is laid out in.</summary>
        Vector2 Position { get; }

        /// <summary>Whether the press is held down right now.</summary>
        bool IsPressed { get; }

        /// <summary>Whether the press began this frame, which is what a button acts on.</summary>
        bool WasPressed { get; }
    }
}
