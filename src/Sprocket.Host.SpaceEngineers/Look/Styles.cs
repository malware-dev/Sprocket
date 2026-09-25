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

        /// <summary>A way to add something, standing in a list among names.</summary>
        public static readonly TextStyle Adding = new TextStyle(Name)
        {
            Base = { Color = Palette.Muted }
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

        /// <summary>What is wrong, in place of the quiet line.</summary>
        public static readonly TextStyle Wrong = new TextStyle(Detail)
        {
            Base = { Color = Palette.Problem }
        }.Frozen();

        /// <summary>What is worth reading, in place of the quiet line.</summary>
        public static readonly TextStyle Warning = new TextStyle(Detail)
        {
            Base = { Color = Palette.Notice }
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

        /// <summary>A state light that is off.</summary>
        public static readonly ElementStyle Off = new ElementStyle
        {
            Base = { Background = Palette.Dim }
        }.Frozen();

        /// <summary>A state light that is on.</summary>
        public static readonly ElementStyle On = new ElementStyle
        {
            Base = { Background = Palette.Live }
        }.Frozen();

        /// <summary>A state light saying something is wrong.</summary>
        public static readonly ElementStyle Fault = new ElementStyle
        {
            Base = { Background = Palette.Problem }
        }.Frozen();

        /// <summary>No state light, its room kept.</summary>
        public static readonly ElementStyle Dark = new ElementStyle().Frozen();

        /// <summary>A symbol that does something to the row it stands on.</summary>
        public static readonly GlyphStyle Tool = new GlyphStyle
        {
            Base = { FontSize = 32f, Tint = Palette.Faint },
            Hot = { Background = Palette.PanelHot, Tint = Palette.Ink },
            Disabled = { Tint = Palette.Dim }
        }.Frozen();

        /// <summary>A symbol that throws something away.</summary>
        public static readonly GlyphStyle Bin = new GlyphStyle(Tool)
        {
            Hot = { Background = Palette.ProblemWash, Tint = Palette.Problem }
        }.Frozen();

        /// <summary>The mark beside a name with something wrong under it.</summary>
        public static readonly GlyphStyle Fails = new GlyphStyle
        {
            Base = { FontSize = 32f, Tint = Palette.Problem }
        }.Frozen();

        /// <summary>The mark beside a name with something worth reading under it.</summary>
        public static readonly GlyphStyle Warns = new GlyphStyle(Fails)
        {
            Base = { Tint = Palette.Notice }
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

        /// <summary>A banner saying something is wrong.</summary>
        public static readonly ElementStyle Problem = new ElementStyle
        {
            Base = { Background = Palette.ProblemWash }
        }.Frozen();

        /// <summary>A banner saying something is worth reading.</summary>
        public static readonly ElementStyle Notice = new ElementStyle
        {
            Base = { Background = Palette.NoticeWash }
        }.Frozen();

        /// <summary>The strip a banner is opened by, lit by an edge in its own colour.</summary>
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

        /// <summary>What a banner saying something is wrong says.</summary>
        public static readonly TextStyle ProblemText = new TextStyle(Name)
        {
            Base = { Color = Palette.Problem }
        }.Frozen();

        /// <summary>What a banner worth reading says.</summary>
        public static readonly TextStyle NoticeText = new TextStyle(Name)
        {
            Base = { Color = Palette.Notice }
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

        /// <summary>The chevron that opens a banner saying something is wrong.</summary>
        public static readonly GlyphStyle ProblemChevron = new GlyphStyle
        {
            Base = { FontSize = 32f, Tint = Palette.Problem }
        }.Frozen();

        /// <summary>The chevron that opens a banner worth reading.</summary>
        public static readonly GlyphStyle NoticeChevron = new GlyphStyle(ProblemChevron)
        {
            Base = { Tint = Palette.Notice }
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
