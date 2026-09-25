using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;
using Glyph = Sprocket.Host.SpaceEngineers.Ui.Legacy.Glyph;
using TextBox = Sprocket.Host.SpaceEngineers.Ui.Legacy.TextBox;

namespace Sprocket.Host.SpaceEngineers.Controls
{
    /// <summary>The building blocks, so a row is one call rather than six lines of setting properties.</summary>
    public static class Widgets
    {
        /// <summary>
        /// The default piece of text, and it wraps.
        /// </summary>
        /// <remarks>
        /// Wrapping by default, because a block somebody named
        /// "Hangar door - outer, port side" has to be readable in a 250-unit column, and the alternative
        /// is a row that silently says something shorter than what is there.
        /// </remarks>
        public static Label Label(string text, Color color, float fontSize)
        {
            var label = new Label();
            label.Font = MyFontEnum.White;
            label.FontSize = fontSize;
            label.Color = color;
            label.Wraps = true;
            label.Text = text;
            return label;
        }

        /// <summary>
        /// A line that drills into something, whatever it is made of.
        /// </summary>
        /// <remarks>
        /// The look is one step of the panel ramp: a row lights to the hot panel where a control that
        /// adds something lights to the panel itself, because a row has a fill to brighten and a button
        /// made of words does not. Two loudnesses, and which one a thing takes says what sort of thing
        /// it is rather than what it happens to be built from - a row is a <see cref="Bar" /> here, a
        /// <see cref="Stack" /> in the picker and a <see cref="Label" /> in a list of choices.
        /// </remarks>
        public static void Row(Element line)
        {
            line.Highlight = Palette.PanelHot;
        }

        /// <summary>What a button holds clear inside itself, whichever kind of button it is.</summary>
        static readonly Thickness Inside = new Thickness(Space.PadX, Space.ChipY * 1.3f);

        /// <summary>A word that answers a question, or runs an operation: a panel, and it lights.</summary>
        public static Label Chip(string text)
        {
            Label chip = Label(text, Palette.Ink, Lettering.Small);
            chip.Padding = Inside;

            // A control rather than a sentence, for the reason a ghost is one: a word that broke onto
            // two lines would change the height of the row of them.
            chip.Wraps = false;
            return Filled(chip);
        }

        /// <summary>
        /// Puts a panel under a control, so it reads as one wherever it is standing.
        /// </summary>
        /// <remarks>
        /// What a quiet control needs when it sits on something other than the page: a highlight the
        /// same colour as the ground behind it is a control that never lights.
        /// </remarks>
        public static Label Filled(Label control)
        {
            control.Background = Palette.Panel;
            control.Highlight = Palette.PanelHot;
            return control;
        }

        /// <summary>
        /// Dresses the one button that moves somebody on, ready to be pressed.
        /// </summary>
        /// <remarks>
        /// Dark words on the lit fill, the way the game does it: its focus colour is pale, so a control
        /// wearing it reads as lit only if what it says goes dark.
        /// <para>
        /// Applied rather than made, because the same control is dressed again whenever what it can do
        /// changes - and it is the screen's primary and the one in the box that asks for a name, which
        /// are the same button and must not drift apart.
        /// </para>
        /// </remarks>
        public static void Go(Label button)
        {
            button.Padding = Inside;
            button.Background = Palette.Accent;
            button.Color = Palette.Chrome;
            button.Highlight = Palette.Ink;
        }

        /// <summary>
        /// The same button with nothing to send yet: a well, and it does not light.
        /// </summary>
        /// <remarks>
        /// A well rather than a fill, because a fill is what says a control is live. Nothing lights,
        /// because a control that lights and then refuses the press is worse than one that never lit.
        /// </remarks>
        public static void Waiting(Label button)
        {
            button.Padding = Inside;
            button.Background = Palette.SlotFill;
            button.Color = Palette.Faint;
            button.Highlight = Palette.None;
        }

        /// <summary>
        /// The same button on a screen that has nowhere to go on to: gone, but its place kept.
        /// </summary>
        /// <remarks>
        /// Not collapsed. The footer stands the same height on every screen, and one that changed
        /// height as somebody walked through the editor would move the list above it.
        /// </remarks>
        public static void Absent(Label button)
        {
            button.Padding = Inside;
            button.Background = Palette.None;
            button.Color = Palette.None;
            button.Highlight = Palette.None;
        }

        /// <summary>
        /// Quiet words, sized and spaced as part of a control rather than as a sentence.
        /// </summary>
        /// <remarks>
        /// Not a control itself: it has no highlight, so it says nothing about being pressable. What
        /// wears this is the word inside something larger - the kind line's word stands beside its
        /// arrow, and the line is what gets pressed.
        /// </remarks>
        public static Label Quiet(string text, float lettering)
        {
            Label quiet = Label(text, Palette.Muted, lettering);
            quiet.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 0.8f);

