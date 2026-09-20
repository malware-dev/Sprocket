using System.Collections.Generic;
namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>What a press on one element does.</summary>
    /// <remarks>
    /// Held by the element it belongs to, so that finding what was pressed and finding out what pressing
    /// it means are the same act. <see cref="Layout.Means" /> says which of the fields below the rest
    /// of it is using.
    /// <para>
    /// A value rather than an object, because a view hands its controls their meaning again on every
    /// tick, and a screen's worth of those sixty times a second is not something to be allocating. The
    /// id of a command never moves - <c>SprocketEditor.Register</c> says why - but <b>which</b> command
    /// a control carries does: an undo with nothing to undo carries none, a switch carries the state it
    /// would put things into, and a row carries whatever is at its position now.
    /// </para>
    /// </remarks>
    public struct Deed
    {
        /// <summary>Nothing. What an element that cannot be pressed holds.</summary>
        public static readonly Deed Nothing = new Deed();

        public Means Means;

        /// <summary>What core is told, and what a typed or spoken answer is sent to.</summary>
        public int Command;

        /// <summary>What to ask, for <see cref="Means.Ask" />.</summary>
        public string Question;

        /// <summary>
        /// The box the keyboard goes into, for <see cref="Means.Type" />.
        /// </summary>
        /// <remarks>
        /// The box and not its words: a search box sends every keystroke, so what is read at the moment
        /// of use is the box's own text rather than anything captured when it was made.
        /// </remarks>
        public TextBox Box;

        /// <summary>What to hang over the screen, for <see cref="Means.Open" />.</summary>
        public Element Overlay;

        /// <summary>
        /// Which way to put a switch, for <see cref="Means.Switch" />.
        /// </summary>
        /// <remarks>
        /// The state to end up in, not "the other one from whatever you have", so two presses arriving
        /// out of order still agree about where it ends up.
        /// </remarks>
        public bool ToggleTo;

        /// <summary>Whether pressing this a second time means "this one, and move on".</summary>
        public bool Confirms;

        /// <summary>
        /// A control that sends <paramref name="command" />.
        /// </summary>
        /// <remarks>
        /// A command of zero is nothing to send, so it is nothing to press either: an undo with nothing
        /// to undo is drawn dim and answers to neither a press nor the pointer.
        /// </remarks>
        public static Deed Sends(int command)
        {
            if (command == 0)
                return Nothing;

            var deed = new Deed();
            deed.Means = Means.Send;
            deed.Command = command;
            return deed;
        }

        /// <summary>A switch that puts <paramref name="command" /> into <paramref name="on" />.</summary>
        /// <summary>
        /// A control that takes one of a list, the way whatever is held down says.
        /// </summary>
        /// <remarks>
        /// The way is not decided here. A deed is made afresh every time a control is dressed, and
        /// what is held down is known only when the press lands - so this says "ask at the time",
        /// and the screen that reads the keyboard answers it.
        /// </remarks>
        public static Deed Picks(int command)
        {
            var deed = new Deed();
            deed.Means = Means.Pick;
            deed.Command = command;
            deed.Asked = true;
            return deed;
        }

        /// <summary>
        /// The same, one way whatever is held down.
        /// </summary>
        /// <remarks>
        /// For the tick at the head of a row, which collects one at a time and means that whether or
        /// not anything is held: the box is the control for building a set up by hand.
        /// </remarks>
        public static Deed Picks(int command, Held held)
        {
            var deed = new Deed();
            deed.Means = Means.Pick;
            deed.Command = command;
            deed.Held = held;
            return deed;
        }

        /// <summary>Which way this takes what it takes, where it does not ask.</summary>
        public Held Held { get; private set; }

        /// <summary>Whether the way is whatever is held down rather than the one above.</summary>
        public bool Asked { get; private set; }

        public static Deed Switches(int command, bool on)
        {
            if (command == 0)
                return Nothing;

            var deed = new Deed();
            deed.Means = Means.Switch;
            deed.Command = command;
            deed.ToggleTo = on;
            return deed;
        }

        /// <summary>
        /// A control that asks <paramref name="question" /> and sends the answer to
        /// <paramref name="command" />.
        /// </summary>
        /// <remarks>
        /// With nothing to ask it is an ordinary control, which is what lets a view hand the same call
        /// a question core may or may not have given it.
        /// </remarks>
        /// <summary>
        /// Asks for the values something takes, as a dialog with one field each.
        /// </summary>
        /// <remarks>
        /// The sibling of <see cref="Asks" /> and deliberately not the same thing. A name is asked
        /// for under the control it belongs to, because it is that control's name. Values are a form
        /// to fill in: several fields, in the middle of the screen, the same way however many there
        /// are and whatever raised them - a button, a chip in a sentence, or a double press.
        /// </remarks>
        public static Deed Values(int command, string question, IEnumerable<Field> fields)
        {
            return Values(command, question, fields, null);
        }

        /// <summary>The same, opening with what is already there.</summary>
        /// <remarks>
        /// So changing one value of three does not mean typing all three again. A form that opened
        /// empty over values somebody had already given would read as having lost them.
        /// </remarks>
        public static Deed Values(int command, string question, IEnumerable<Field> fields,
            IEnumerable<string> filled)
        {
            Deed asks = Asks(command, question);
            asks.Fields = fields;
            asks.Filled = filled;
            return asks;
        }

        /// <summary>
        /// What each field asks for, one per field, and null where this asks for nothing.
        /// </summary>
        /// <remarks>
        /// Held rather than copied, and copied out only when a question actually opens. A deed is
        /// made afresh every time a control is dressed, which is every tick, so anything built here
        /// would be built sixty times a second for a question nobody has asked yet.
        /// </remarks>
        public IEnumerable<Field> Fields { get; private set; }

        /// <summary>What the fields open with, or null for empty ones.</summary>
        public IEnumerable<string> Filled { get; private set; }

        /// <summary>Whether this asks for values rather than for one thing under what was pressed.</summary>
        public bool IsForm
        {
            get { return Fields != null; }
        }


        public static Deed Asks(int command, string question)
        {
            if (command == 0)
                return Nothing;

            if (string.IsNullOrEmpty(question))
                return Sends(command);

            var deed = new Deed();
            deed.Means = Means.Ask;
            deed.Command = command;
            deed.Question = question;
            return deed;
        }

        /// <summary>A box that takes the keyboard, and sends what is typed to <paramref name="command" />.</summary>
        public static Deed Types(int command, TextBox box)
        {
            if (command == 0)
                return Nothing;

            if (box == null)
                return Sends(command);

            var deed = new Deed();
            deed.Means = Means.Type;
            deed.Command = command;
            deed.Box = box;
            return deed;
        }

        /// <summary>A control that hangs <paramref name="overlay" /> over the screen, under itself.</summary>
        public static Deed Opens(Element overlay)
        {
            if (overlay == null)
                return Nothing;

            var deed = new Deed();
            deed.Means = Means.Open;
            deed.Overlay = overlay;
            return deed;
        }

        /// <summary>The control that sends the answer standing in the question already open.</summary>
        public static Deed Commits()
        {
            var deed = new Deed();
            deed.Means = Means.Commit;
            return deed;
        }

        /// <summary>The control that abandons the question already open.</summary>
        public static Deed Cancels()
        {
            var deed = new Deed();
            deed.Means = Means.Cancel;
            return deed;
        }

        /// <summary>The control that puts the editor away.</summary>
        public static Deed Leaves()
        {
            var deed = new Deed();
            deed.Means = Means.Leave;
            return deed;
        }
    }
}
