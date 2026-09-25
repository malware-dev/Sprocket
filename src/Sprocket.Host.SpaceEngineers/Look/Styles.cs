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
        }.Frozen();

        /// <summary>What a list says while it has nothing in it.</summary>
        public static readonly TextStyle Empty = new TextStyle(Name)
        {
            Base = { Color = Palette.Muted, Padding = new Thickness(8f, 8f, 0f, 0f) }
        }.Frozen();

        /// <summary>The quiet line under a name: what it is doing, and when it last did anything.</summary>
        public static readonly TextStyle Detail = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Faint }
        }.Frozen();

        /// <summary>The rest of what a banner says, once it is opened.</summary>
        public static readonly TextStyle Opened = new TextStyle(Name)
        {
            Base = { Color = Palette.Faint, Padding = new Thickness(16f, 0f, 16f, 8f) }
        }.Frozen();

        /// <summary>What went wrong, in the words of whatever found it, for whoever mends it.</summary>
        public static readonly TextStyle Diagnostic = new TextStyle(Detail)
        {
            Base = { Color = Palette.Dim, Padding = new Thickness(16f, 0f, 16f, 8f) }
        }.Frozen();

        /// <summary>A row of a list that opens what it names.</summary>
        public static readonly ElementStyle Row = new ElementStyle
        {
            Base = { Background = Palette.Panel, Padding = new Thickness(16f, 8f) },
            Hot = { Background = Palette.PanelHot }
        }.Frozen();

        /// <summary>A row whose sequence is running.</summary>
        public static readonly ElementStyle Running = new ElementStyle(Row)
        {
            Base = { Background = Palette.LiveWash }
        }.Frozen();

        /// <summary>A row with something wrong in it.</summary>
        public static readonly ElementStyle Broken = new ElementStyle(Row)
        {
            Base = { Background = Palette.ProblemWash }
        }.Frozen();

        /// <summary>A row that has stopped where it is waiting to be let on.</summary>
        public static readonly ElementStyle Parked = new ElementStyle(Row)
        {
            Base = { Background = Palette.NoticeWash }
        }.Frozen();

        /// <summary>A row that adds something rather than naming it.</summary>
        public static readonly ElementStyle AddRow = new ElementStyle(Row)
        {
            Base = { Background = Palette.None },
            Hot = { Background = Palette.Panel }
        }.Frozen();

        /// <summary>A row with nothing to open.</summary>
        public static readonly ElementStyle Still = new ElementStyle(Row)
        {
            Base = { Background = Palette.None },
            Hot = { Background = Palette.None }
        }.Frozen();

        /// <summary>A state light, whose colour the view sets from the state.</summary>
        public static readonly GlyphStyle Light = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 16f, Tint = Palette.Dim }
        }.Frozen();

        /// <summary>A mark beside a name, whose colour the view sets from what it marks.</summary>
        public static readonly GlyphStyle Flag = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 32f }
        }.Frozen();

        /// <summary>The chevron that opens a banner, whose colour the view sets from the banner's.</summary>
        public static readonly GlyphStyle Chevron = new GlyphStyle(Flag).Frozen();

        /// <summary>A symbol that does something to the row it stands on.</summary>
        public static readonly GlyphStyle Tool = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 32f, Tint = Palette.Faint },
            Hot = { Background = Palette.PanelHot, Tint = Palette.Ink },
            Disabled = { Tint = Palette.Dim }
        }.Frozen();

        /// <summary>A symbol that throws something away.</summary>
        public static readonly GlyphStyle Bin = new GlyphStyle(Tool)
        {
            Hot = { Background = Palette.ProblemWash, Tint = Palette.Problem }
        }.Frozen();

        /// <summary>A switch that governs one thing.</summary>
        public static readonly SwitchStyle Switch = new SwitchStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 16f,
                Color = Palette.Ink,
                QuietColor = Palette.Faint,
                LitColor = Palette.SwitchLit,
                UnlitColor = Palette.SwitchDark,
                WordPadding = new Thickness(16f, 8f),
                Edge = Palette.Dim
            },
            Hot = { Edge = Palette.Muted }
        }.Frozen();

        /// <summary>A switch that governs more than one thing, which draws its edge brighter.</summary>
        public static readonly SwitchStyle Governing = new SwitchStyle(Switch)
        {
            Base = { Edge = Palette.Faint }
        }.Frozen();

        /// <summary>A way to start something, offered in place: quiet words, and a panel under the pointer.</summary>
        public static readonly ButtonStyle Way = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Muted, Padding = new Thickness(16f, 8f) },
            Hot = { Background = Palette.Panel }
        }.Frozen();

        /// <summary>The quiet line under a name, where pressing it changes what it says.</summary>
        public static readonly ButtonStyle Status = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Faint },
            Hot = { Background = Palette.Panel }
        }.Frozen();

        /// <summary>The strip a banner saying something is wrong is opened by, lit by an edge in its colour.</summary>
        public static readonly ElementStyle ProblemStrip = new ElementStyle
        {
            Base = { Padding = new Thickness(16f, 8f) },
            Hot = { Edge = Palette.Problem }
        }.Frozen();

        /// <summary>The same strip on a banner worth reading.</summary>
        public static readonly ElementStyle NoticeStrip = new ElementStyle(ProblemStrip)
        {
            Hot = { Edge = Palette.Notice }
        }.Frozen();

        /// <summary>A way to mend what a banner saying something is wrong is about.</summary>
        public static readonly ButtonStyle ProblemAction = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 24f,
                Color = Palette.Problem,
                Background = Palette.Panel,
                Padding = new Thickness(16f, 8f)
            },
            Hot = { Background = Palette.PanelHot }
        }.Frozen();

        /// <summary>A way to act on what a banner worth reading is about.</summary>
        public static readonly ButtonStyle NoticeAction = new ButtonStyle(ProblemAction)
        {
            Base = { Color = Palette.Notice }
        }.Frozen();
    }
}
