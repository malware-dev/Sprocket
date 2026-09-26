using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Input
{
    /// <summary>Something that points at a place on the screen and can press it.</summary>
    public interface IPointer
    {
        /// <summary>Whether there is a pointer on the surface at all this frame.</summary>
        bool IsAvailable { get; }

        /// <summary>Where it is, in the same UI units everything else is laid out in.</summary>
        Vector2 Position { get; }

        /// <summary>Whether the press is held down right now.</summary>
        bool IsPressed { get; }

        /// <summary>Whether the press began this frame.</summary>
        bool WasPressed { get; }
    }
}