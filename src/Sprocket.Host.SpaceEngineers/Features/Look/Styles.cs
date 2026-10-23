using Lumel;
using Lumel.Styling;
using VRage.Game;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>How the screens' views look.</summary>
    public static class Styles
    {
        static readonly NineSlice Shade = new NineSlice(Materials.Shadow, 64, 24);

        /// <summary>The shadow a floating window casts, lying well away from the page as if lit from the top left.</summary>
        public static readonly Shadow Shadow = new Shadow(Shade, 24f, new VRageMath.Vector2(6f, 10f), Palette.Shadow);

        /// <summary>The shorter shadow a window hanging from a control casts, lying close to the page.</summary>
        public static readonly Shadow ShortShadow = new Shadow(Shade, 8f, new VRageMath.Vector2(2f, 4f), Palette.Shadow);

        /// <summary>A name, or a sentence that is the point of where it stands.</summary>
        public static readonly TextStyle Name = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink }
        };

        /// <summary>The name of something listed inside a group, a size under the group's own.</summary>
        public static readonly TextStyle InnerName = new TextStyle(Name)
        {
            Base = { FontSize = 20f }
        };

        /// <summary>What a list says while it has nothing in it.</summary>
        public static readonly TextStyle Empty = new TextStyle(Name)
        {
            Base = { Color = Palette.Muted, Padding = new Thickness(8f, 8f, 0f, 0f) }
        };

        /// <summary>A codex tip: the chrome's color, edged, with room inside the edge.</summary>
        public static readonly ElementStyle CodexTip = new ElementStyle
        {
            Base = { Background = Palette.Chrome, Edge = Palette.Line, Padding = new Thickness(12f, 8f) }
        };

        /// <summary>The words of a codex tip: quiet words, strong ones bold and emphasized ones italic, under a title in the bright ink.</summary>
        public static readonly RichTextStyle Tip = new RichTextStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                StrongFont = TextSizes.Bold,
                EmphasisFont = TextSizes.Italic,
                StrongEmphasisFont = TextSizes.BoldItalic,
                CodeFont = TextSizes.Mono,
                FontSize = 20f,
                Heading1Size = 24f,
                Heading2Size = 20f,
                Heading3Size = 20f,
                Color = Palette.Muted,
                StrongColor = Palette.Muted,
                EmphasisColor = Palette.Muted,
                LinkColor = Palette.Link,
                HeadingColor = Palette.Ink,
                RuleColor = Palette.Line,
                BlockSpacing = 8f,
                Indent = 20f
            }
        };

        /// <summary>The looks a box in a description or the codex can ask for: <c>warning</c> for what can go wrong, <c>player</c> for table-talk, in italics.</summary>
        public static readonly BoxStyles Boxes = new BoxStyles(
            new BoxStyle("warning", bar: Palette.Notice, fill: Palette.NoticeWash, color: Palette.Ink),
            new BoxStyle("player", bar: Palette.Accent, fill: Palette.Panel, italic: true));

        /// <summary>What the author of a blueprint wrote about it, read by whoever fills it in.</summary>
        public static readonly RichTextStyle Description = new RichTextStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                StrongFont = TextSizes.Bold,
                EmphasisFont = TextSizes.Italic,
                StrongEmphasisFont = TextSizes.BoldItalic,
                CodeFont = TextSizes.Mono,
                FontSize = 20f,
                Heading1Size = 40f,
                Heading2Size = 32f,
                Heading3Size = 24f,
                Color = Palette.Muted,
                StrongColor = Palette.Muted,
                EmphasisColor = Palette.Muted,
                LinkColor = Palette.Link,
                HeadingColor = Palette.Ink,
                RuleColor = Palette.Line,
                BlockSpacing = 8f,
                RuledHeadings = 2,
                Indent = 20f,
                Padding = new Thickness(20f, 4f),
                Boxes = Boxes
            }
        };

        /// <summary>A page of the codex, set in the reading face.</summary>
        public static readonly RichTextStyle Codex = new RichTextStyle(Description)
        {
            Base =
            {
                Font = TextSizes.Sans,
                StrongFont = TextSizes.SansBold,
                EmphasisFont = TextSizes.SansItalic,
                StrongEmphasisFont = TextSizes.SansBoldItalic,
                Padding = new Thickness(20f, 24f)
            }
        };

        /// <summary>What the author of a blueprint wrote about it, in the library under its name.</summary>
        public static readonly RichTextStyle LibraryAbout = new RichTextStyle(Description)
        {
            Base = { Padding = new Thickness(8f, 8f, 0f, 0f) }
        };

        /// <summary>What the author of a blueprint wrote about one of its slots, under the slot's name.</summary>
        public static readonly RichTextStyle SlotDescription = new RichTextStyle(Description)
        {
            Base =
            {
                FontSize = 16f,
                Heading1Size = 20f,
                Heading2Size = 16f,
                Heading3Size = 16f,
                Color = Palette.Faint,
                StrongColor = Palette.Faint,
                EmphasisColor = Palette.Faint,
                BlockSpacing = 4f,
                Indent = 16f,
                Padding = new Thickness(0f)
            }
        };

        /// <summary>A question hanging under the control that raised it, small and quiet above its answers.</summary>
        public static readonly RichTextStyle MenuQuestion = new RichTextStyle(SlotDescription);

        /// <summary>A description as it reads, with no room of its own around it.</summary>
        public static readonly RichTextStyle Example = new RichTextStyle(Description)
        {
            Base = { Padding = new Thickness(0f) }
        };

        /// <summary>The guide to rich text's window: the page's own color, edged, with nothing veiled behind it, casting a floating window's shadow.</summary>
        public static readonly WindowStyle GuideWindow = new WindowStyle
        {
            Base = { Background = Palette.Page, Edge = Palette.Line, Shadow = Shadow }
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
                StrongFont = TextSizes.Bold,
                EmphasisFont = TextSizes.Italic,
                StrongEmphasisFont = TextSizes.BoldItalic,
                CodeFont = TextSizes.Mono,
                FontSize = 24f,
                Color = Palette.Ink,
                StrongColor = Palette.Ink,
                EmphasisColor = Palette.Ink,
                LinkColor = Palette.Link,
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

        /// <summary>Names in a heading that are not the heading's own to give, in italics.</summary>
        public static readonly TextStyle TitleNameEmphasized = new TextStyle(TitleName)
        {
            Base = { Font = TextSizes.Italic }
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

        /// <summary>A box a value is typed into, in a list of answers.</summary>
        public static readonly TextBoxStyle Field = new TextBoxStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 24f,
                Color = Palette.Ink,
                CaretColor = Palette.Ink,
                SelectionColor = Palette.Line,
                HintColor = Palette.Faint,
                Background = Palette.Page,
                Edge = Palette.Line,
                Padding = new Thickness(12f, 8f)
            },
            Focused = { Edge = Palette.ControlEdgeHot }
        };

        /// <summary>The box a heading's name is typed in, holding a name that cannot be given.</summary>
        public static readonly TextBoxStyle TitleRefused = new TextBoxStyle(TitleBox)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>A name in a row that can be renamed in place, spaced as its box is so the box takes its place without moving it.</summary>
        public static readonly TextStyle PlateName = new TextStyle(Name)
        {
            Base = { Padding = new Thickness(8f, 4f) }
        };

        /// <summary>Names in a row that are not the row's own to give, in italics.</summary>
        public static readonly TextStyle PlateNameEmphasized = new TextStyle(PlateName)
        {
            Base = { Font = TextSizes.Italic }
        };

        /// <summary>The box a name in a row is typed in, a well on the page in the name's size.</summary>
        public static readonly TextBoxStyle PlateBox = new TextBoxStyle(TitleBox)
        {
            Base = { FontSize = 24f }
        };

        /// <summary>The box a name in a row is typed in, holding a name that cannot be given.</summary>
        public static readonly TextBoxStyle PlateRefused = new TextBoxStyle(PlateBox)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>A field holding what cannot be taken, marked as a heading's box is.</summary>
        public static readonly TextBoxStyle FieldRefused = new TextBoxStyle(Field)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>A heading's name with its pencil, lit under the pointer where pressing it renames.</summary>
        public static readonly ElementStyle Plate = new ElementStyle
        {
            Hot = { Background = Palette.Panel }
        };

        /// <summary>A name that opens a menu rather than being renamed at a press, drawn as the game's button around its words.</summary>
        public static readonly ElementStyle PlateButton = new ElementStyle
        {
            Base = { Background = Palette.Panel, Edge = Palette.ControlEdge, Padding = new Thickness(8f, 4f) },
            Hot = { Background = Palette.PanelHot, Edge = Palette.ControlEdgeHot }
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

        /// <summary>A section's name, in capitals, over what it heads, where the name is a term in the codex.</summary>
        public static readonly RichTextStyle HeadingTerm = new RichTextStyle(Section)
        {
            Base = { EmphasisColor = Palette.Ink, LinkColor = Palette.Link, Padding = new Thickness(4f, 8f, 0f, 8f) }
        };

        /// <summary>A section's name a size down, heading part of a section, where the name is a term in the codex.</summary>
        public static readonly RichTextStyle SubHeadingTerm = new RichTextStyle(HeadingTerm)
        {
            Base = { FontSize = 16f, Padding = new Thickness(4f, 0f, 0f, 8f) }
        };

        /// <summary>A section's name following other content, with room above it, where the name holds a term in the codex.</summary>
        public static readonly RichTextStyle NextHeadingTerm = new RichTextStyle(HeadingTerm)
        {
            Base = { Padding = new Thickness(4f, 48f, 0f, 8f) }
        };

        /// <summary>A section's name, in capitals, over what it heads.</summary>
        public static readonly TextStyle Heading = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Faint, Padding = new Thickness(4f, 8f, 0f, 8f) }
        };

        /// <summary>A heading within a section, a size down from the section's own, over the part of it that it names.</summary>
        public static readonly TextStyle SubHeading = new TextStyle(Heading)
        {
            Base = { FontSize = 16f, Padding = new Thickness(4f, 0f, 0f, 0f) }
        };

        /// <summary>A section's name following other content, with room above it so it reads as the start of what comes after.</summary>
        public static readonly TextStyle NextHeading = new TextStyle(Heading)
        {
            Base = { Padding = new Thickness(4f, 48f, 0f, 8f) }
        };

        /// <summary>The quiet line under a name: what it is doing, and when it last did anything.</summary>
        public static readonly TextStyle Needs = new TextStyle(Heading)
        {
            Base = { Color = Palette.Notice }
        };

        /// <summary>A small note under or beside something, in the faint tone.</summary>
        public static readonly TextStyle Detail = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Faint }
        };

        /// <summary>Table-talk said in a line of its own: a word about the game itself rather than the world, in italics.</summary>
        public static readonly TextStyle TableTalk = new TextStyle
        {
            Base = { Font = TextSizes.Italic, FontSize = 16f, Color = Palette.Faint }
        };

        /// <summary>What a thing is doing, said in a line of its own tools rather than under its name.</summary>
        public static readonly TextStyle Status = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Muted }
        };

        /// <summary>The quieter words beside a status: when it last did anything, a step's number, why nothing can be added.</summary>
        public static readonly TextStyle Aside = new TextStyle(Status)
        {
            Base = { Color = Palette.Faint }
        };

        /// <summary>A word on an amber wash, saying something asked for did not happen.</summary>
        public static readonly TextStyle Skipped = new TextStyle(Status)
        {
            Base = { Color = Palette.Notice, Background = Palette.NoticeWash, Padding = new Thickness(16f, 8f) }
        };

        /// <summary>What is wrong with a thing, on its own line under it.</summary>
        public static readonly TextStyle Problem = new TextStyle(Detail)
        {
            Base = { Color = Palette.Problem }
        };

        /// <summary>What an answer does that somebody should know before giving it, on its own line under it.</summary>
        public static readonly TextStyle Caution = new TextStyle(Detail)
        {
            Base = { Color = Palette.Notice }
        };

        /// <summary>One answer in a list of them, pressed to give it.</summary>
        public static readonly ElementStyle Answer = new ElementStyle
        {
            Base = { Background = Palette.Panel, Padding = new Thickness(20f, 16f) },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>The answer already given, lit a step at rest, so it goes a step further again under the pointer.</summary>
        public static readonly ElementStyle Given = new ElementStyle(Answer)
        {
            Base = { Background = Palette.PanelHot },
            Hot = { Background = Palette.Line }
        };

        /// <summary>The words of a sentence around the parts somebody chose.</summary>
        public static readonly TextStyle Phrase = new TextStyle(Name)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>The words of a sentence that is paused, quieter than those of one that runs.</summary>
        public static readonly TextStyle PausedPhrase = new TextStyle(Phrase)
        {
            Base = { Color = Palette.Dim }
        };

        /// <summary>A part of a sentence: the game's button in a name's size, its words in the warning color while nothing fills it, and the words alone where it cannot be pressed.</summary>
        public static readonly SlotStyle Slot = new SlotStyle
        {
            Base =
            {
                Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink, Background = Palette.Panel, Edge = Palette.ControlEdge,
                Padding = new Thickness(16f, 8f)
            },
            Empty = { Color = Palette.Notice },
            Still = { Background = Palette.None, Edge = Palette.None },
            Hot = { Background = Palette.PanelHot, Edge = Palette.ControlEdgeHot }
        };

        /// <summary>A part of a sentence that is paused: there to be read and pressed, and quieter than one that runs.</summary>
        public static readonly SlotStyle PausedSlot = new SlotStyle(Slot)
        {
            Base = { Color = Palette.Dim, Background = Palette.Chrome }
        };

        /// <summary>A word on a green wash, saying something is true at this instant.</summary>
        public static readonly TextStyle Live = new TextStyle
        {
            Base =
            {
                Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Live, Background = Palette.LiveWash,
                Padding = new Thickness(16f, 8f)
            }
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

        /// <summary>The row chosen from a list, whose details stand beside it.</summary>
        public static readonly ElementStyle Chosen = new ElementStyle(Row)
        {
            Base = { Background = Palette.PanelHot, Edge = Palette.Accent }
        };

        /// <summary>The chosen row, with something wrong in it.</summary>
        public static readonly ElementStyle BrokenChosen = new ElementStyle(Broken)
        {
            Base = { Edge = Palette.Accent }
        };

        /// <summary>A row that has stopped where it is waiting to be let on, or is switched off: a warning, washed amber.</summary>
        public static readonly ElementStyle Parked = new ElementStyle(Row)
        {
            Base = { Background = Palette.NoticeWash }
        };

        /// <summary>A row with nothing to open.</summary>
        public static readonly ElementStyle Still = new ElementStyle(Row)
        {
            Base = { Background = Palette.None },
            Hot = { Background = Palette.None }
        };

        /// <summary>A state light, whose color the view sets from the state.</summary>
        public static readonly GlyphStyle Light = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 8f, Tint = Palette.Dim }
        };

        /// <summary>A row's light drawn as a symbol of what the row is, large enough to be read rather than only seen.</summary>
        public static readonly GlyphStyle Marked = new GlyphStyle(Light)
        {
            Base = { FontSize = 20f }
        };

        /// <summary>A mark beside a name, whose color the view sets from what it marks.</summary>
        public static readonly GlyphStyle Flag = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f }
        };

        /// <summary>The mark after a tab's words saying something on its page wants looking at, whose color the view sets from how much it matters.</summary>
        public static readonly GlyphStyle TabMark = new GlyphStyle(Flag)
        {
            Base = { FontSize = 16f, Padding = new Thickness(8f, 0f, 0f, 0f) }
        };

        /// <summary>The chevron that opens a banner, whose color the view sets from the banner's.</summary>
        public static readonly GlyphStyle Chevron = new GlyphStyle(Flag);

        /// <summary>A screen's scroll, whose bar is as wide as the one the game reserves for its own.</summary>
        public static readonly ScrollViewStyle ScrollView = new ScrollViewStyle
        {
            Base = { BarWidth = Sizes.Bar, Thumb = Palette.Thumb, LeastThumb = 32f },
            Held = { Thumb = Palette.ThumbHeld }
        };

        /// <summary>Text shown as it will read in place of the box it is written in, framed as that box is so the editor keeps its edge.</summary>
        public static readonly ScrollViewStyle Preview = new ScrollViewStyle(ScrollView)
        {
            Base = { Background = Palette.Page, Edge = Palette.Line, Padding = new Thickness(8f, 6f) }
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
                OnThumb = Palette.Ink,
                OffThumb = Palette.Ink,
                WordPadding = new Thickness(8f, 4f),
                Padding = new Thickness(4f)
            },
            Hot = { Edge = Palette.ControlEdgeHot },
            Disabled = { Color = Palette.Dim, OnThumb = Palette.Dim, OffThumb = Palette.Dim, Background = Palette.Well, OffTrack = Palette.Well }
        };

        /// <summary>A logic system's or an installation's switch, its thumb amber while off, as the row or heading bar holding it is washed.</summary>
        public static readonly SwitchStyle SystemSwitch = new SwitchStyle(Switch)
        {
            Base = { OffThumb = Palette.Notice }
        };

        /// <summary>The game's button: a panel with a hairline round it, lifted under the pointer, lit while pressed, and its words dimmed while it cannot be used.</summary>
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
            Pressed = { Background = Palette.Line },
            Disabled = { Color = Palette.Dim }
        };

        /// <summary>A slot's name on the installation panel where pressing it opens the slot to be named and described: the game's button, in a name's size.</summary>
        /// <remarks>Padded as a block chip beside it is, so the name stands level with the first block.</remarks>
        public static readonly ButtonStyle SlotName = new ButtonStyle(Button)
        {
            Base = { FontSize = 24f }
        };

        /// <summary>A slot's name on the installation panel where pressing it opens nothing: the words alone, where the button's would stand.</summary>
        public static readonly ButtonStyle SlotNameStill = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink, Padding = new Thickness(16f, 8f) }
        };

        /// <summary>The header of one page of several, as the game draws the tabs across its terminal, the page showing on the lightest plate.</summary>
        public static readonly TabStyle Tab = new TabStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 20f,
                Color = Palette.Muted,
                Background = Palette.Panel,
                Edge = Palette.Line,
                Padding = new Thickness(8f, 8f)
            },
            Hot = { Background = Palette.PanelHot, Color = Palette.Ink },
            Current = { Background = Palette.Line, Color = Palette.Ink }
        };

        /// <summary>The button that finishes a screen or answers a dialog as Enter would, grayed while there is nothing to finish.</summary>
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
            Disabled = { Color = Palette.Dim, Background = Palette.Panel }
        };

        /// <summary>A way to start something, offered in place: a plus and quiet words, and a panel under the pointer.</summary>
        public static readonly ButtonStyle Start = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Muted, Padding = new Thickness(16f, 4f) },
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

        /// <summary>The mark a drop-down line ends with, smaller than a symbol pressed on its own.</summary>
        public static readonly GlyphStyle ListMark = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 12f, Tint = Palette.Muted }
        };

        /// <summary>A symbol that pauses something, smaller than the icons round it while nothing is paused.</summary>
        public static readonly GlyphStyle Holds = new GlyphStyle(Icon)
        {
            Base = { FontSize = 12f }
        };

        /// <summary>A symbol that pauses something, lit in the color of something worth noticing while it is paused.</summary>
        public static readonly GlyphStyle Held = new GlyphStyle(Icon)
        {
            Base = { Background = Palette.NoticeWash, Tint = Palette.Notice },
            Hot = { Background = Palette.NoticeWash, Tint = Palette.Ink }
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

        /// <summary>The strip a banner with a warning is opened by, lit by an edge in its color.</summary>
        public static readonly ElementStyle NoticeStrip = new ElementStyle(ProblemStrip)
        {
            Hot = { Edge = Palette.Notice }
        };

        /// <summary>The strip a banner with a note is opened by, lit in the ink of its words.</summary>
        public static readonly ElementStyle InfoStrip = new ElementStyle(ProblemStrip)
        {
            Hot = { Edge = Palette.Ink }
        };

        /// <summary>A box a value is typed into, in a form of values.</summary>
        public static readonly TextBoxStyle Value = new TextBoxStyle(Field)
        {
            Base = { FontSize = 20f, Padding = new Thickness(12f, 8f) }
        };

        /// <summary>A box a value is typed into, in a form of values, holding something its value will not take.</summary>
        public static readonly TextBoxStyle ValueWrong = new TextBoxStyle(Value)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>What a value in a form is, beside where it is given.</summary>
        /// <remarks>Muted rather than faint, for 6.6:1 against the panel, where faint gives 4.5:1, the least WCAG allows.</remarks>
        public static readonly TextStyle ValueLabel = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Muted }
        };

        /// <summary>Why a form cannot be finished as it stands, beside the way to finish it.</summary>
        public static readonly TextStyle Refusal = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Problem }
        };

        /// <summary>One of a few words a value may be, pressed to give it.</summary>
        public static readonly ButtonStyle Word = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 20f,
                Color = Palette.Muted,
                Background = Palette.Well,
                Edge = Palette.Line,
                Padding = new Thickness(12f, 4f)
            },
            Hot = { Background = Palette.Panel, Edge = Palette.Muted }
        };

        /// <summary>The word a value has been given.</summary>
        public static readonly ButtonStyle WordChosen = new ButtonStyle(Word)
        {
            Base = { Color = Palette.Ink, Background = Palette.Line }
        };

        /// <summary>The line a list hangs from, saying what is chosen in it, edged while it holds the keyboard.</summary>
        public static readonly ElementStyle ListLine = new ElementStyle
        {
            Base = { Background = Palette.Well, Edge = Palette.Line, Padding = new Thickness(12f, 8f) },
            Hot = { Background = Palette.Panel, Edge = Palette.Muted }
        };

        /// <summary>The line a list hangs from where it stands in a column's heading, as tall as the heading's words so nothing under it moves.</summary>
        public static readonly ElementStyle ListLineInHeading = new ElementStyle(ListLine)
        {
            Base = { Padding = new Thickness(12f, 2f) }
        };

        /// <summary>The line a list hangs from, saying a word its value will not take.</summary>
        public static readonly ElementStyle ListLineWrong = new ElementStyle(ListLine)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>Controls hanging from what opened them, on the ground a form's controls stand on, so they look as they do in a form.</summary>
        public static readonly WindowStyle Panel = new WindowStyle
        {
            Base = { Background = Palette.Panel, Edge = Palette.Line, Padding = new Thickness(12f), Shadow = ShortShadow }
        };

        /// <summary>A menu or a list hanging open from what opened it, on the tone every hanging thing shares: the one a form's controls stand on.</summary>
        public static readonly WindowStyle Menu = new WindowStyle
        {
            Base = { Background = Palette.Panel, Edge = Palette.Line, Padding = new Thickness(8f), Shadow = ShortShadow }
        };

        /// <summary>One entry of a menu, lit a step past the menu's own tone under the pointer.</summary>
        public static readonly ElementStyle MenuRow = new ElementStyle
        {
            Base = { Padding = new Thickness(12f, 8f) },
            Hot = { Background = Palette.Line }
        };

        /// <summary>An entry of a menu listed under another, set in from it.</summary>
        public static readonly ElementStyle MenuRowNested = new ElementStyle
        {
            Base = { Padding = new Thickness(32f, 8f, 12f, 8f) },
            Hot = { Background = Palette.Line }
        };

        /// <summary>The entry of a menu that Enter takes, drawn the way every other thing that carries on is drawn.</summary>
        public static readonly ElementStyle MenuRowTaken = new ElementStyle(MenuRow)
        {
            Base = { Background = Palette.Accent },
            Hot = { Background = Palette.Ink }
        };

        /// <summary>The words of an entry in a list hanging open.</summary>
        public static readonly TextStyle ListWord = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Ink }
        };

        /// <summary>The words of the entry Enter takes, on its accent.</summary>
        public static readonly TextStyle ListWordTaken = new TextStyle(ListWord)
        {
            Base = { Color = Palette.Chrome }
        };

        /// <summary>One column of a pick: a panel with a hairline round it.</summary>
        public static readonly ElementStyle Column = new ElementStyle
        {
            Base = { Background = Palette.Panel, Edge = Palette.Line, Padding = new Thickness(16f) }
        };

        /// <summary>A column standing for a page of a pick, pressed anywhere down its length to turn to it.</summary>
        public static readonly ElementStyle Fold = new ElementStyle
        {
            Base = { Background = Palette.Panel, Edge = Palette.Line, Padding = new Thickness(0f, 16f) },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>What a column of a pick asks.</summary>
        public static readonly TextStyle ColumnTitle = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink }
        };

        /// <summary>What a column of a pick asks, where its words may be a term in the codex.</summary>
        public static readonly RichTextStyle ColumnTitleTerm = new RichTextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink, LinkColor = Palette.Link }
        };

        /// <summary>A quiet line under what a column asks, or about what it lists.</summary>
        /// <remarks>Muted rather than faint, for 6.6:1 against the panel, where faint gives 4.5:1, the least WCAG allows.</remarks>
        public static readonly TextStyle ColumnNote = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Muted }
        };

        /// <summary>A box a list is narrowed by typing into.</summary>
        public static readonly TextBoxStyle Search = new TextBoxStyle(Value);

        /// <summary>A line over a list saying what it is narrowed by, on a wash of its own.</summary>
        public static readonly TextStyle Note = new TextStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = 20f,
                Color = Palette.Muted,
                Background = Palette.SlotFill,
                Padding = new Thickness(16f, 8f)
            }
        };

        /// <summary>What a list says where it has nothing in it.</summary>
        public static readonly TextStyle Nothing = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Muted, Padding = new Thickness(4f, 8f, 0f, 0f) }
        };

        /// <summary>A quiet link at the end of a column's heading to another way of looking at its list.</summary>
        public static readonly ButtonStyle ViewLink = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Muted, Padding = new Thickness(8f, 4f) },
            Hot = { Background = Palette.PanelHot, Color = Palette.Ink }
        };

        /// <summary>A thing to choose in a column, lit under the pointer.</summary>
        public static readonly ElementStyle Pick = new ElementStyle
        {
            Base = { Padding = new Thickness(0f, 0f, 12f, 0f) },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>The one thing settled on in a column, on the highlight tone at rest and a step past it under the pointer.</summary>
        public static readonly ElementStyle Settled = new ElementStyle(Pick)
        {
            Base = { Background = Palette.PanelHot },
            Hot = { Background = Palette.Line }
        };

        /// <summary>A thing listed to be read about and not chosen, or a heading over a group of them.</summary>
        public static readonly ElementStyle Unpicked = new ElementStyle(Pick)
        {
            Hot = { Background = Palette.None }
        };

        /// <summary>A heading over a group of things to choose, against the first of them.</summary>
        public static readonly ElementStyle PickHeading = new ElementStyle(Unpicked)
        {
            Base = { Padding = new Thickness(0f, 0f, 12f, 0f) }
        };

        /// <summary>The name of a thing to choose, at the size the game writes a block list in.</summary>
        public static readonly TextStyle PickName = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink }
        };

        /// <summary>The name of a thing listed only to be read about.</summary>
        public static readonly TextStyle PickWithheld = new TextStyle(PickName)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>A heading over a group of things to choose.</summary>
        public static readonly TextStyle PickGroup = new TextStyle(PickName)
        {
            Base = { Color = Palette.Faint }
        };

        /// <summary>A block's picture beside its name in a column, on a well of its own.</summary>
        public static readonly ElementStyle Picture = new ElementStyle
        {
            Base = { Background = Palette.SlotFill }
        };

        /// <summary>A toolbar slot that holds nothing, offered under a toolbar a person presses.</summary>
        public static readonly ElementStyle SlotFree = new ElementStyle
        {
            Base = { Background = Palette.Well, Edge = Palette.Line },
            Hot = { Background = Palette.Panel, Edge = Palette.Muted }
        };

        /// <summary>A toolbar slot holding something of the game's: an action, a group's action or a thing.</summary>
        public static readonly ElementStyle SlotTaken = new ElementStyle
        {
            Base = { Background = Palette.SlotFill, Edge = Palette.SlotEdge },
            Hot = { Background = Palette.PanelHot, Edge = Palette.Ink }
        };

        /// <summary>A toolbar slot already answering a signal of the automation.</summary>
        public static readonly ElementStyle SlotAnswering = new ElementStyle
        {
            Base = { Background = Palette.LiveWash, Edge = Palette.Live },
            Hot = { Background = Palette.PanelHot, Edge = Palette.Live }
        };

        /// <summary>The ring around the toolbar slot chosen, over whatever the slot holds.</summary>
        public static readonly ElementStyle SlotChosen = new ElementStyle
        {
            Base = { Edge = Palette.Ink }
        };

        /// <summary>A toolbar slot's number, in its corner.</summary>
        public static readonly TextStyle SlotNumber = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Ink, Padding = new Thickness(4f, 0f) }
        };

        /// <summary>A page of a toolbar, offered beside its other pages.</summary>
        public static readonly ButtonStyle SlotPage = new ButtonStyle(Word)
        {
            Base = { FontSize = 16f, Padding = new Thickness(8f, 2f) }
        };

        /// <summary>A page of a toolbar where something already sits in one of its slots.</summary>
        public static readonly ButtonStyle SlotPageTaken = new ButtonStyle(SlotPage)
        {
            Base = { Color = Palette.Ink }
        };

        /// <summary>The page of a toolbar being shown.</summary>
        public static readonly ButtonStyle SlotPageChosen = new ButtonStyle(SlotPage)
        {
            Base = { Color = Palette.Ink, Background = Palette.Line }
        };

        /// <summary>A mark saying something has not been chosen: a ring for one of several, a box for any of them.</summary>
        public static readonly GlyphStyle Mark = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 16f, Tint = Palette.Faint }
        };

        /// <summary>A mark saying something has not been chosen, where it is pressed by itself, lit under the pointer.</summary>
        public static readonly GlyphStyle MarkPressed = new GlyphStyle(Mark)
        {
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>A mark saying something has been chosen.</summary>
        public static readonly GlyphStyle MarkOn = new GlyphStyle(Mark)
        {
            Base = { Tint = Palette.Live }
        };

        /// <summary>A mark saying something has been chosen, where it is pressed by itself.</summary>
        public static readonly GlyphStyle MarkOnPressed = new GlyphStyle(MarkOn)
        {
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>A mark beside words, muted: the kind of thing they name, or which way a folded column opens.</summary>
        public static readonly GlyphStyle Waits = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f, Tint = Palette.Muted }
        };

        /// <summary>A mark beside words on a paused step, as quiet as its words.</summary>
        public static readonly GlyphStyle WaitsPaused = new GlyphStyle(Waits)
        {
            Base = { Tint = Palette.Dim }
        };

        /// <summary>A choice about the picking itself, lit under the pointer.</summary>
        public static readonly ElementStyle Option = new ElementStyle
        {
            Base = { Padding = new Thickness(12f, 4f) },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>The words of a choice about the picking itself.</summary>
        public static readonly TextStyle OptionWord = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Muted }
        };

        /// <summary>The words of the choice about the picking that holds.</summary>
        public static readonly TextStyle OptionChosen = new TextStyle(OptionWord)
        {
            Base = { Color = Palette.Ink }
        };

        /// <summary>A count beside what it counts, quieter than it.</summary>
        public static readonly TextStyle Count = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 16f, Color = Palette.Faint }
        };

    }
}
