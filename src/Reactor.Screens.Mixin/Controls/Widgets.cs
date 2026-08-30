using VRage.Game;
using VRage.Utils;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Controls
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
        public static Label Label(string text, Color color, float scale)
        {
            var label = new Label();
            label.Font = MyFontEnum.White;
            label.Scale = scale;
            label.Color = color;
            label.Wraps = true;
            label.Text = text;
            return label;
        }

        /// <summary>A row that drills into something. The workhorse of every list on every screen.</summary>
        public static Label Row(string text, Color color)
        {
            Label row = Label(text, color, Lettering.Body);
            row.Background = Palette.Panel;
            row.Highlight = Palette.PanelHot;
            row.Padding = new Thickness(Space.PadX, Space.PadY);
            return row;
        }

        /// <summary>Quiet, for something that is available without being the point.</summary>
        public static Label Ghost(string text)
        {
            Label ghost = Label(text, Palette.Muted, Lettering.Small);
            ghost.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 0.8f);

            // A control rather than a sentence. One that broke onto two lines would change the height of
            // whatever row it sits in, which is the fidgeting this screen is trying not to do.
            ghost.Wraps = false;
            return ghost;
        }

        /// <summary>
        /// One of a set of narrowings, lit when it is the one in force.
        /// </summary>
        /// <remarks>
        /// Dark words on the lit one. The game's focus colour is a pale ice blue that it fills a control
        /// with rather than outlines, so anything wearing it has to put dark text on top - pale on pale
        /// is the trap a saturated accent hides and this one does not.
        /// </remarks>
        public static Label Chip(string text, bool on)
        {
            Label chip = Label(text, on ? Palette.Chrome : Palette.Muted, Lettering.Small);
            chip.Background = on ? Palette.Accent : Palette.Panel;
            chip.Padding = new Thickness(Space.ChipX, Space.ChipY);
            chip.Wraps = false;
            return chip;
        }

        /// <summary>
        /// Somewhere to type.
        /// </summary>
        /// <remarks>
        /// Here because it was not, and three boxes made in three places quietly kept Phosphor's own
        /// defaults for the caret, the selection and the hint - three colours on screen that the palette
        /// had never been asked about. Phosphor is right to have them: it knows nothing about Reactor
        /// and something has to be drawn. Reactor is wrong to leave them.
        /// </remarks>
        public static TextBox Box(float scale, Color background)
        {
            var box = new TextBox();
            box.Font = MyFontEnum.White;
            box.Scale = scale;
            box.Color = Palette.Ink;
            box.Background = background;
            box.Edge = Palette.Line;
            box.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.3f);

            box.CaretColor = Palette.Ink;
            box.HintColor = Palette.Faint;

            // A mid tone rather than the focus blue. The words stay their own colour while they are
            // selected, and pale ink on the pale focus fill is two things the same brightness.
            box.SelectionColor = Palette.Line;
            return box;
        }

        /// <summary>Gives a scroller a bar, in this target's colours.</summary>
        /// <remarks>
        /// Here rather than at each of the three call sites, because a bar that differed between the block
        /// list and the screen behind it would read as two different controls doing the same job. Phosphor
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
        public static Nameplate Nameplate(float scale)
        {
            var plate = new Nameplate(Symbols.Pencil, Lettering.Body);

            plate.Font = MyFontEnum.White;
            plate.Scale = scale;
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

        /// <summary>One symbol, at the size a symbol stands beside a line of text.</summary>
        public static Glyph Symbol(string symbol, Color tint)
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
            // along a row of controls, which is the fidgeting this screen keeps being pulled back from.
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