            // A control rather than a sentence. One that broke onto two lines would change the height of
            // whatever row it sits in, which is the fidgeting this screen is trying not to do.
            quiet.Wraps = false;
            return quiet;
        }

        /// <summary>
        /// A button offered in place: quiet words, no panel, and it lights.
        /// </summary>
        /// <remarks>
        /// <see cref="Quiet" /> wearing a highlight, which is the whole of the difference between words
        /// and a button: a thing that lights up had better do something when it is pressed.
        /// <para>
        /// The one under a sentence, the one under an instance, and the verbs in the footer. Something
        /// available without being the point, which is what the size says.
        /// </para>
        /// </remarks>
        public static Label Ghost(string text)
        {
            Label ghost = Quiet(text, Lettering.Small);
            ghost.Highlight = Palette.Panel;
            return ghost;
        }

        /// <summary>
        /// The same button where adding something is the main act of the screen.
        /// </summary>
        /// <remarks>
        /// The ways to start a system are the largest thing on a construct's front page, and still the
        /// same button as the one that adds a step to a sentence. A style of its own rather than a size
        /// passed in, because how loud a control is on a screen is not a decision for the screen's own
        /// builder to take one call at a time.
        /// </remarks>
        public static Label Way(string text)
        {
            Label way = Quiet(text, Lettering.Body);
            way.Highlight = Palette.Panel;
            return way;
        }

        /// <summary>
        /// Somewhere to type.
        /// </summary>
        /// <remarks>
        /// Made here so the caret, the selection and the hint come from the palette. The layout has its
        /// own defaults for all three and is right to: it knows nothing about Sprocket and something has
        /// to be drawn. A box made anywhere else keeps them, which puts three colours on screen that the
        /// palette was never asked about.
        /// </remarks>
        public static TextBox Box(float fontSize, Color background)
        {
            var box = new TextBox();
            box.Font = MyFontEnum.White;
            box.FontSize = fontSize;
            box.Color = Palette.Ink;
            box.Background = background;
            box.Edge = Palette.Line;
            box.Padding = new Thickness(Space.ChipX * 0.8f, Space.FieldY);

            box.CaretColor = Palette.Ink;
            box.HintColor = Palette.Faint;
            box.WrongEdge = Palette.Problem;

            // A mid tone rather than the focus blue. The words stay their own colour while they are
            // selected, and pale ink on the pale focus fill is two things the same brightness.
            box.SelectionColor = Palette.Line;
            return box;
        }

        /// <summary>Gives a scroller a bar, in this target's colours.</summary>
        /// <remarks>
        /// Here rather than at each of the three call sites, because a bar that differed between the block
        /// list and the screen behind it would read as two different controls doing the same job. The layout
        /// knows the geometry and nothing about the look, which is the same split every other widget here
        /// is built on.
        /// </remarks>
        public static Scroll Barred(Scroll scroll)
        {
            scroll.BarWidth = Sizes.Bar;
            scroll.BarMinLength = Sizes.Thumb;
            scroll.BarColor = Palette.Thumb;
            scroll.BarHeldColor = Palette.ThumbHeld;
            return scroll;
        }

        /// <summary>
        /// A name somebody can change where it stands.
        /// </summary>
        /// <remarks>
        /// The pencil goes a step above the size a symbol usually gets, because it stands beside the
        /// largest text on a screen rather than beside a row. At the ordinary size it read as a speck
        /// somebody had dropped on the heading.
        /// </remarks>
        public static Nameplate Nameplate(float fontSize)
        {
            var plate = new Nameplate(Symbols.Pencil, Lettering.Pen);

            plate.Font = MyFontEnum.White;
            plate.FontSize = fontSize;
            plate.PenLift = Lettering.SymbolLift;
            // None at all, because the pencil brings its own. A symbol advances a full square cell and
            // this one fills about half of it, so at heading size there are already ten units of empty
            // cell between the name and the first ink of the pencil - and ten more after it. Anything
            // added here is on top of that.
            plate.Gap = 0f;
            plate.LeastWidth = Sizes.Name;

            plate.Color = Palette.Ink;
            plate.PenColor = Palette.Faint;

            // No hint, deliberately. A hint says what a blank box is for, and a heading is never a blank
            // box: it stands where the name stands, in the largest text on the screen, with a pencil on it
            // that somebody pressed to get here. There is nothing left to explain.
            //
            // Not the rename prompt either. That is written to be asked - "What is it?" - which reads
            // oddly with no question mark, and it is far longer than a box sized to a name, so it clips
            // in every case that matters. It has a home already: the popup that asks it, where the box
            // really is blank and really is unlabelled.

            // The hot panel tone rather than a control's well, because a heading is not sunk into the
            // chrome - it sits on it, and the fill is here to say "this is a field now" rather than to
            // make a hole for it.
            plate.Well = Palette.PanelHot;

            // Red-brown in place of the blue-grey well, and a rule under it in the loud red. The fill says
            // "this one" at a glance across the screen; the rule is the part that survives being looked at
            // on a dim monitor, because a red wash cannot get far enough from the chrome to do it alone.
            plate.WrongWell = Palette.WrongFill;
            plate.WrongEdge = Palette.Problem;
            plate.WrongEdgeThickness = 2f;

            // A mid tone rather than the focus blue, for the reason a search box takes the same one: the
            // words keep their own colour while they are selected, and pale ink on the pale focus fill is
            // two things the same brightness.
            plate.SelectionColor = Palette.Line;
            return plate;
        }

