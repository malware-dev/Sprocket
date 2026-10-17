using VRage.Input;
using VRageMath;

namespace Lumel.Input
{
    /// <summary>What an input root tells an element: presses, keys, typed characters, and the keyboard coming and going.</summary>
    /// <remarks>Implemented explicitly by <see cref="Element" />, so input reaches an element only from an input root or a deliberate cast.</remarks>
    public interface IInputTarget
    {
        /// <summary>A press has begun at <paramref name="point" />, the <paramref name="presses" />th in quick succession.</summary>
        void PointerDown(Vector2 point, int presses, bool shift, bool ctrl);

        /// <summary>The pointer has moved to <paramref name="point" /> during a press begun on the element.</summary>
        void PointerMove(Vector2 point);

        /// <summary>A press begun on the element has been released at <paramref name="point" />.</summary>
        void PointerUp(Vector2 point);

        /// <summary>Offers <paramref name="key" />, answering whether the element took it.</summary>
        bool Key(InputRoot input, MyKeys key, bool shift, bool ctrl);

        /// <summary>Gives the element the typed character <paramref name="c" />.</summary>
        void Text(char c);

        /// <summary>The element now holds the keyboard.</summary>
        void Focus();

        /// <summary>The element no longer holds the keyboard.</summary>
        void Blur();
    }
}
