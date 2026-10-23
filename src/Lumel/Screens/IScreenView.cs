using Lumel.Drawing;
using Lumel.Input;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>What a <see cref="GameScreen" /> shows: it draws itself in the screen's room and is handed the screen's input.</summary>
    /// <remarks>Every point it is given is in UI units, the same units it is laid out and drawn in.</remarks>
    public interface IScreenView
    {
        /// <summary>Where keys and typed characters go while something in the view holds the keyboard.</summary>
        InputRoot Input { get; }

        /// <summary>Where the pointer is, set before anything else each frame.</summary>
        Vector2 Pointer { set; }

        /// <summary>Asks whether the host's keep key, which keeps a tip open, is held now; given once, as the screen opens.</summary>
        System.Func<bool> IsKeepKeyHeld { set; }

        /// <summary>Whether a caret is drawn this half of its blink, which the screen keeps from its own clock.</summary>
        bool CaretVisible { set; }

        /// <summary>Whether a question is open, which takes Enter as its default answer and no other key.</summary>
        bool IsQuestioning { get; }

        /// <summary>Takes the open question's default answer, and answers whether it had one.</summary>
        bool TakeDefault();

        /// <summary>A press beginning at <paramref name="point" />, the <paramref name="presses" />th in quick succession, with <paramref name="shift" /> and <paramref name="ctrl" /> saying which of those keys were held.</summary>
        void PointerDown(Vector2 point, int presses, bool shift, bool ctrl);

        /// <summary>The pointer moving to <paramref name="point" /> while it is held down.</summary>
        void PointerMove(Vector2 point);

        /// <summary>The press ending at <paramref name="point" />.</summary>
        void PointerUp(Vector2 point);

        /// <summary>Whether something in the view asked for the screen to close since this was last asked, which asking forgets.</summary>
        bool TakeClose();

        /// <summary>The wheel turned with the pointer at <paramref name="point" />, by <paramref name="amount" /> UI units, positive down.</summary>
        void ScrollBy(Vector2 point, float amount);

        /// <summary>Escape pressed, answering whether the screen may close: false where the view used the key itself.</summary>
        bool Escape();

        /// <summary>Brings the view up to date before it is drawn, once a frame.</summary>
        void Update();

        /// <summary>Draws the view across <paramref name="bounds" />, the screen's room.</summary>
        void Draw(IGraphics g, RectangleF bounds);
    }
}