        /// <summary>Something said against a coloured ground, because it needs reading.</summary>
        public static Label Wash(string text, Color ink, Color background)
        {
            Label wash = Label(text, ink, Lettering.Small);
            wash.Background = background;
            wash.Padding = new Thickness(Space.ChipX, Space.ChipY * 1.6f);
            return wash;
        }

        /// <summary>
        /// The game's own way of saying "this is a control".
        /// </summary>
        /// <remarks>
        /// Space Engineers puts its buttons and its section headings in capitals, and doing the same is
        /// most of what makes a panel read as belonging to it. Only the chrome: a block called "Hangar
        /// Door" is a name somebody typed, and shouting it back at them would be this target editing
        /// their words.
        /// </remarks>
        public static string Caps(string text)
        {
            return text == null ? "" : text.ToUpperInvariant();
        }

        /// <summary>
        /// Readies a control that only sometimes has anything to do.
        /// </summary>
        /// <remarks>
        /// A command of zero is how core says "not now", so a control with none is not a control: it
        /// goes quiet, stops lighting under the pointer, and has nothing to say about itself. Greying
        /// is right for these and wrong for a sentence - these are controls that will become
        /// available, where a sentence is not a control at all.
        /// </remarks>
        public static void Waiting(Glyph button, int command, string says)
        {
            bool ready = command != 0;
            button.Tint = ready ? Palette.Muted : Palette.Dim;
            button.Highlight = ready ? Palette.Panel : Palette.None;
            button.Press = Deed.Sends(command);
            button.Tip = ready ? says : null;
        }

        public static Rule Line()
        {
            return new Rule(Space.Hair, Palette.Line);
        }

        public static Stack Column(float spacing)
        {
            var stack = new Stack();
            stack.Spacing = spacing;
            return stack;
        }

        /// <summary>
        /// One symbol, at the size a symbol stands beside a line of text, and lined up with it.
        /// </summary>
        /// <remarks>
        /// Lifted by <see cref="Lettering.SymbolLift" />, because the game's letters sit high in their
        /// own line and a symbol centred in its line would read as low beside them. Anything that is
        /// not standing on a line of text wants <see cref="Centred" /> instead.
        /// </remarks>
        public static Glyph Symbol(string symbol, Color tint)
        {
            var glyph = new Glyph(symbol, Lettering.Symbol, tint);
            glyph.Lift = Lettering.SymbolLift;
            return glyph;
        }

        /// <summary>
        /// One symbol in the middle of whatever room it is given, with no text to line up with.
        /// </summary>
        /// <remarks>
        /// A tick inside a checkbox is centred in the checkbox and nothing about the game's letters
        /// should reach it. Correcting one of these is the bug the other case exists to fix, arriving
        /// from the opposite direction.
        /// </remarks>
        public static Glyph Centred(string symbol, Color tint)
        {
            return new Glyph(symbol, Lettering.Symbol, tint);
        }

        /// <summary>
        /// A note that follows the pointer, saying what a control is when the control cannot.
        /// </summary>
        /// <remarks>
        /// Drawn on <see cref="Palette.Chrome" /> rather than a panel tone, so it reads as sitting above
        /// the screen rather than as another row of it - it is the only thing here that is allowed over
        /// everything, and it should look borrowed rather than built in.
        /// </remarks>
        public static Label Tip()
        {
            Label tip = Label("", Palette.Ink, Lettering.Small);
            tip.Background = Palette.Chrome;
            tip.Edge = Palette.Line;
            tip.EdgeThickness = Space.Hair;
            tip.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY);

            // One line however long it is. A note that wrapped would change shape as the pointer moved
            // along a row of controls, which is the fidgeting this screen keeps being pulled back from -
            // and a note long enough to need two lines is a note saying more than a note should.
            tip.Wraps = false;
            return tip;
        }

        public static Stack RowOf(float spacing)
        {
            var stack = new Stack();
            stack.Horizontal = true;
            stack.Spacing = spacing;
            return stack;
        }
    }
}
