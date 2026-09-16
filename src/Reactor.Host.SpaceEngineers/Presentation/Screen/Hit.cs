using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Ui;

namespace Reactor.Host.SpaceEngineers.Presentation.Screen
{
    /// <summary>Somewhere that can be pressed, and what pressing it means.</summary>
    /// <remarks>
    /// A hit carrying a <see cref="Question" /> is not a command to send, it is a command to send an
    /// answer to, and it names who to open the box beneath. Keeping all of that in one structure means the
    /// press path has one shape rather than three.
    /// </remarks>
    public struct Hit
    {
        public RectangleF Bounds;
        public int Command;
        public string Question;

        /// <summary>Whether this sits inside the scrolling region and can therefore be out of sight.</summary>
        public bool Scrolled;

        /// <summary>What the question should hang under. Null when it has no particular home.</summary>
        public Element Anchor;

        /// <summary>
        /// A box this hit puts the keyboard into, rather than a command to send at once.
        /// </summary>
        /// <remarks>
        /// A search box is the case: pressing it starts typing, and every keystroke sends the text to
        /// <see cref="Command" />. Core re-issues command ids on every rebuild, so the id is read at the
        /// moment of use rather than captured when the box was made.
        /// </remarks>
        public TextBox Box;

        /// <summary>Something to hang over the screen when this is pressed, anchored to it.</summary>
        public Element Overlay;

        /// <summary>
        /// Whether this is a switch rather than a command, and what state to put it in.
        /// </summary>
        /// <remarks>
        /// A switch is not a button that happens to look different. Core takes the state it should be in,
        /// so two presses arriving out of order still agree about where it ends up, where two invocations
        /// would each mean "the other one from whatever you have".
        /// </remarks>
        public bool Toggles;

        public bool ToggleTo;

        /// <summary>Whether pressing this a second time means "and move on".</summary>
        public bool Confirms;

        /// <summary>
        /// Whether this asks to leave the editor rather than to do anything inside it.
        /// </summary>
        /// <remarks>
        /// A flag and not a command, because there is no command: core has no idea it is being looked at
        /// and closing a window is not a thing it could be asked to do. The host reads
        /// <see cref="EditorView.TakeClose" /> after a press and decides what leaving means.
        /// </remarks>
        public bool Closes;

        /// <summary>
        /// What to light up while the pointer is resting here. Null for a hit with nothing to light.
        /// </summary>
        /// <remarks>
        /// The element rather than the rectangle, because the rectangle is already in <see cref="Bounds" />
        /// and drawing over it would put a wash on top of the words. An element lights itself, from the
        /// inside, using colours it chose - see <see cref="Element.Highlight" />.
        /// </remarks>
        public Element Hot;

        /// <summary>
        /// What to say about this while the pointer rests on it. Null for most things.
        /// </summary>
        /// <remarks>
        /// <b>Only for a control that cannot say what it is.</b> A row reading "Airlock 1" needs no note
        /// telling you it is Airlock 1, and a screen that explained every word on it would be a screen
        /// nobody could scan. What earns one is a picture: a bin, an arrow that turns back on itself, a
        /// switch whose two words say the state rather than the act.
        /// <para>
        /// The wording is the host's, except where core already publishes the thing being named - undo
        /// borrows <c>UndoText</c>, which exists precisely because "Undo" alone asks somebody to remember
        /// what they just did. Nothing here adds a string to core; who should own that wording is Design
        /// decision 11 and is deliberately still open.
        /// </para>
        /// </remarks>
        public string Tip;

        public Hit(RectangleF bounds, int command)
        {
            Bounds = bounds;
            Command = command;
            Question = null;
            Scrolled = true;
            Anchor = null;
            Box = null;
            Overlay = null;
            Toggles = false;
            ToggleTo = false;
            Confirms = false;
            Closes = false;
            Hot = null;
            Tip = null;
        }

        public Hit(RectangleF bounds, int command, string question)
        {
            Bounds = bounds;
            Command = command;
            Question = question;
            Scrolled = true;
            Anchor = null;
            Box = null;
            Overlay = null;
            Toggles = false;
            ToggleTo = false;
            Confirms = false;
            Closes = false;
            Hot = null;
            Tip = null;
        }

        public Hit(RectangleF bounds, int command, string question, Element anchor)
        {
            Bounds = bounds;
            Command = command;
            Question = question;
            Scrolled = true;
            Anchor = anchor;
            Box = null;
            Overlay = null;
            Toggles = false;
            ToggleTo = false;
            Confirms = false;
            Closes = false;
            Hot = null;
            Tip = null;
        }

        /// <summary>
        /// A hit on <paramref name="element" />, which lights up while the pointer rests on it.
        /// </summary>
        /// <remarks>
        /// Takes the element rather than its rectangle because it needs both and they must agree. Passing
        /// one thing's bounds and another thing's element is a class of mistake this makes unavailable.
        /// </remarks>
        /// <summary>A control that puts the editor away.</summary>
        public static Hit Leaves(Element element)
        {
            var hit = new Hit(element.Bounds, 0);
            hit.Scrolled = false;
            hit.Closes = true;
            hit.Hot = element;
            return hit;
        }

        public static Hit On(Element element, int command)
        {
            var hit = new Hit(element.Bounds, command);
            hit.Hot = element;
            return hit;
        }

        /// <summary>A hit on an element in the chrome, which is never scrolled away.</summary>
        public static Hit OnFixed(Element element, int command)
        {
            Hit hit = On(element, command);
            hit.Scrolled = false;
            return hit;
        }

        /// <summary>A hit that puts a switch into <paramref name="on" />.</summary>
        public static Hit Switch(RectangleF bounds, int command, bool on)
        {
            var hit = new Hit(bounds, command);
            hit.Toggles = true;
            hit.ToggleTo = on;
            return hit;
        }

        /// <summary>A hit that opens <paramref name="list" /> over the screen, under itself.</summary>
        public static Hit Opens(RectangleF bounds, Element list)
        {
            var hit = new Hit(bounds, 0);
            hit.Overlay = list;
            hit.Anchor = null;
            return hit;
        }

        /// <summary>A hit that puts the keyboard into <paramref name="box" />.</summary>
        public static Hit Typing(RectangleF bounds, int command, TextBox box)
        {
            var hit = new Hit(bounds, command);
            hit.Box = box;
            return hit;
        }

        /// <summary>A hit in the chrome, which is never scrolled away.</summary>
        public static Hit Fixed(RectangleF bounds, int command)
        {
            var hit = new Hit(bounds, command);
            hit.Scrolled = false;
            return hit;
        }

        public static Hit Fixed(RectangleF bounds, int command, string question)
        {
            var hit = new Hit(bounds, command, question);
            hit.Scrolled = false;
            return hit;
        }
    }
}
