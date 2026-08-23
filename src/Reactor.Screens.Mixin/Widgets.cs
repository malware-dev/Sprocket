using Phosphor;
using VRage.Game;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>
    /// What Reactor looks like on this target.
    /// <para>
    /// Core hands over intent - heading, muted, slot, problem - and never colours or sizes, so every
    /// value here is this target's own opinion. The <b>roles</b> are the Avalonia host's, one for one, and
    /// deliberately so: two targets that agree on what a slot is are two targets somebody learns once.
    /// The <b>values</b> are not, and should not be. That host is a desktop application and wears a
    /// desktop's dark theme; this one is drawn over a ship, in a game with a look of its own, and a
    /// panel that ignored it would read as something bolted on.
    /// </para>
    /// <para>
    /// So: the same neutral slate, turned cold and blue and let the world through. Space Engineers is
    /// steel and glass lit blue, with hard corners, hairlines rather than borders, and colour saved for
    /// the few things that mean something - which is the same restraint the roles below already had.
    /// </para>
    /// <para>
    /// Most of the values below were taken out of the game's own textures rather than guessed at, and
    /// each one says where it came from. Guessing was the first attempt and it was wrong in a way worth
    /// recording: the eye remembers Space Engineers as dark navy, and it is not - its panels are a light,
    /// almost grey-green steel, and its highlight is a pale ice blue rather than a saturated one.
    /// </para>
    /// </summary>
    public static class Palette
    {
        /// <summary>
        /// The world behind shows through. This is drawn over the game rather than on a page.
        /// </summary>
        /// <remarks>
        /// The single biggest thing that makes an overlay look like it belongs to the game rather than
        /// like a window pasted over it. Not so far that the ship behind competes with the words: enough
        /// that the screen reads as glass with something beyond it.
        /// <para>
        /// The game's own panels are opaque, because they are drawn on a menu that has already dimmed
        /// everything. This is drawn over a ship nobody dimmed, so it does the dimming itself.
        /// </para>
        /// </remarks>
        public const byte Veil = 226;

        /// <summary>Darker than a panel, so panels read as sitting on it. Derived from <see cref="Panel" />.</summary>
        public static readonly Color Page = new Color(20, 27, 32, Veil);

        /// <summary>Header and footer. A step darker again, so the screen's edges settle.</summary>
        public static readonly Color Chrome = new Color(27, 36, 42, 242);

        /// <summary>Textures/GUI/Controls/DarkBlueBackground.png, which is flat and entirely this colour.</summary>
        public static readonly Color Panel = new Color(41, 54, 62, 218);

        /// <summary>DarkBlueBackground_highlight.png. What the game does to a panel under the pointer.</summary>
        public static readonly Color PanelHot = new Color(60, 76, 82, 246);

        /// <summary>rectangle_button_center_checked.png. The game's mid tone, which reads as a hairline.</summary>
        public static readonly Color Line = new Color(91, 115, 123);

        /// <summary>
        /// Textures/GUI/Icons/*.png, which are single-colour and drawn in exactly this.
        /// </summary>
        /// <remarks>
        /// Not white, which is what it looks like next to the panels and is the mistake worth avoiding.
        /// Everything the game draws as a symbol is this pale ice blue, so text in it belongs to the same
        /// family as the icons beside it.
        /// </remarks>
        public static readonly Color Ink = new Color(191, 230, 242);

        /// <summary>A step down from <see cref="Ink" />, toward the focus blue.</summary>
        public static readonly Color Muted = new Color(146, 178, 192);

        /// <summary>A step down again, still clear of the panel it sits on.</summary>
        public static readonly Color Faint = new Color(101, 128, 140);

        /// <summary>
        /// DarkBlueBackground_focus.png. What the game fills a control with when it is the one in hand.
        /// </summary>
        /// <remarks>
        /// Pale, and filled rather than outlined, which is why anything wearing it takes dark text. A
        /// saturated blue was the guess and it was the wrong family altogether.
        /// </remarks>
        public static readonly Color Accent = new Color(142, 188, 207);

        public static readonly Color SlotEdge = new Color(142, 188, 207);
        public static readonly Color SlotFill = new Color(55, 72, 81);

        /// <summary>checkbox_green_checked.png. Vivid, and nothing like a desktop's mint.</summary>
        public static readonly Color Live = new Color(58, 204, 0);

        public static readonly Color Dim = new Color(60, 76, 82);
        public static readonly Color LiveWash = new Color(30, 58, 26);

        public static readonly Color Problem = new Color(232, 74, 58);
        public static readonly Color ProblemWash = new Color(58, 32, 30);
        public static readonly Color Notice = new Color(240, 172, 40);
        public static readonly Color NoticeWash = new Color(58, 46, 24);

        public static readonly Color None = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// Text sizes, named for what they are for.
    /// <para>
    /// The other host says these in pixels because it draws into a window. This one says them as font
    /// scales, because it draws into a fixed UI size that the game stretches. The numbers therefore
    /// differ while the sizes agree, which is the whole reason they are named rather than written out.
    /// </para>
    /// </summary>
    public static class Type
    {
        public const float Title = 0.8f;
        public const float Body = 0.55f;
        public const float Small = 0.5f;
        public const float Tiny = 0.45f;
    }

    /// <summary>
    /// Room, named for what it separates.
    /// </summary>
    /// <remarks>
    /// Here for the same reason <see cref="Type" /> is: these were sixty literals scattered across five
    /// files, every one of them a judgement about how much air something needs, and none of them
    /// findable. Naming them makes a pass over the whole look six numbers rather than an archaeology
    /// expedition.
    /// <para>
    /// They are larger than the Avalonia host's, and that is not a disagreement with the template. That
    /// host draws into a window somebody sized with a mouse; this draws one of the game's own screens,
    /// which is four by three at the full height of the monitor. The same layout in the same proportions
    /// simply has half as much again to work with, and holding onto the smaller numbers would be leaving
    /// a third of the screen empty out of habit.
    /// </para>
    /// </remarks>
    public static class Space
    {
        /// <summary>A rule. One unit is one pixel at the height the UI is defined against.</summary>
        public const float Hair = 1f;

        /// <summary>Between two lines that are one thing - a name, and what it is doing.</summary>
        public const float Line = 3f;

        /// <summary>Between one row of a list and the next.</summary>
        public const float Row = 6f;

        /// <summary>Between two controls sitting side by side.</summary>
        public const float Item = 10f;

        /// <summary>Between one region of a screen and another.</summary>
        public const float Region = 16f;

        /// <summary>Inside a row, across.</summary>
        public const float PadX = 19f;

        /// <summary>Inside a row, down. Less than across, because a line of text is wider than it is tall.</summary>
        public const float PadY = 14f;

        /// <summary>Inside something small that is not a row - a chip, a switch, a quiet control.</summary>
        public const float ChipX = 16f;

        public const float ChipY = 6f;
    }

    /// <summary>Things that are a size rather than a gap.</summary>
    public static class Sizes
    {
        /// <summary>A state light.</summary>
        public const float Dot = 13f;

        /// <summary>
        /// A block's picture.
        /// </summary>
        /// <remarks>
        /// Entirely this target's number. Core hands over a name and says nothing about size, which it
        /// should not: Space Engineers draws recognisable artwork for every block type and it earns the
        /// room, where a game whose things all look alike would want it smaller or absent.
        /// </remarks>
        public const float Icon = 54f;

        /// <summary>One stage of a pick.</summary>
        public const float Column = 400f;

        /// <summary>Anything hanging over the screen - a question, a list that opens downward.</summary>
        public const float Popup = 512f;
    }

    /// <summary>A hairline across a block, with room around it.</summary>
    /// <remarks>
    /// The line is drawn rather than being the element's background, so that padding puts space around it
    /// instead of thickening it. Phosphor has no margins - padding is the only spacing there is - and a
    /// background fills the whole box including that padding, which turns a one-pixel rule into a band.
    /// </remarks>
    public class Rule : Element
    {
        public Rule(float thickness, Color color)
        {
            Thickness = thickness;
            Color = color;
        }

        public float Thickness { get; set; }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(available.X, Thickness);
        }

        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Solid,
                new RectangleF(ContentBounds.X, ContentBounds.Y, ContentBounds.Width, Thickness),
                Color, 0f);
        }
    }

    /// <summary>
    /// A state light.
    /// </summary>
    /// <remarks>
    /// Square, where the other host draws a circle. Not a decision - the seam draws sprites by material
    /// id and there is no round one declared yet, and inventing a rounded primitive for it would put a
    /// shape into the graphics abstraction that an <c>IMyTextSurface</c> would then have to reproduce.
    /// A circular texture is the answer when this is worth doing properly.
    /// </remarks>
    public class Dot : Element
    {
        public const float Size = Sizes.Dot;

        public Dot()
        {
            Background = Palette.Dim;
        }

        /// <summary>Whether it is lit right now.</summary>
        public bool IsLit
        {
            set { Background = value ? Palette.Live : Palette.Dim; }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        protected override void DrawContent(IGraphics g)
        {
        }
    }

    /// <summary>
    /// A fillable part of a sentence.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means, so nothing else may borrow this
    /// look. The line under it is what distinguishes it from a row at a glance, and it is drawn rather
    /// than being a border because the seam has no borders - a rectangle of colour is all either target
    /// can be asked for.
    /// </remarks>
    public class Slot : Label
    {
        public Slot()
        {
            Scale = Type.Body;
            Color = Palette.Ink;
            Background = Palette.SlotFill;
            Padding = new Thickness(Space.ChipX, Space.ChipY);
            Underline = Palette.SlotEdge;

            // A slot is one thing named, and a wrapped one would stop reading as a single chip.
            Wraps = false;
        }

        /// <summary>The colour of the line along the bottom.</summary>
        public Color Underline { get; set; }

        protected override void DrawContent(IGraphics g)
        {
            const float thickness = 2f;

            g.DrawSprite(Solid,
                new RectangleF(Bounds.X, Bounds.Y + Bounds.Height - thickness, Bounds.Width, thickness),
                Underline, 0f);

            base.DrawContent(g);
        }
    }

    /// <summary>
    /// A switch, drawn filled when it governs everything under it and as an outline when it governs only
    /// itself.
    /// </summary>
    /// <remarks>
    /// Two of them in a column need telling apart. The indent already says which is nested; the weight
    /// says which is the bigger act, because stopping a whole airlock is not the same as stopping one of
    /// them. The outline is drawn as four thin rectangles, since the seam has no borders.
    /// </remarks>
    public class Toggle : Label
    {
        public Toggle()
        {
            Scale = Type.Tiny;
            Padding = new Thickness(Space.ChipX, Space.ChipY);
            Wraps = false;
        }

        /// <summary>Whether this governs everything below it, and is therefore drawn solid.</summary>
        public bool Governs { get; set; }

        /// <summary>Puts it in the one state or the other, so the two can never be half set.</summary>
        public void Show(bool governs, Color accent, Color ink)
        {
            Governs = governs;
            Color = ink;
            Background = governs ? accent : Palette.None;
            Edge = governs ? Palette.None : accent;
        }

        /// <summary>Nothing to switch here, so nothing is drawn.</summary>
        public void Hide()
        {
            Text = "";
            Color = Palette.None;
            Background = Palette.None;
            Edge = Palette.None;
        }
    }

    /// <summary>The building blocks, so a row is one call rather than six lines of setting properties.</summary>
    public static class Widgets
    {
        /// <summary>
        /// The default piece of text, and it wraps.
        /// </summary>
        /// <remarks>
        /// Wrapping by default matches the other host, and it matters more here: a block somebody named
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
            Label row = Label(text, color, Type.Body);
            row.Background = Palette.Panel;
            row.Padding = new Thickness(Space.PadX, Space.PadY);
            return row;
        }

        /// <summary>Quiet, for something that is available without being the point.</summary>
        public static Label Ghost(string text)
        {
            Label ghost = Label(text, Palette.Muted, Type.Small);
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
            Label chip = Label(text, on ? Palette.Chrome : Palette.Muted, Type.Small);
            chip.Background = on ? Palette.Accent : Palette.Panel;
            chip.Padding = new Thickness(Space.ChipX, Space.ChipY);
            chip.Wraps = false;
            return chip;
        }

        /// <summary>Something said against a coloured ground, because it needs reading.</summary>
        public static Label Wash(string text, Color ink, Color background)
        {
            Label wash = Label(text, ink, Type.Small);
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

        public static Stack RowOf(float spacing)
        {
            var stack = new Stack();
            stack.Horizontal = true;
            stack.Spacing = spacing;
            return stack;
        }
    }
}
