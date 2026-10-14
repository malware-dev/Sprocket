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
    /// <summary>How the views converted to the size scale look.</summary>
    public static class Styles
    {
        /// <summary>The soft square every shadow is drawn with.</summary>
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

        /// <summary>The note beside the pointer: quiet words, strong ones bold and emphasized ones italic.</summary>
        public static readonly RichTextStyle Tip = new RichTextStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                StrongFont = Lettering.Bold,
                EmphasisFont = Lettering.Italic,
                StrongEmphasisFont = Lettering.BoldItalic,
                CodeFont = Lettering.Mono,
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
                Indent = 20f,
                Background = Palette.Chrome,
                Edge = Palette.Line,
                Padding = new Thickness(12f, 8f)
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
                StrongFont = Lettering.Bold,
                EmphasisFont = Lettering.Italic,
                StrongEmphasisFont = Lettering.BoldItalic,
                CodeFont = Lettering.Mono,
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
                Font = Lettering.Sans,
                StrongFont = Lettering.SansBold,
                EmphasisFont = Lettering.SansItalic,
                StrongEmphasisFont = Lettering.SansBoldItalic,
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

        /// <summary>The codex's window: the page's own color, edged, with nothing veiled behind it, casting a floating window's shadow.</summary>
        public static readonly WindowStyle CodexWindow = new WindowStyle
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
                StrongFont = Lettering.Bold,
                EmphasisFont = Lettering.Italic,
                StrongEmphasisFont = Lettering.BoldItalic,
                CodeFont = Lettering.Mono,
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

        /// <summary>The same box holding a name that cannot be given.</summary>
        public static readonly TextBoxStyle TitleRefused = new TextBoxStyle(TitleBox)
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

        /// <summary>The same, a size down, heading part of a section.</summary>
        public static readonly RichTextStyle SubHeadingTerm = new RichTextStyle(HeadingTerm)
        {
            Base = { FontSize = 16f, Padding = new Thickness(4f, 0f, 0f, 8f) }
        };

        /// <summary>A section's name, in capitals, over what it heads.</summary>
        public static readonly TextStyle Heading = new TextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = 20f, Color = Palette.Faint, Padding = new Thickness(4f, 8f, 0f, 8f) }
        };

        /// <summary>A heading within a section, a size down from the section's own, over the part of it that it names.</summary>
        public static readonly TextStyle SubHeading = new TextStyle(Heading)
        {
            Base = { FontSize = 16f, Padding = new Thickness(4f, 0f, 0f, 8f) }
        };

        /// <summary>The same, following other content, with room above it so it reads as the start of what comes after.</summary>
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
            Base = { Font = Lettering.Italic, FontSize = 16f, Color = Palette.Faint }
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

        /// <summary>A part of a sentence: a chip with a line along its bottom, the one look nothing else may borrow.</summary>
        /// <remarks>
        /// One that cannot be pressed keeps its place and its shape, lifted only a notch off the page so the words still
        /// read as one part. Color alone marks one still waiting to be filled, because the game ships one weight and
        /// there is no italic to lean it into.
        /// </remarks>
        public static readonly SlotStyle Slot = new SlotStyle
        {
            Base =
            {
                Font = MyFontEnum.White, FontSize = 24f, Color = Palette.Ink, Background = Palette.SlotFill,
                Padding = new Thickness(16f, 8f), Underline = Palette.SlotEdge
            },
            Empty = { Color = Palette.Notice, Underline = Palette.Notice },
            Still = { Background = Palette.Chrome, Underline = Palette.None },
            Hot = { Background = Palette.Well }
        };

        /// <summary>A part of a sentence that is paused: there to be read and pressed, and quieter than one that runs.</summary>
        public static readonly SlotStyle PausedSlot = new SlotStyle(Slot)
        {
            Base = { Color = Palette.Dim, Background = Palette.Chrome, Underline = Palette.Line }
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

        /// <summary>A row that has stopped where it is waiting to be let on.</summary>
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
                Thumb = Palette.Ink,
                WordPadding = new Thickness(8f, 4f),
                Padding = new Thickness(4f)
            },
            Hot = { Edge = Palette.ControlEdgeHot },
            Disabled = { Color = Palette.Dim, Thumb = Palette.Dim, Background = Palette.Well, OffTrack = Palette.Well }
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

        /// <summary>One page of several to look at, as the game draws the tabs across its terminal.</summary>
        public static readonly ButtonStyle Tab = new ButtonStyle
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
            Hot = { Background = Palette.PanelHot, Color = Palette.Ink }
        };

        /// <summary>The page that is showing, the lightest plate as the game has it.</summary>
        public static readonly ButtonStyle TabCurrent = new ButtonStyle(Tab)
        {
            Base = { Background = Palette.Line },
            Hot = { Background = Palette.Line }
        };

        /// <summary>The button that finishes a screen or answers a dialog as Enter would, greyed while there is nothing to finish.</summary>
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

        /// <summary>The question mark that opens the codex, an icon drawn from the lettering, which the symbol font has no mark for.</summary>
        public static readonly GlyphStyle Help = new GlyphStyle(Icon)
        {
            Base = { Font = MyFontEnum.White, FontSize = 28f }
        };

        /// <summary>A term in the codex's margin, which moves the codex there when pressed.</summary>
        public static readonly ButtonStyle CodexEntry = new ButtonStyle(Plain)
        {
            Base = { FontSize = 20f, Padding = new Thickness(12f, 6f) }
        };

        /// <summary>The term in the codex's margin that is being read.</summary>
        public static readonly ButtonStyle CodexEntryShown = new ButtonStyle(CodexEntry)
        {
            Base = { Color = Palette.Ink, Background = Palette.Panel },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>The term in the codex's margin that heads a section, the terms of the section listed under it.</summary>
        public static readonly ButtonStyle CodexSection = new ButtonStyle(CodexEntry)
        {
            Base = { Font = Lettering.Bold, Padding = new Thickness(12f, 16f, 12f, 6f) }
        };

        /// <summary>The section heading in the codex's margin whose page is being read.</summary>
        public static readonly ButtonStyle CodexSectionShown = new ButtonStyle(CodexSection)
        {
            Base = { Color = Palette.Ink, Background = Palette.Panel },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>How far in from a section's heading the codex's margin lists the terms of that section.</summary>
        public const float CodexIndent = 16f;

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

        /// <summary>The same strip on a banner with a warning.</summary>
        public static readonly ElementStyle NoticeStrip = new ElementStyle(ProblemStrip)
        {
            Hot = { Edge = Palette.Notice }
        };

        /// <summary>The same strip on a banner with a note, lit in the ink of its words.</summary>
        public static readonly ElementStyle InfoStrip = new ElementStyle(ProblemStrip)
        {
            Hot = { Edge = Palette.Ink }
        };

        /// <summary>A box a value is typed into, in a form of values.</summary>
        public static readonly TextBoxStyle Value = new TextBoxStyle(Field)
        {
            Base = { FontSize = 20f, Padding = new Thickness(12f, 8f) }
        };

        /// <summary>The same box holding something its value will not take.</summary>
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

        /// <summary>The same line saying a word its value will not take.</summary>
        public static readonly ElementStyle ListLineWrong = new ElementStyle(ListLine)
        {
            Base = { Background = Palette.WrongFill, Edge = Palette.Problem }
        };

        /// <summary>A question asked in a box hanging under what raised it.</summary>
        public static readonly WindowStyle Prompt = new WindowStyle
        {
            Base = { Background = Palette.PanelHot, Edge = Palette.Line, Padding = new Thickness(16f, 8f) }
        };

        /// <summary>A list hanging open under what opened it.</summary>
        public static readonly WindowStyle List = new WindowStyle
        {
            Base = { Background = Palette.Well, Edge = Palette.Line, Padding = new Thickness(4f), Shadow = ShortShadow }
        };

        /// <summary>A menu hanging open from what opened it, on the highlight tone.</summary>
        public static readonly WindowStyle Menu = new WindowStyle
        {
            Base = { Background = Palette.PanelHot, Edge = Palette.Line, Padding = new Thickness(8f), Shadow = ShortShadow }
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

        /// <summary>One entry of a list hanging open, lit under the pointer.</summary>
        public static readonly ElementStyle ListRow = new ElementStyle
        {
            Base = { Padding = new Thickness(12f, 8f) },
            Hot = { Background = Palette.Panel }
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
        /// <remarks>Padded on its right alone: the box at its head and what follows it hold the rest of the room themselves.</remarks>
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

        /// <summary>A mark saying something has not been chosen: a ring for one of several, a box for any of them.</summary>
        public static readonly GlyphStyle Mark = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 16f, Tint = Palette.Faint }
        };

        /// <summary>The same mark where it is pressed by itself, lit under the pointer.</summary>
        public static readonly GlyphStyle MarkPressed = new GlyphStyle(Mark)
        {
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>A mark saying something has been chosen.</summary>
        public static readonly GlyphStyle MarkOn = new GlyphStyle(Mark)
        {
            Base = { Tint = Palette.Live }
        };

        /// <summary>The same mark where it is pressed by itself.</summary>
        public static readonly GlyphStyle MarkOnPressed = new GlyphStyle(MarkOn)
        {
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>A mark beside words, muted: the kind of thing they name, or which way a folded column opens.</summary>
        public static readonly GlyphStyle Waits = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 20f, Tint = Palette.Muted }
        };

        /// <summary>The line hanging under a step that takes time, saying whether the sequence waits for it, lit under the pointer.</summary>
        public static readonly ElementStyle Underhang = new ElementStyle
        {
            Base = { Padding = new Thickness(6f, 2f) },
            Hot = { Background = Palette.PanelHot }
        };

        /// <summary>The words of an underhang while the sequence waits for its step.</summary>
        public static readonly TextStyle WaitWords = new TextStyle(Status)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>The words of an underhang while the sequence goes straight on from its step, quieter.</summary>
        public static readonly TextStyle WaitWordsOff = new TextStyle(Status)
        {
            Base = { Color = Palette.Dim }
        };

        /// <summary>The clock of an underhang while the sequence waits for its step.</summary>
        public static readonly GlyphStyle WaitOn = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 16f, Tint = Palette.Muted }
        };

        /// <summary>The clock of an underhang while the sequence goes straight on from its step, quieter.</summary>
        public static readonly GlyphStyle WaitOff = new GlyphStyle(WaitOn)
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
