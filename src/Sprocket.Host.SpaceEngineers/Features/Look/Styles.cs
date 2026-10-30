using Lumel;
using Lumel.Styling;
using VRage.Game;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Layout;
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

        // The faint wash code is drawn on in rich text, reaching a little either side of it and stopping a unit short of
        // the line above and below.
        static readonly VRageMath.Color CodeWash = new VRageMath.Color(255, 255, 255, 10);
        static readonly Thickness CodeRoom = new Thickness(4f, -1f);

        /// <summary>The hazard stripes a warning lies on, over its amber wash.</summary>
        public static readonly Pattern NoticeStripes = new Pattern(Materials.Stripes, 64, 64f, Palette.NoticeStripe);

        /// <summary>A warning's hazard stripes under the pointer, lifted with its wash.</summary>
        public static readonly Pattern NoticeStripesHot = new Pattern(Materials.Stripes, 64, 64f, Palette.NoticeStripeHot);

        /// <summary>The hazard stripes something wrong lies on, over its red wash.</summary>
        public static readonly Pattern ProblemStripes = new Pattern(Materials.Stripes, 64, 64f, Palette.ProblemStripe);

        /// <summary>Something wrong's hazard stripes under the pointer, lifted with its wash.</summary>
        public static readonly Pattern ProblemStripesHot = new Pattern(Materials.Stripes, 64, 64f, Palette.ProblemStripeHot);

        /// <summary>The shadow a floating window casts, lying well away from the page as if lit from the top left.</summary>
        public static readonly Shadow Shadow = new Shadow(Shade, 24f, new VRageMath.Vector2(6f, 10f), Palette.Shadow);

        /// <summary>The shorter shadow a window hanging from a control casts, lying close to the page.</summary>
        public static readonly Shadow ShortShadow = new Shadow(Shade, 8f, new VRageMath.Vector2(2f, 4f), Palette.Shadow);

        /// <summary>A name, or a sentence that is the point of where it stands.</summary>
        public static readonly IStyle<Label> Name = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Body, Color = Palette.Ink }
        };

        /// <summary>The name of something listed inside a group, a size under the group's own.</summary>
        public static readonly IStyle<Label> InnerName = new LabelStyle(Name)
        {
            Base = { FontSize = TextSizes.Small }
        };

        /// <summary>What a list says while it has nothing in it.</summary>
        public static readonly IStyle<Label> Empty = new LabelStyle(Name)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>What a group inside a list says while it has nothing in it, the size of the names listed in it.</summary>
        public static readonly IStyle<Label> InnerEmpty = new LabelStyle(InnerName)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>The words of a codex tip: quiet words, strong ones bold and emphasized ones italic, under a title in the bright ink.</summary>
        public static readonly IStyle<RichText> Tip = new RichTextStyle
        {
            Base =
            {
                CodeBackground = CodeWash,
                CodePadding = CodeRoom,
                Font = MyFontEnum.White,
                StrongFont = TextSizes.Bold,
                EmphasisFont = TextSizes.Italic,
                StrongEmphasisFont = TextSizes.BoldItalic,
                CodeFont = TextSizes.Mono,
                FontSize = TextSizes.Small,
                Heading1Size = TextSizes.Body,
                Heading2Size = TextSizes.Small,
                Heading3Size = TextSizes.Small,
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

        /// <summary>What the author of a blueprint wrote about it, read by whoever fills it in, its callouts <c>warning</c> for what can go wrong and <c>player</c> for table-talk, in italics.</summary>
        public static readonly IStyle<RichText> Description = new RichTextStyle
        {
            Base =
            {
                CodeBackground = CodeWash,
                CodePadding = CodeRoom,
                Font = MyFontEnum.White,
                StrongFont = TextSizes.Bold,
                EmphasisFont = TextSizes.Italic,
                StrongEmphasisFont = TextSizes.BoldItalic,
                CodeFont = TextSizes.Mono,
                FontSize = TextSizes.Small,
                Heading1Size = TextSizes.Title,
                Heading2Size = TextSizes.Body,
                Heading3Size = TextSizes.Body,
                Color = Palette.Muted,
                StrongColor = Palette.Muted,
                EmphasisColor = Palette.Muted,
                LinkColor = Palette.Link,
                HeadingColor = Palette.Ink,
                RuleColor = Palette.Line,
                BlockSpacing = Gaps.Parts,
                UnderlinedHeadingLevel = 2,
                Indent = Sizes.Indent,
                Callouts =
                {
                    ["warning"] = { BarColor = Palette.Notice, Background = Palette.NoticeWash, Color = Palette.Ink },
                    ["player"] = { BarColor = Palette.Accent, Background = Palette.Panel, IsEmphasized = true }
                }
            }
        };

        /// <summary>A page of words read on its own, as the guide to rich text and the paused screen are: a description given room round it.</summary>
        public static readonly IStyle<RichText> Reading = new RichTextStyle(Description)
        {
            Base = { Padding = new Thickness(20f, 24f) }
        };

        /// <summary>What the author of a blueprint wrote about one of its slots, under the slot's name.</summary>
        public static readonly IStyle<RichText> SlotDescription = new RichTextStyle(Description)
        {
            Base =
            {
                FontSize = TextSizes.Tiny,
                Heading1Size = TextSizes.Small,
                Heading2Size = TextSizes.Tiny,
                Heading3Size = TextSizes.Tiny,
                Color = Palette.Faint,
                StrongColor = Palette.Faint,
                EmphasisColor = Palette.Faint,
                BlockSpacing = Gaps.Within
            }
        };

        /// <summary>A question hanging under the control that raised it, small and quiet above its answers.</summary>
        public static readonly IStyle<RichText> MenuQuestion = new RichTextStyle(SlotDescription);

        /// <summary>The guide to rich text's window: the page's own color, edged, with nothing veiled behind it, casting a floating window's shadow.</summary>
        public static readonly IStyle<Window> GuideWindow = new WindowStyle
        {
            Base = { Background = Palette.Page, Border = Palette.Line, Shadow = Shadow }
        };

        /// <summary>A dialog: a panel in the middle of the screen, with the screen veiled behind it.</summary>
        public static readonly IStyle<Dialog> Dialog = new DialogStyle
        {
            Base = { Background = Palette.Chrome, Border = Palette.Line, Padding = new Thickness(24f), Backdrop = Palette.Veil }
        };

        /// <summary>What a dialog asks.</summary>
        public static readonly IStyle<RichText> Question = new RichTextStyle
        {
            Base =
            {
                CodeBackground = CodeWash,
                CodePadding = CodeRoom,
                Font = MyFontEnum.White,
                StrongFont = TextSizes.Bold,
                EmphasisFont = TextSizes.Italic,
                StrongEmphasisFont = TextSizes.BoldItalic,
                CodeFont = TextSizes.Mono,
                FontSize = TextSizes.Body,
                Color = Palette.Ink,
                StrongColor = Palette.Ink,
                EmphasisColor = Palette.Ink,
                LinkColor = Palette.Link,
                BlockSpacing = 8f,
                Indent = 24f
            }
        };

        /// <summary>A screen's heading, in the largest words on it.</summary>
        public static readonly IStyle<Label> Title = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Title, Color = Palette.Ink }
        };

        /// <summary>Words in a heading around a name, quieter than the name.</summary>
        public static readonly IStyle<Label> TitleLead = new LabelStyle(Title)
        {
            Base = { Color = Palette.Muted }
        };

        /// <summary>A name in a heading, spaced as its box is so the box takes its place without moving it; in italics where it is set apart.</summary>
        public static readonly IStyle<Label> TitleName = new LabelStyle(Title)
        {
            Base = { Padding = new Thickness(0f, 4f, 8f, 4f) },
            [LookStates.Emphasized] = { Font = TextSizes.Italic }
        };

        /// <summary>The box a heading's name is typed in, a well on the chrome; washed and edged in red while the name in it cannot be given.</summary>
        public static readonly IStyle<TextBox> TitleBox = new TextBoxStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Title,
                Color = Palette.Ink,
                CaretColor = Palette.Ink,
                SelectionColor = Palette.Line,
                Background = Palette.PanelHot,
                Padding = new Thickness(0f, 4f, 8f, 4f)
            },
            [LookStates.Wrong] = { Background = Palette.WrongFill, Border = Palette.Problem }
        };

        /// <summary>A box a value is typed into, in a list of answers.</summary>
        public static readonly IStyle<TextBox> Field = new TextBoxStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Body,
                Color = Palette.Ink,
                CaretColor = Palette.Ink,
                SelectionColor = Palette.Line,
                HintColor = Palette.Faint,
                Background = Palette.Page,
                Border = Palette.Line,
                Padding = new Thickness(12f, 8f)
            },
            Focused = { Border = Palette.ControlEdgeHot }
        };

        /// <summary>A name in a row that can be renamed in place, spaced as its box is so the box takes its place without moving it; in italics where it is set apart.</summary>
        public static readonly IStyle<Label> PlateName = new LabelStyle(Name)
        {
            Base = { Padding = new Thickness(8f, 4f) },
            [LookStates.Emphasized] = { Font = TextSizes.Italic }
        };

        /// <summary>The box a name in a row is typed in, a well on the page in the name's size.</summary>
        public static readonly IStyle<TextBox> PlateBox = new TextBoxStyle(TitleBox)
        {
            Base = { FontSize = TextSizes.Body, Padding = new Thickness(8f, 4f) }
        };


        /// <summary>A heading's name with its pencil, lit under the pointer where pressing it renames.</summary>
        public static readonly IStyle<Stack> Plate = new StackStyle
        {
            PointerOver = { Background = Palette.Panel }
        };

        /// <summary>A name that opens a menu rather than being renamed at a press, drawn as the game's button around its words.</summary>
        public static readonly IStyle<Stack> PlateButton = new StackStyle
        {
            Base = { Background = Palette.Panel, Border = Palette.ControlEdge, Padding = new Thickness(8f, 4f) },
            PointerOver = { Background = Palette.PanelHot, Border = Palette.ControlEdgeHot }
        };

        /// <summary>The pencil beside a name that can be renamed, whose color the view sets while the name is typed.</summary>
        public static readonly IStyle<Glyph> Pen = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Small, Tint = Palette.Faint }
        };

        /// <summary>The arrow between two parts of a heading.</summary>
        public static readonly IStyle<Glyph> Join = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Small, Tint = Palette.Faint }
        };

        /// <summary>The name of one of a page's own sections, in capitals, where the name is a term in the codex; the term in the heading's own color while tips are off.</summary>
        public static readonly IStyle<RichText> H1Term = new RichTextStyle
        {
            Base =
            {
                CodeBackground = CodeWash,
                CodePadding = CodeRoom,
                Font = MyFontEnum.White,
                FontSize = TextSizes.Small,
                Color = Palette.Faint,
                StrongColor = Palette.Ink,
                EmphasisColor = Palette.Ink,
                LinkColor = Palette.Link
            },
            [LookStates.Plain] = { LinkColor = Palette.Faint }
        };

        /// <summary>The name of a section inside one of a page's sections or inside a tab, a size down, where the name is a term in the codex.</summary>
        public static readonly IStyle<RichText> H2Term = new RichTextStyle(H1Term)
        {
            Base = { FontSize = TextSizes.Tiny }
        };

        /// <summary>The name of one of a page's own sections, in capitals.</summary>
        public static readonly IStyle<Label> H1 = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Faint }
        };

        /// <summary>The name of a section inside one of a page's sections or inside a tab, a size down.</summary>
        public static readonly IStyle<Label> H2 = new LabelStyle(H1)
        {
            Base = { FontSize = TextSizes.Tiny }
        };

        /// <summary>A category's name on the installation panel: a section's name a size down, with room after it before the pencil that renames it.</summary>
        public static readonly IStyle<Label> CategoryName = new LabelStyle(H2)
        {
            Base = { Padding = new Thickness(0f, 0f, 8f, 0f) }
        };

        /// <summary>The box a category's name is typed in: the name's own face and size, its words where the name's stood, on a well.</summary>
        public static readonly IStyle<TextBox> CategoryBox = new TextBoxStyle(PlateBox)
        {
            Base = { FontSize = TextSizes.Tiny, Padding = new Thickness(0f, 4f, 8f, 4f) }
        };

        /// <summary>The quiet line under a name: what it is doing, and when it last did anything.</summary>
        public static readonly IStyle<Label> Needs = new LabelStyle(H1)
        {
            Base = { Color = Palette.Notice }
        };

        /// <summary>A small note under or beside something, in the faint tone.</summary>
        public static readonly IStyle<Label> Detail = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Tiny, Color = Palette.Faint }
        };

        /// <summary>Table-talk said in a line of its own: a word about the game itself rather than the world, in italics.</summary>
        public static readonly IStyle<Label> TableTalk = new LabelStyle
        {
            Base = { Font = TextSizes.Italic, FontSize = TextSizes.Tiny, Color = Palette.Faint }
        };

        /// <summary>What a thing is doing, said in a line of its own tools rather than under its name; on its level's wash and stripes for a warning or a problem.</summary>
        public static readonly IStyle<Label> Status = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Muted },
            [LookStates.Warned] = { Color = Palette.Notice, Background = Palette.NoticeWash, Pattern = NoticeStripes, Padding = new Thickness(16f, 8f) },
            [LookStates.Broken] = { Color = Palette.Problem, Background = Palette.ProblemWash, Pattern = ProblemStripes, Padding = new Thickness(16f, 8f) }
        };

        /// <summary>The quieter words beside a status: when it last did anything, a step's number, why nothing can be added.</summary>
        public static readonly IStyle<Label> Aside = new LabelStyle(Status)
        {
            Base = { Color = Palette.Faint }
        };

        /// <summary>What is wrong with a thing, on its own line under it.</summary>
        public static readonly IStyle<Label> Problem = new LabelStyle(Detail)
        {
            Base = { Color = Palette.Problem }
        };

        /// <summary>What an answer does that somebody should know before giving it, on its own line under it.</summary>
        public static readonly IStyle<Label> Caution = new LabelStyle(Detail)
        {
            Base = { Color = Palette.Notice }
        };

        /// <summary>What is wrong, on a line of its own: in red, on its red ground.</summary>
        public static readonly IStyle<Label> ProblemLine = new LabelStyle(Problem)
        {
            Base = { Background = Palette.ProblemWash, Pattern = ProblemStripes, Padding = new Thickness(8f, 4f) }
        };

        /// <summary>A warning, on a line of its own: in amber, on its amber ground.</summary>
        public static readonly IStyle<Label> WarningLine = new LabelStyle(Caution)
        {
            Base = { Background = Palette.NoticeWash, Pattern = NoticeStripes, Padding = new Thickness(8f, 4f) }
        };

        /// <summary>One answer in a list of them, pressed to give it; lit where it is the answer given.</summary>
        public static readonly IStyle<Stack> Answer = new StackStyle
        {
            Base = { Background = Palette.Panel, Padding = new Thickness(20f, 16f) },
            PointerOver = { Background = Palette.PanelHot },
            [LookStates.Chosen] = { Background = Palette.PanelHot },
            [LookStates.Chosen | State.PointerOver] = { Background = Palette.Line }
        };

        /// <summary>The words of a sentence around the parts somebody chose; dim on a paused step.</summary>
        public static readonly IStyle<Label> Phrase = new LabelStyle(Name)
        {
            Base = { Color = Palette.Muted },
            [LookStates.Paused] = { Color = Palette.Dim }
        };

        /// <summary>The words a sentence opens with, where they may be a term in the codex; the term in the words' own color while tips are off, and all of it dim on a paused step.</summary>
        public static readonly IStyle<RichText> PhraseTerm = new RichTextStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Body, Color = Palette.Muted, LinkColor = Palette.Link, CodeBackground = CodeWash, CodePadding = CodeRoom },
            [LookStates.Plain] = { LinkColor = Palette.Muted },
            [LookStates.Paused] = { Color = Palette.Dim, LinkColor = Palette.Dim }
        };

        /// <summary>A part of a sentence: the game's button in a name's size, its words in the warning color while nothing fills it, a quiet box where it cannot be pressed, and quieter on a paused step.</summary>
        public static readonly IStyle<SlotChip> Slot = new SlotStyle
        {
            Base =
            {
                Font = MyFontEnum.White, FontSize = TextSizes.Body, Color = Palette.Ink, Background = Palette.Panel, Border = Palette.ControlEdge,
                Padding = new Thickness(16f, 8f)
            },
            Empty = { Color = Palette.Notice },
            Still = { Background = Palette.Well, Border = Palette.None },
            PointerOver = { Background = Palette.PanelHot, Border = Palette.ControlEdgeHot },
            [LookStates.Paused] = { Color = Palette.Dim, Background = Palette.Chrome }
        };

        /// <summary>A word on a green wash, saying something is true at this instant.</summary>
        public static readonly IStyle<Label> Live = new LabelStyle
        {
            Base =
            {
                Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Live, Background = Palette.LiveWash,
                Padding = new Thickness(16f, 8f)
            }
        };

        /// <summary>The rest of what a banner says, once it is opened.</summary>
        public static readonly IStyle<Label> Opened = new LabelStyle(Name)
        {
            Base = { Color = Palette.Faint, Padding = new Thickness(16f, 0f, 16f, 8f) }
        };

        /// <summary>What went wrong, in the words of whatever found it, for whoever mends it.</summary>
        public static readonly IStyle<Label> Diagnostic = new LabelStyle(Detail)
        {
            Base = { Color = Palette.Dim, Padding = new Thickness(16f, 0f, 16f, 8f) }
        };

        /// <summary>The strip between the library's panes, a step darker than the buttons in it.</summary>
        public static readonly IStyle<Stack> MoveStrip = new StackStyle
        {
            Base = { Background = Palette.Well, Padding = new Thickness(Gaps.Within), Spacing = Gaps.Parts }
        };

        /// <summary>A button in the strip between the library's panes, lit under the pointer; quiet where it cannot be pressed.</summary>
        public static readonly IStyle<Element> Move = new ElementStyle
        {
            Base = { Background = Palette.Panel },
            PointerOver = { Background = Palette.PanelHot },
            Disabled = { Background = Palette.Chrome }
        };

        /// <summary>A mark on a button in the strip between the library's panes; dim while the button cannot be pressed.</summary>
        public static readonly IStyle<Glyph> MoveMark = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Body, Tint = Palette.Ink },
            [LookStates.Off] = { Tint = Palette.Dim }
        };

        /// <summary>A row of a list that opens what it names.</summary>
        public static readonly IStyle<Stack> Row = new StackStyle
        {
            Base = { Background = Palette.Panel, Padding = new Thickness(16f, 8f) },
            PointerOver = { Background = Palette.PanelHot }
        };

        /// <summary>A row whose sequence is running.</summary>
        public static readonly IStyle<Stack> Running = new StackStyle(Row)
        {
            Base = { Background = Palette.LiveWash }
        };

        /// <summary>A row with something wrong in it.</summary>
        public static readonly IStyle<Stack> Broken = new StackStyle(Row)
        {
            Base = { Background = Palette.ProblemWash, Pattern = ProblemStripes },
            PointerOver = { Background = Palette.ProblemWashHot, Pattern = ProblemStripesHot }
        };

        /// <summary>The row chosen from a list, whose details stand beside it.</summary>
        public static readonly IStyle<Stack> Chosen = new StackStyle(Row)
        {
            Base = { Background = Palette.PanelHot, Border = Palette.Accent }
        };

        /// <summary>The chosen row, with something wrong in it.</summary>
        public static readonly IStyle<Stack> BrokenChosen = new StackStyle(Broken)
        {
            Base = { Border = Palette.Accent }
        };

        /// <summary>A row with a warning, or stopped where it is waiting to be let on, or switched off: washed amber.</summary>
        public static readonly IStyle<Stack> Parked = new StackStyle(Row)
        {
            Base = { Background = Palette.NoticeWash, Pattern = NoticeStripes },
            PointerOver = { Background = Palette.NoticeWashHot, Pattern = NoticeStripesHot }
        };

        /// <summary>The chosen row, with a warning, stopped or switched off.</summary>
        public static readonly IStyle<Stack> ParkedChosen = new StackStyle(Parked)
        {
            Base = { Border = Palette.Accent }
        };

        /// <summary>A row with nothing to open.</summary>
        public static readonly IStyle<Stack> Still = new StackStyle(Row)
        {
            Base = { Background = Palette.None },
            PointerOver = { Background = Palette.None }
        };

        /// <summary>A state light, whose color the view sets from the state.</summary>
        public static readonly IStyle<Glyph> Light = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 8f, Tint = Palette.Dim }
        };

        /// <summary>A row's light drawn as a symbol of what the row is, large enough to be read rather than only seen.</summary>
        public static readonly IStyle<Glyph> Marked = new GlyphStyle(Light)
        {
            Base = { FontSize = TextSizes.Small }
        };

        /// <summary>A mark beside a name, whose color the view sets from what it marks.</summary>
        public static readonly IStyle<Glyph> Flag = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Small }
        };

        /// <summary>The mark after a tab's words saying something on its page wants looking at, whose color the view sets from how much it matters.</summary>
        public static readonly IStyle<Glyph> TabMark = new GlyphStyle(Flag)
        {
            Base = { FontSize = TextSizes.Tiny, Padding = new Thickness(8f, 0f, 0f, 0f) }
        };

        /// <summary>The chevron that opens a banner, whose color the view sets from the banner's.</summary>
        public static readonly IStyle<Glyph> Chevron = new GlyphStyle(Flag);

        /// <summary>A screen's scroll, whose bar is as wide as the one the game reserves for its own.</summary>
        public static readonly IStyle<ScrollView> ScrollView = new ScrollViewStyle
        {
            Base = { ScrollBarWidth = Sizes.Bar, Thumb = Palette.Thumb, MinThumbLength = 32f },
            Dragging = { Thumb = Palette.ThumbHeld }
        };

        /// <summary>Text shown as it will read in place of the box it is written in, framed as that box is so the editor keeps its edge.</summary>
        public static readonly IStyle<ScrollView> Preview = new ScrollViewStyle(ScrollView)
        {
            Base = { Background = Palette.Page, Border = Palette.Line, Padding = new Thickness(8f) }
        };

        /// <summary>An on/off switch: a square thumb and the word, sunk into a dark track while off, and edged under the pointer.</summary>
        public static readonly IStyle<Switch> Switch = new SwitchStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Tiny,
                Color = Palette.Ink,
                Background = Palette.Line,
                Track = Palette.Well,
                Thumb = Palette.Ink,
                WordPadding = new Thickness(8f, 4f),
                Padding = new Thickness(4f)
            },
            On = { Track = Palette.None },
            PointerOver = { Border = Palette.ControlEdgeHot },
            Disabled = { Color = Palette.Dim, Thumb = Palette.Dim, Background = Palette.Well, Track = Palette.Well }
        };

        /// <summary>A logic system's or an installation's switch, its thumb amber while off, as the row or heading bar holding it is washed.</summary>
        public static readonly IStyle<Switch> SystemSwitch = new SwitchStyle(Switch)
        {
            Base = { Thumb = Palette.Notice },
            On = { Thumb = Palette.Ink }
        };

        /// <summary>A color to choose in the color picker: a patch edged in the chrome's color, edged in ink under the pointer.</summary>
        public static readonly IStyle<Swatch> ColorChoice = new SwatchStyle
        {
            Base = { Border = Palette.Chrome },
            PointerOver = { Border = Palette.Ink }
        };

        /// <summary>A color shown on its own, in a sentence where its words would be or beside the box it is typed in: a patch with a hairline round it.</summary>
        public static readonly IStyle<Swatch> ColorSample = new SwatchStyle
        {
            Base = { Border = Palette.Line }
        };

        /// <summary>A box a whole script or text is read and typed in: the fixed-width face, its lines numbered in a dim color, and the game's scroll bar.</summary>
        public static readonly IStyle<TextArea> ListingBox = new TextAreaStyle
        {
            Base =
            {
                Font = TextSizes.Mono, FontSize = TextSizes.Small, Color = Palette.Ink, CaretColor = Palette.Ink, SelectionColor = Palette.Line,
                LineNumberColor = Palette.Dim, Padding = new Thickness(0f, 0f, Gaps.Parts, 0f), ScrollBarWidth = Sizes.Bar, Thumb = Palette.Thumb,
                MinThumbLength = 32f
            },
            Dragging = { Thumb = Palette.ThumbHeld }
        };

        /// <summary>A box a description is written in: the game's face on the page's color, edged, its hint faint, and the game's scroll bar.</summary>
        public static readonly IStyle<TextArea> WritingBox = new TextAreaStyle
        {
            Base =
            {
                Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Ink, CaretColor = Palette.Ink, SelectionColor = Palette.Line,
                HintColor = Palette.Faint, Background = Palette.Page, Border = Palette.Line, Padding = new Thickness(8f),
                ScrollBarWidth = Sizes.Bar, Thumb = Palette.Thumb, MinThumbLength = 24f
            },
            Dragging = { Thumb = Palette.ThumbHeld }
        };

        /// <summary>The game's button: a panel with a hairline round it, lifted under the pointer, lit while pressed, and its words dimmed while it cannot be used.</summary>
        public static readonly IStyle<Button> Button = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Tiny,
                Color = Palette.Ink,
                Background = Palette.Panel,
                Border = Palette.ControlEdge,
                Padding = new Thickness(16f, 8f)
            },
            PointerOver = { Background = Palette.PanelHot, Border = Palette.ControlEdgeHot },
            Pressed = { Background = Palette.Line },
            Disabled = { Color = Palette.Dim }
        };

        /// <summary>The game's button that stays lit as if pressed while what it turns on is on.</summary>
        public static readonly IStyle<Button> Toggle = new ButtonStyle(Button)
        {
            [LookStates.Chosen] = { Background = Palette.Line }
        };

        /// <summary>A slot's name on the installation panel where pressing it opens the slot to be named and described: the game's button, in a name's size; a quiet box where it cannot be pressed.</summary>
        /// <remarks>Padded as a block chip beside it is, so the name stands level with the first block.</remarks>
        public static readonly IStyle<Button> SlotName = new ButtonStyle(Button)
        {
            Base = { FontSize = TextSizes.Body },
            [LookStates.Still] = { Color = Palette.Ink, Background = Palette.Well, Border = Palette.None, Padding = new Thickness(16f, 8f) }
        };

        /// <summary>A slot's name on an installation's own page, which is never pressed: the words alone, level with the blocks beside them.</summary>
        public static readonly IStyle<Button> SlotNamePlain = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Body, Color = Palette.Ink, Padding = new Thickness(0f, 8f, 0f, 0f) }
        };

        /// <summary>The header of one page of several, as the game draws the tabs across its terminal, the page showing on the lightest plate; on its level's wash and stripes where something on its page is worth a warning or is wrong.</summary>
        public static readonly IStyle<Tab> Tab = new TabStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Small,
                Color = Palette.Muted,
                Background = Palette.Panel,
                Border = Palette.Line,
                Padding = new Thickness(8f, 8f)
            },
            PointerOver = { Background = Palette.PanelHot, Color = Palette.Ink },
            Selected = { Background = Palette.Line, Color = Palette.Ink },
            [LookStates.Warned] = { Background = Palette.NoticeWash, Pattern = NoticeStripes },
            [LookStates.Warned | State.PointerOver] = { Background = Palette.NoticeWashHot, Pattern = NoticeStripesHot },
            [LookStates.Warned | Lumel.Controls.Tab.Selected] = { Background = Palette.NoticeWashHot, Pattern = NoticeStripesHot, Border = Palette.Accent },
            [LookStates.Broken] = { Background = Palette.ProblemWash, Pattern = ProblemStripes },
            [LookStates.Broken | State.PointerOver] = { Background = Palette.ProblemWashHot, Pattern = ProblemStripesHot },
            [LookStates.Broken | Lumel.Controls.Tab.Selected] = { Background = Palette.ProblemWashHot, Pattern = ProblemStripesHot, Border = Palette.Accent }
        };

        /// <summary>The button that finishes a screen or answers a dialog as Enter would, grayed while there is nothing to finish.</summary>
        public static readonly IStyle<Button> Primary = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Tiny,
                Color = Palette.Chrome,
                Background = Palette.Accent,
                Padding = new Thickness(16f, 8f)
            },
            PointerOver = { Background = Palette.Ink },
            Disabled = { Color = Palette.Dim, Background = Palette.Panel }
        };

        /// <summary>A way to start something, offered in place: a plus and quiet words, and a panel under the pointer.</summary>
        public static readonly IStyle<Button> Start = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Body, Color = Palette.Muted, Padding = new Thickness(16f, 4f) },
            PointerOver = { Background = Palette.Panel }
        };

        /// <summary>Words that do something when pressed, with no box round them, and a panel under the pointer; in the ink for the screen showing.</summary>
        public static readonly IStyle<Button> Plain = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Tiny, Color = Palette.Muted, Padding = new Thickness(16f, 8f) },
            PointerOver = { Background = Palette.Panel },
            [LookStates.Chosen] = { Color = Palette.Ink }
        };

        /// <summary>A symbol that does something, lit under the pointer and dimmed while there is nothing for it to do.</summary>
        public static readonly IStyle<Glyph> Icon = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Small, Tint = Palette.Muted },
            PointerOver = { Background = Palette.PanelHot, Tint = Palette.Ink },
            Disabled = { Tint = Palette.Dim }
        };

        /// <summary>A codex tip: the chrome's color, edged, with room inside the edge, casting a short shadow, its words quiet and its steps symbols.</summary>
        public static readonly IStyle<CodexTip> CodexTip = new CodexTipStyle
        {
            Base =
            {
                Background = Palette.Chrome,
                Border = Palette.Line,
                Padding = new Thickness(12f, 8f),
                Shadow = ShortShadow,
                MaxHeight = Sizes.TipHigh,
                Spacing = Gaps.Parts,
                BodyStyle = Tip,
                StepStyle = Icon,
                BackSymbol = Symbols.Left,
                ForwardSymbol = Symbols.Right
            }
        };

        /// <summary>The question mark in the heading bar while tips are shown, in the ink terms are drawn in; dim while they are hidden.</summary>
        public static readonly IStyle<Glyph> Tips = new GlyphStyle(Icon)
        {
            Base = { Font = MyFontEnum.White, FontSize = 28f, Tint = Palette.Link },
            [LookStates.Off] = { Tint = Palette.Dim }
        };

        /// <summary>The mark a drop-down line ends with, smaller than a symbol pressed on its own.</summary>
        public static readonly IStyle<Glyph> ListMark = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = 12f, Tint = Palette.Muted }
        };

        /// <summary>A symbol that pauses something, smaller than the icons round it while nothing is paused; full size on an amber wash while the step is paused.</summary>
        public static readonly IStyle<Glyph> Holds = new GlyphStyle(Icon)
        {
            Base = { FontSize = 12f },
            [LookStates.Paused] = { FontSize = TextSizes.Small, Background = Palette.NoticeWash, Tint = Palette.Notice },
            [LookStates.Paused | State.PointerOver] = { Background = Palette.NoticeWash, Tint = Palette.Ink }
        };

        /// <summary>A symbol that throws something away, lit in the color of something wrong under the pointer.</summary>
        public static readonly IStyle<Glyph> Bin = new GlyphStyle(Icon)
        {
            PointerOver = { Background = Palette.ProblemWash, Tint = Palette.Problem }
        };

        /// <summary>The strip a banner saying something is wrong is opened by, lit by an edge in its color.</summary>
        public static readonly IStyle<Stack> ProblemStrip = new StackStyle
        {
            Base = { Padding = new Thickness(16f, 8f) },
            PointerOver = { Border = Palette.Problem }
        };

        /// <summary>The strip a banner with a warning is opened by, lit by an edge in its color.</summary>
        public static readonly IStyle<Stack> NoticeStrip = new StackStyle(ProblemStrip)
        {
            PointerOver = { Border = Palette.Notice }
        };

        /// <summary>The strip a banner with a note is opened by, lit in the ink of its words.</summary>
        public static readonly IStyle<Stack> InfoStrip = new StackStyle(ProblemStrip)
        {
            PointerOver = { Border = Palette.Ink }
        };

        /// <summary>A box a value is typed into, in a form of values; washed and edged in red while what was typed cannot be used.</summary>
        public static readonly IStyle<TextBox> Value = new TextBoxStyle(Field)
        {
            Base = { FontSize = TextSizes.Small, Padding = new Thickness(12f, 8f) },
            [LookStates.Wrong] = { Background = Palette.WrongFill, Border = Palette.Problem }
        };

        /// <summary>A box a number is typed into, in a form of values, the number standing against its right edge; washed and edged in red while what was typed cannot be used.</summary>
        public static readonly IStyle<NumberBox> Number = new NumberBoxStyle(Value)
        {
            Base = { TextAlignment = TextAlignment.Right }
        };

        /// <summary>A dial an angle is turned on: faint marks, the quarters a step brighter, and the needle in the ink, lit while it is under the pointer or turned.</summary>
        public static readonly IStyle<Dial> Dial = new DialStyle
        {
            Base = { MarkColor = Palette.Faint, QuarterColor = Palette.Muted, NeedleColor = Palette.Ink },
            PointerOver = { NeedleColor = Palette.Live },
            Dragging = { NeedleColor = Palette.Live }
        };

        /// <summary>What a value in a form is, beside where it is given.</summary>
        /// <remarks>Muted rather than faint, for 6.6:1 against the panel, where faint gives 4.5:1, the least WCAG allows.</remarks>
        public static readonly IStyle<Label> ValueLabel = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Muted }
        };

        /// <summary>Why a form cannot be finished as it stands, beside the way to finish it.</summary>
        public static readonly IStyle<Label> Refusal = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Problem }
        };

        /// <summary>One of a few words a value may be, pressed to give it; on a lighter plate, in the ink, where it is the one chosen.</summary>
        public static readonly IStyle<Button> Word = new ButtonStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Small,
                Color = Palette.Muted,
                Background = Palette.Well,
                Border = Palette.Line,
                Padding = new Thickness(12f, 4f)
            },
            PointerOver = { Background = Palette.Panel, Border = Palette.Muted },
            [LookStates.Chosen] = { Color = Palette.Ink, Background = Palette.Line }
        };

        /// <summary>The line a list hangs from, saying what is chosen in it, edged while it holds the keyboard; washed and edged in red while what it holds cannot be used.</summary>
        public static readonly IStyle<Stack> ListLine = new StackStyle
        {
            Base = { Background = Palette.Well, Border = Palette.Line, Padding = new Thickness(12f, 8f) },
            PointerOver = { Background = Palette.Panel, Border = Palette.Muted },
            [LookStates.Wrong] = { Background = Palette.WrongFill, Border = Palette.Problem }
        };

        /// <summary>The line a list hangs from where it stands in a column's heading, as tall as the heading's words so nothing under it moves.</summary>
        public static readonly IStyle<Stack> ListLineInHeading = new StackStyle(ListLine)
        {
            Base = { Padding = new Thickness(12f, 0f) }
        };

        /// <summary>Controls hanging from what opened them, on the ground a form's controls stand on, so they look as they do in a form.</summary>
        public static readonly IStyle<Popup> Panel = new PopupStyle
        {
            Base = { Background = Palette.Panel, Border = Palette.Line, Padding = new Thickness(12f), Shadow = ShortShadow }
        };

        /// <summary>A menu or a list hanging open from what opened it, on the tone every hanging thing shares: the one a form's controls stand on.</summary>
        public static readonly IStyle<Popup> Menu = new PopupStyle
        {
            Base = { Background = Palette.Panel, Border = Palette.Line, Padding = new Thickness(8f), Shadow = ShortShadow }
        };

        /// <summary>One entry of a menu, lit a step past the menu's own tone under the pointer; set in under its family where nested, and lit in the accent where it is the one chosen.</summary>
        public static readonly IStyle<Stack> MenuRow = new StackStyle
        {
            Base = { Padding = new Thickness(12f, 8f) },
            PointerOver = { Background = Palette.Line },
            [LookStates.Nested] = { Padding = new Thickness(32f, 8f, 12f, 8f) },
            [LookStates.Chosen] = { Background = Palette.Accent },
            [LookStates.Chosen | State.PointerOver] = { Background = Palette.Ink }
        };

        /// <summary>The words of an entry in a list hanging open; in the chrome's color on the row chosen.</summary>
        public static readonly IStyle<Label> ListWord = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Ink },
            [LookStates.Chosen] = { Color = Palette.Chrome }
        };

        /// <summary>One column of a pick: a panel with a hairline round it.</summary>
        public static readonly IStyle<Stack> Column = new StackStyle
        {
            Base = { Background = Palette.Panel, Border = Palette.Line, Padding = new Thickness(16f) }
        };

        /// <summary>A column standing for a page of a pick, pressed anywhere down its length to turn to it.</summary>
        public static readonly IStyle<Stack> Fold = new StackStyle
        {
            Base = { Background = Palette.Panel, Border = Palette.Line, Padding = new Thickness(0f, 16f) },
            PointerOver = { Background = Palette.PanelHot }
        };



        /// <summary>A quiet line under what a column asks, or about what it lists.</summary>
        /// <remarks>Muted rather than faint, for 6.6:1 against the panel, where faint gives 4.5:1, the least WCAG allows.</remarks>
        public static readonly IStyle<Label> ColumnNote = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Tiny, Color = Palette.Muted }
        };

        /// <summary>The quiet line under a block's name that says the grid it is on, the grid's name a step brighter than the rest.</summary>
        public static readonly IStyle<RichText> ColumnNoteOnGrid = new RichTextStyle
        {
            Base = { Font = MyFontEnum.White, StrongFont = MyFontEnum.White, FontSize = TextSizes.Tiny, Color = Palette.Muted, StrongColor = Palette.Ink }
        };

        /// <summary>A box a list is narrowed by typing into.</summary>
        public static readonly IStyle<TextBox> Search = new TextBoxStyle(Value);

        /// <summary>A line over a list saying what it is narrowed by, on a wash of its own.</summary>
        public static readonly IStyle<Label> Note = new LabelStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = TextSizes.Small,
                Color = Palette.Muted,
                Background = Palette.SlotFill,
                Padding = new Thickness(16f, 8f)
            }
        };

        /// <summary>What a list says where it has nothing in it.</summary>
        public static readonly IStyle<Label> Nothing = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Muted, Padding = new Thickness(4f, 8f, 0f, 0f) }
        };

        /// <summary>A quiet link at the end of a column's heading to another way of looking at its list.</summary>
        public static readonly IStyle<Button> ViewLink = new ButtonStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Muted, Padding = new Thickness(8f, 4f) },
            PointerOver = { Background = Palette.PanelHot, Color = Palette.Ink }
        };

        /// <summary>A thing to choose in a column, lit under the pointer and where it is the one chosen, and never lit where it cannot be picked.</summary>
        public static readonly IStyle<Stack> Pick = new StackStyle
        {
            Base = { Padding = new Thickness(0f, 0f, 12f, 0f) },
            PointerOver = { Background = Palette.PanelHot },
            [LookStates.Chosen] = { Background = Palette.PanelHot },
            [LookStates.Chosen | State.PointerOver] = { Background = Palette.Line },
            [LookStates.Still] = { Background = Palette.None }
        };

        /// <summary>The name of a thing to choose, at the size the game writes a block list in; muted where it is listed only to be read about, faint where it heads a group.</summary>
        public static readonly IStyle<MemberName> PickName = new MemberNameStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Body, Color = Palette.Ink },
            [LookStates.Off] = { Color = Palette.Muted },
            [LookStates.Heading] = { FontSize = TextSizes.Tiny, Color = Palette.Faint }
        };

        /// <summary>A block's picture beside its name in a column, on a well of its own.</summary>
        public static readonly IStyle<Image> Picture = new ImageStyle
        {
            Base = { Background = Palette.SlotFill }
        };

        /// <summary>A toolbar slot that holds nothing, offered under a toolbar a person presses; filled where it holds a slot, washed green while it waits for its signal.</summary>
        public static readonly IStyle<Element> SlotFree = new ElementStyle
        {
            Base = { Background = Palette.Well, Border = Palette.Line },
            PointerOver = { Background = Palette.Panel, Border = Palette.Muted },
            [LookStates.Taken] = { Background = Palette.SlotFill, Border = Palette.SlotEdge },
            [LookStates.Taken | State.PointerOver] = { Background = Palette.PanelHot, Border = Palette.Ink },
            [LookStates.Answering] = { Background = Palette.LiveWash, Border = Palette.Live },
            [LookStates.Answering | State.PointerOver] = { Background = Palette.PanelHot, Border = Palette.Live }
        };

        /// <summary>The ring around the toolbar slot chosen, over whatever the slot holds.</summary>
        public static readonly IStyle<Element> SlotChosen = new ElementStyle
        {
            Base = { Border = Palette.Ink }
        };

        /// <summary>A toolbar slot's number, in its corner.</summary>
        public static readonly IStyle<Label> SlotNumber = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Tiny, Color = Palette.Ink, Padding = new Thickness(4f, 0f) }
        };

        /// <summary>A page of a toolbar, offered beside its other pages; in the ink where its page holds a slot, on a lighter plate where it is the page showing.</summary>
        public static readonly IStyle<Button> SlotPage = new ButtonStyle(Word)
        {
            Base = { FontSize = TextSizes.Tiny, Padding = new Thickness(8f, 4f) },
            [LookStates.Taken] = { Color = Palette.Ink }
        };

        /// <summary>A mark saying something has not been chosen: a ring for one of several, a box for any of them.</summary>
        public static readonly IStyle<Glyph> Mark = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Tiny, Tint = Palette.Faint }
        };

        /// <summary>A mark saying something has not been chosen, where it is pressed by itself, lit under the pointer.</summary>
        public static readonly IStyle<Glyph> MarkPressed = new GlyphStyle(Mark)
        {
            PointerOver = { Background = Palette.PanelHot }
        };

        /// <summary>A mark saying something has been chosen.</summary>
        public static readonly IStyle<Glyph> MarkOn = new GlyphStyle(Mark)
        {
            Base = { Tint = Palette.Live }
        };

        /// <summary>A mark saying something has been chosen, where it is pressed by itself.</summary>
        public static readonly IStyle<Glyph> MarkOnPressed = new GlyphStyle(MarkOn)
        {
            PointerOver = { Background = Palette.PanelHot }
        };

        /// <summary>A mark beside words, muted: the kind of thing they name, or which way a folded column opens; dim on a paused step.</summary>
        public static readonly IStyle<Glyph> Waits = new GlyphStyle
        {
            Base = { Font = Symbols.Font, FontSize = TextSizes.Small, Tint = Palette.Muted },
            [LookStates.Paused] = { Tint = Palette.Dim }
        };

        /// <summary>A choice about the picking itself, lit under the pointer.</summary>
        public static readonly IStyle<Stack> Option = new StackStyle
        {
            Base = { Padding = new Thickness(12f, 4f) },
            PointerOver = { Background = Palette.PanelHot }
        };

        /// <summary>The words of a choice about the picking itself; in the ink where it is chosen.</summary>
        public static readonly IStyle<Label> OptionWord = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Small, Color = Palette.Muted },
            [LookStates.Chosen] = { Color = Palette.Ink }
        };

        /// <summary>A count beside what it counts, quieter than it.</summary>
        public static readonly IStyle<Label> Count = new LabelStyle
        {
            Base = { Font = MyFontEnum.White, FontSize = TextSizes.Tiny, Color = Palette.Faint }
        };

        /// <summary>A question in a dialog, its answers buttons and the one Enter takes the primary.</summary>
        public static readonly IStyle<MessageBox> Asking = new MessageBoxStyle(Dialog)
        {
            Base = { MessageStyle = Question, ButtonStyle = Button, DefaultButtonStyle = Primary }
        };
    }
}
