using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>How the views converted to the size scale look.</summary>
    public static class Styles
    {
        /// <summary>A name, or a sentence that is the point of where it stands.</summary>
        public static readonly TextStyle Name = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink }
        };

        /// <summary>What a list says while it has nothing in it.</summary>
        public static readonly TextStyle Empty = new TextStyle(Name)
        {
            Base = { Color = Palette.Muted, Padding = new Thickness(8f, 8f, 0f, 0f) }
        };

        /// <summary>The note beside the pointer: quiet words, strong ones in the strong color and emphasized ones bright.</summary>
        public static readonly RichTextStyle Tip = new RichTextStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                CodeFont = Lettering.Mono,
                FontSize = 20f,
                Heading1Size = 24f,
                Heading2Size = 20f,
                Heading3Size = 20f,
                Color = Palette.Muted,
                StrongColor = Palette.Strong,
                EmphasisColor = Palette.Ink,
                HeadingColor = Palette.Ink,
                RuleColor = Palette.Line,
                BlockSpacing = 8f,
                Indent = 20f,
                Background = Palette.Chrome,
                Edge = Palette.Line,
                Padding = new Thickness(12f, 8f)
            }
        };

        /// <summary>A dialog: a panel in the middle of the screen, with the screen veiled behind it.</summary>
        public static readonly WindowStyle Dialog = new WindowStyle
        {
            Base = { Background = Palette.Chrome, Edge = Palette.Line, Padding = new Thickness(24f), Backdrop = Palette.Veil }
        };

        /// <summary>What a dialog asks.</summary>
        public static readonly RichTextStyle Question = new RichTextStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                CodeFont = Lettering.Mono,
                FontSize = 24f,
                Color = Palette.Ink,
                StrongColor = Palette.Strong,
                EmphasisColor = Palette.Ink,
                BlockSpacing = 8f,
                Indent = 24f
            }
        };

        /// <summary>A screen's heading, in the largest words on it.</summary>
        public static readonly TextStyle Title = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 32f, Color = Palette.Ink }
        };

        /// <summary>Words in a heading around a name, quieter than the name.</summary>
        public static readonly TextStyle TitleLead = new TextStyle(Title)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>A name in a heading, spaced as its box is so the box takes its place without moving it.</summary>
        public static readonly TextStyle TitleName = new TextStyle(Title)
        {
            Base = { Padding = new Thickness(8f, 4f) }
        };

        /// <summary>The box a heading's name is typed in, a well on the chrome.</summary>
        public static readonly TextBoxStyle TitleBox = new TextBoxStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 32f,
                Color = Palette.Ink,
                CaretColor = Palette.Ink,
                SelectionColor = Palette.Line,
                Background = Palette.PanelHot,
                Padding = new Thickness(8f, 4f)
            }
        };

        /// <summary>The same box holding a name that cannot be given.</summary>
        public static readonly TextBoxStyle TitleRefused = new TextBoxStyle(TitleBox)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>A heading's name with its pencil, lit under the pointer where pressing it renames.</summary>
        public static readonly ElementStyle Plate = new ElementStyle
        {
            Hot = { Background = Palette.Panel }
        };

        /// <summary>The pencil beside a name that can be renamed, whose color the view sets while the name is typed.</summary>
        public static readonly GlyphStyle Pen = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f, Tint = Palette.Faint }
        };

        /// <summary>The arrow between two parts of a heading.</summary>
        public static readonly GlyphStyle Join = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f, Tint = Palette.Faint }
        };

        /// <summary>A section's name, over what it heads, with a name inside it in the bright ink.</summary>
        public static readonly RichTextStyle Section = new RichTextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Faint, StrongColor = Palette.Ink }
        };

        /// <summary>The quiet line under a name: what it is doing, and when it last did anything.</summary>
        public static readonly TextStyle Detail = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Faint }
        };

        /// <summary>The rest of what a banner says, once it is opened.</summary>
        public static readonly TextStyle Opened = new TextStyle(Name)
        {
            Base = { Color = Palette.Faint, Padding = new Thickness(16f, 0f, 16f, 8f) }
        };

        /// <summary>What went wrong, in the words of whatever found it, for whoever mends it.</summary>
        public static readonly TextStyle Diagnostic = new TextStyle(Detail)
        {
            Base = { Color = Palette.Dim, Padding = new Thickness(16f, 0f, 16f, 8f) }
        };

        /// <summary>A row of a list that opens what it names.</summary>
        public static readonly ElementStyle Row = new ElementStyle
        {
            Base = { Background = Palette.Panel, Padding = new Thickness(16f, 8f) },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>A row whose sequence is running.</summary>
        public static readonly ElementStyle Running = new ElementStyle(Row)
        {
            Base = { Background = Palette.LiveWash }
        };

        /// <summary>A row with something wrong in it.</summary>
        public static readonly ElementStyle Broken = new ElementStyle(Row)
        {
            Base = { Background = Palette.ProblemWash }
        };

        /// <summary>A row that has stopped where it is waiting to be let on.</summary>
        public static readonly ElementStyle Parked = new ElementStyle(Row)
        {
            Base = { Background = Palette.NoticeWash }
        };

        /// <summary>A row that adds something rather than naming it.</summary>
        public static readonly ElementStyle AddRow = new ElementStyle(Row)
        {
            Base = { Background = Palette.None },
            Hot = { Background = Palette.Panel }
        };

        /// <summary>A row with nothing to open.</summary>
        public static readonly ElementStyle Still = new ElementStyle(Row)
        {
            Base = { Background = Palette.None },
            Hot = { Background = Palette.None }
        };

        /// <summary>The row a screen is about, edged so being chosen shows whatever else the row is.</summary>
        public static readonly ElementStyle Chosen = new ElementStyle(Row)
        {
            Base = { Background = Palette.SlotFill, Edge = Palette.Accent }
        };

        /// <summary>The chosen row, with something wrong in it.</summary>
        public static readonly ElementStyle BrokenChosen = new ElementStyle(Broken)
        {
            Base = { Edge = Palette.Accent }
        };

        /// <summary>The chosen row, running.</summary>
        public static readonly ElementStyle RunningChosen = new ElementStyle(Running)
        {
            Base = { Edge = Palette.Accent }
        };

        /// <summary>The chosen row, stopped where it is waiting to be let on.</summary>
        public static readonly ElementStyle ParkedChosen = new ElementStyle(Parked)
        {
            Base = { Edge = Palette.Accent }
        };

        /// <summary>A state light, whose color the view sets from the state.</summary>
        public static readonly GlyphStyle Light = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 8f, Tint = Palette.Dim }
        };

        /// <summary>A mark beside a name, whose color the view sets from what it marks.</summary>
        public static readonly GlyphStyle Flag = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f }
        };

        /// <summary>The chevron that opens a banner, whose color the view sets from the banner's.</summary>
        public static readonly GlyphStyle Chevron = new GlyphStyle(Flag);

        /// <summary>A screen's scroll, whose bar is as wide as the one the game reserves for its own.</summary>
        public static readonly ScrollViewStyle ScrollView = new ScrollViewStyle
        {
            Base = { BarWidth = Sizes.Bar, Thumb = Palette.Thumb, LeastThumb = 32f },
            Held = { Thumb = Palette.ThumbHeld }
        };

        /// <summary>An on/off switch: a square thumb and the word, sunk into a dark track while off, and edged under the pointer.</summary>
        public static readonly SwitchStyle Switch = new SwitchStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 16f,
                Color = Palette.Ink,
                Background = Palette.Line,
                OffTrack = Palette.Well,
                Thumb = Palette.Ink,
                WordPadding = new Thickness(8f, 4f),
                Padding = new Thickness(4f)
            },
            Hot = { Edge = Palette.ControlEdgeHot }
        };

        /// <summary>The game's button: a panel with a hairline round it, lifted under the pointer and lit while pressed.</summary>
        public static readonly ButtonStyle Button = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 16f,
                Color = Palette.Ink,
                Background = Palette.Panel,
                Edge = Palette.ControlEdge,
                Padding = new Thickness(16f, 8f)
            },
            Hot = { Background = Palette.PanelHot, Edge = Palette.ControlEdgeHot },
            Pressed = { Background = Palette.Line }
        };

        /// <summary>The button that finishes a screen or answers a dialog as Enter would, and none at all while there is nothing to finish, keeping its place.</summary>
        public static readonly ButtonStyle Primary = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 16f,
                Color = Palette.Chrome,
                Background = Palette.Accent,
                Padding = new Thickness(16f, 8f)
            },
            Hot = { Background = Palette.Ink },
            Disabled = { Color = Palette.None, Background = Palette.None }
        };

        /// <summary>A way to start something, offered in place: a plus and quiet words, and a panel under the pointer.</summary>
        public static readonly ButtonStyle Start = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Muted, Padding = new Thickness(16f, 8f) },
            Hot = { Background = Palette.Panel }
        };

        /// <summary>Words that do something when pressed, with no box round them, and a panel under the pointer.</summary>
        public static readonly ButtonStyle Plain = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Muted, Padding = new Thickness(16f, 8f) },
            Hot = { Background = Palette.Panel }
        };

        /// <summary>One of a set of plain words that is already in effect.</summary>
        public static readonly ButtonStyle Current = new ButtonStyle(Plain)
        {
            Base = { Color = Palette.Ink }
        };

        /// <summary>A symbol that does something, lit under the pointer and dimmed while there is nothing for it to do.</summary>
        public static readonly GlyphStyle Icon = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f, Tint = Palette.Muted },
            Hot = { Background = Palette.PanelHot, Tint = Palette.Ink },
            Disabled = { Tint = Palette.Dim }
        };

        /// <summary>A symbol that throws something away, lit in the color of something wrong under the pointer.</summary>
        public static readonly GlyphStyle Bin = new GlyphStyle(Icon)
        {
            Hot = { Background = Palette.ProblemWash, Tint = Palette.Problem }
        };

        /// <summary>The strip a banner saying something is wrong is opened by, lit by an edge in its color.</summary>
        public static readonly ElementStyle ProblemStrip = new ElementStyle
        {
            Base = { Padding = new Thickness(16f, 8f) },
            Hot = { Edge = Palette.Problem }
        };

        /// <summary>The same strip on a banner worth reading.</summary>
        public static readonly ElementStyle NoticeStrip = new ElementStyle(ProblemStrip)
        {
            Hot = { Edge = Palette.Notice }
        };

    }
}
