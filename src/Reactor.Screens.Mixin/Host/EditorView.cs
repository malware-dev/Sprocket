using System.Collections.Generic;
using Reactor.Editing;
using Reactor.Ui;
using VRageMath;
using Phosphor.Graphics;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Host
{
    /// <summary>
    /// Renders a <see cref="ReactorEditor" /> on the Space Engineers screen.
    /// </summary>
    /// <remarks>
    /// The whole of this target's editor. It decides nothing about what the editor does: core states
    /// what is on screen and this decides how it looks, which is the split <c>Conformance</c> exists to
    /// police.
    /// <para>
    /// <b>Nothing already on screen is allowed to move.</b> Controls that come and go keep their place
    /// whether or not they are showing, the heading turns into its own edit box rather than summoning one,
    /// and a question opens directly beneath whatever was pressed so that everything above the press stays
    /// exactly where the eye left it. A screen that rearranges itself as you use it makes you re-find your
    /// place every time you act, and undoes the one thing a layout is for.
    /// </para>
    /// <para>
    /// Shared source rather than a library, because the mod compiles loose files and cannot load one. The
    /// simulator compiles the same files, so what is seen there is what ships.
    /// </para>
    /// </remarks>
    public sealed class EditorView : IScreenHost
    {
        readonly ReactorEditor _editor;

        readonly Dock _root = new Dock();
        readonly Bar _headerBar = new Bar();
        // The chevron alone. It carried the word BACK beside it for a while, which cost most of the room
        // between the left edge and the heading to say what the arrow had already said. What replaces the
        // word is the hover tip, which is where a control that cannot speak for itself is supposed to say
        // what it is - so nothing is actually lost except the space.
        readonly Glyph _back = Widgets.Symbol(Symbols.Left, Palette.Muted);
        readonly HeadingRow _title = new HeadingRow();

        readonly Switch _switch = new Switch(Lettering.Small);
        readonly Label _subtitle = Widgets.Label("", Palette.Faint, Lettering.Small);

        /// <summary>Whether the screen now up has written a subtitle at any point since it opened.</summary>
        bool _hadSubtitle;

        readonly Dock _bodyDock = new Dock();
        readonly Scroll _scroll = new Scroll();

        /// <summary>Where the pointer was when this screen's own thumb last moved.</summary>
        float _held;

        /// <summary>Set by a press on the cross, and read once by whoever owns the window.</summary>
        bool _wantsClose;
        readonly Stack _body = Widgets.Column(0f);
        readonly Stack _above = Widgets.Column(0f);
        readonly Stack _below = Widgets.Column(0f);

        readonly Stack _actions = Widgets.RowOf(Space.Item);
        readonly Label _primary = Widgets.Label("", Palette.Chrome, Lettering.Body);

        readonly Ask _ask = new Ask();

        readonly List<ComponentView> _views = new List<ComponentView>();
        readonly List<EditorComponent> _shown = new List<EditorComponent>();
        readonly Dictionary<EditorComponent, ComponentView> _byComponent =
            new Dictionary<EditorComponent, ComponentView>();

        readonly List<Hit> _hits = new List<Hit>();

        /// <summary>What is lit right now, so it can be put out without searching for it.</summary>
        Element _lit;

        /// <summary>What the lit thing had to say, if anything.</summary>
        string _saying;

        readonly Label _tip = Widgets.Tip();
        readonly List<Hit> _actionHits = new List<Hit>();

        int _actionRevision = -1;
        /// <summary>What an open rename will send its answer to.</summary>
        int _renameCommand;
        /// <summary>Which part of the heading is being typed into, or -1 when none is.</summary>
        int _renaming = -1;

        readonly Pin _pin = new Pin();

        readonly Overlay _overlay = new Overlay();

        TextBox _focus;
        int _focusCommand;
        ComponentView _dragging;

        public EditorView(ReactorEditor editor)
        {
            _editor = editor;

            // The heading, its pencil and the box that replaces it are one control, so the plate is what
            // lights up and what gets pressed. A pencil somebody could see and not press is worse than no
            // pencil: it names the act and then refuses it.


            // Inside a stack that takes the slack, because Fill stretches whatever it is given and a
            // heading lit across the whole header would be a highlight the size of the screen. A
            // horizontal stack hands each child its own width, which is exactly the hug wanted here.
            var titleLane = Widgets.RowOf(0f);
            titleLane.Children.Add(_title.Root);

            // No slot holding it to a width any more. A switch showing both of its positions is already
            // the width of the longer word twice over whichever way it is set, so the title cannot be
            // shifted sideways by somebody stopping a system.
            _headerBar.Spacing = Space.Row;
            _headerBar.Left = _back;
            _headerBar.Fill = titleLane;
            _headerBar.Right = _switch;

            // The subtitle's line is held for as long as a screen that has ever had one is up, and not
            // at all on a screen that never does. Reserving it everywhere was the first answer and it was
            // paying for the picker's guidance on every screen in the editor - a fifth of the header, on
            // screens where nothing can ever write there. Dropping the reservation outright was the other
            // wrong answer: a picker's guidance clears the moment the picking is done, and a header that
            // shrank at that moment would take the whole screen up with it.
            Stack headerBlock = Widgets.Column(Space.Line);
            headerBlock.Background = Palette.Chrome;

            // Less than a row's down the screen, because the title inside it now carries its own - it is
            // a control since the pencil joined it, and the two paddings stack. Eighteen units of chrome
            // above a heading was most of why the header looked like a banner.
            headerBlock.Padding = new Thickness(Space.PadX, Space.Item);
            headerBlock.Children.Add(_headerBar);
            headerBlock.Children.Add(_subtitle);

            Stack header = Widgets.Column(0f);
            header.Children.Add(headerBlock);
            header.Children.Add(Widgets.Line());

                        Widgets.Barred(_scroll);
            _scroll.Content = _body;
            _bodyDock.Padding = new Thickness(Space.PadX);

            _primary.Padding = new Thickness(Space.PadX, Space.ChipY * 1.3f);

            var footerBar = new Bar();
            footerBar.Spacing = Space.Region;
            footerBar.Left = _actions;
            footerBar.Right = _primary;

            Stack footerBlock = Widgets.Column(0f);
            footerBlock.Background = Palette.Chrome;
            footerBlock.Padding = new Thickness(Space.Region, Space.PadY * 0.8f);
            footerBlock.Children.Add(footerBar);

            Stack footer = Widgets.Column(0f);
            footer.Children.Add(Widgets.Line());
            footer.Children.Add(footerBlock);

            _root.Background = Palette.Page;
            _root.Top = header;
            _root.Fill = _bodyDock;
            _root.Bottom = footer;
        }

        /// <summary>
        /// Whether somebody has just asked to leave, answered once and then forgotten.
        /// </summary>
        /// <remarks>
        /// Taken rather than read, so a host that forgets to ask does not close the screen three frames
        /// later on a press nobody remembers making.
        /// </remarks>
        public bool TakeClose()
        {
            bool wanted = _wantsClose;
            _wantsClose = false;
            return wanted;
        }

        /// <summary>Whether a question is open and wants the keyboard.</summary>
        public bool IsAsking
        {
            get { return _ask.IsUp || _renaming >= 0; }
        }

        /// <summary>
        /// Why this screen cannot be left as it stands, or empty when it can.
        /// </summary>
        /// <remarks>
        /// One place that answers "may this be walked away from". The alternative is every control
        /// deciding for itself, which is how back and the door both walk off a refused rename and leave
        /// the box open on the screen they arrive at.
        /// <para>
        /// It is about the <b>edit in hand</b> and not about the model. Core never sees an empty required
        /// name - its own <c>Rename</c> refuses one - so nothing here is ever saved wrong. What is wrong is
        /// a box somebody is being told to fill in, and walking away from that would throw away their words
        /// without saying it had. Core's own idea of a thing being incomplete is a separate business and
        /// already has somewhere to live: the problem line on a sequence.
        /// </para>
        /// <para>
        /// Escape is deliberately not gated. Something has to get you out of a modal state or it is a trap,
        /// and Escape already means "abandon this edit" rather than "leave the screen" while a box is up -
        /// so the way out exists, costs nothing, and is the one everybody already reaches for.
        /// </para>
        /// </remarks>
        public string Blocked
        {
            get
            {
                if (_renaming < 0)
                    return "";

                List<HeadingPart> parts = _editor.Screen.Heading;
                if (_renaming >= parts.Count || !parts[_renaming].NameRequired)
                    return "";

                // Says what to do rather than what is wrong. "Needs a name" is a diagnosis and leaves
                // somebody to work out the cure; there is only one cure and it fits in three words.
                return string.IsNullOrEmpty(AskBox.Text.Trim()) ? "Give it a name first" : "";
            }
        }

        /// <summary>The box a question is being answered in. Only meaningful while <see cref="IsAsking" />.</summary>
        public TextBox AskBox
        {
            get { return _renaming >= 0 ? _title[_renaming].Box : _ask.Box; }
        }

        /// <summary>Whether the caret is drawn this frame. The blink belongs to whoever owns a clock.</summary>
        public bool CaretVisible
        {
            set
            {
                _ask.Box.CaretVisible = value;

                for (int i = 0; i < _title.Count; i++)
                    _title[i].Box.CaretVisible = value;

                if (_focus != null)
                    _focus.CaretVisible = value;
            }
        }

        /// <summary>
        /// Brings the elements into line with the screen. Rebuilt only when the component list actually
        /// changed, which <see cref="ObservableList{T}.Revision" /> answers without a comparison; on every
        /// other tick the existing elements are handed their new values.
        /// </summary>
        public void Update()
        {
            EditorScreen screen = _editor.Screen;

            _title.Update(screen.Heading, _renaming);

            // Latched rather than read straight off, so the line does not come and go inside one screen.
            // Cleared in Rebuild, which is where one screen becomes another.
            if (!string.IsNullOrEmpty(screen.Subtitle))
                _hadSubtitle = true;

            _subtitle.Text = screen.Subtitle;
            _subtitle.Collapsed = !_hadSubtitle;



            // Lit on every screen, the root included, where it leaves the editor rather than retreating a
            // page. Not two controls: a chevron means "leave what I am looking at", and on the front door
            // what you are looking at is the editor. That is the rule Escape has always followed here and
            // nobody has ever found Escape confusing.
            // Lit on every screen including the root, which would otherwise be the one screen everybody
            // arrives on and the one with no visible way off it.
            _back.Tint = Palette.Muted;
            _back.Highlight = Palette.Panel;
            // Which part wears the pencil is the row's business now, because it is a per-part answer.
            if (_renaming >= 0)
                _title[_renaming].Wrong = Blocked.Length > 0;

            // Governs everything on the screen under it, which is what a switch in the header means.
            if (screen.HasSwitch)
                _switch.Show(screen.SwitchIsOn, true);
            else
                _switch.Hide();

            // Dark words on the lit fill, the way the game does it: its focus colour is pale, so a
            // control wearing it reads as lit only if what it says goes dark.
            _primary.Text = screen.HasPrimary ? Widgets.Caps(screen.PrimaryText) : "";
            _primary.Background = screen.HasPrimary ? Palette.Accent : Palette.None;
            _primary.Color = screen.HasPrimary ? Palette.Chrome : Palette.None;
            _primary.Highlight = screen.HasPrimary ? Palette.Ink : Palette.None;

            // A screen that cannot be renamed while the box is open takes the box away with it.
            if (_renaming >= 0 && !screen.CanRename)
                CancelAsk();

            if (!ShowingSameComponents(screen))
                Rebuild(screen);

            for (int i = 0; i < _views.Count; i++)
                _views[i].Update(_editor.CurrentTick);

            if (_actionRevision != screen.Actions.Revision)
            {
                _actionRevision = screen.Actions.Revision;
                RebuildActions(screen);
            }
        }

        /// <summary>How wide a question's popup is, whatever it is hanging under.</summary>
        const float PopupWidth = Sizes.Popup;

        /// <summary>
        /// How big one of the game's own screens is, as a share of the safe area.
        /// </summary>
        /// <remarks>
        /// Read out of the game rather than guessed at. Space Engineers sizes a GUI screen in normalized
        /// units where 1.0 is the safe GUI rectangle exactly - <c>MyGuiManager</c> multiplies by
        /// <c>m_safeGuiRectangle</c> and nothing else - and its terminal, which is the screen Reactor is
        /// the closest thing to, is <c>new Vector2(1.0157f, 0.9172f)</c>.
        /// <para>
        /// So a working screen is the full width of the safe area and about nine tenths of its height.
        /// That is worth writing down because it is not what it looks like: the safe area is four by
        /// three at the full height of the monitor, so on anything wider the screen already sits in a
        /// generous letterbox of its own and does not need a second one. The game's dialogs are a
        /// different thing entirely and much smaller - creating a faction is 0.66 by 0.64.
        /// </para>
        /// <para>
        /// The width here is 1.0 rather than the terminal's 1.0157. That overhang is under a percent
        /// either side and exists because the game's screen background has soft edges to hide; ours has
        /// hard ones, and <see cref="IGraphics.Size" /> is the area the seam calls drawable.
        /// </para>
        /// </remarks>
        public const float ScreenWidth = 1f;

        public const float ScreenHeight = 0.9172f;

        /// <summary>
        /// Draws the editor at the size one of the game's screens is, centred where they are centred.
        /// </summary>
        /// <remarks>
        /// Here rather than in whatever calls it. Where the editor sits is a presentation decision like
        /// every other one in this file, and leaving it to the caller meant the mod and the simulator
        /// each deciding it separately - which is two chances to disagree about the one thing somebody
        /// notices first.
        /// </remarks>
        public void Draw(IGraphics g)
        {
            Vector2 size = g.Size;

            float width = size.X * ScreenWidth;
            float height = size.Y * ScreenHeight;

            Draw(g, new RectangleF(
                (size.X - width) * 0.5f, (size.Y - height) * 0.5f, width, height));
        }

        public void Draw(IGraphics g, RectangleF bounds)
        {
            _root.Measure(g, new Vector2(bounds.Width, bounds.Height));
            _root.Arrange(bounds);

            LightWhatIsUnderThePointer();

            _root.Draw(g);

            DrawPopup(g, bounds);
            DrawOverlay(g, bounds);
            DrawTip(g, bounds);
            CollectHits();
        }

        /// <summary>
        /// Where the pointer is, in UI units. Somewhere outside the screen means there is not one.
        /// </summary>
        /// <remarks>
        /// A property the host sets rather than an argument to <see cref="Draw" />, because a target
        /// without a pointer - a text panel, a screenshot, a test - should not have to pass one. What it
        /// leaves alone stays where it was, which is off the screen and therefore over nothing.
        /// </remarks>
        public Vector2 Pointer { get; set; }

        /// <summary>
        /// Lights the one thing the pointer is resting on, and puts out whatever was lit before.
        /// </summary>
        /// <remarks>
        /// Worked out from <b>the previous frame's</b> hits, which is why this can run before anything is
        /// drawn. Hits are gathered after a layout because that is when rectangles exist, so the only
        /// alternative was arranging everything, gathering, and then drawing everything - two passes over
        /// the popup and the overlay to save one frame of lag on a highlight nobody can perceive.
        /// <para>
        /// Only one thing is ever lit. Hits are walked last first, the same order a press walks them, so
        /// the thing that would be pressed is the thing that lights - and a row under a popup cannot light
        /// up through it.
        /// </para>
        /// </remarks>
        void LightWhatIsUnderThePointer()
        {
            Element wanted = null;
            string says = null;

            // A popup owns the screen and nothing behind it should answer the pointer. A rename does not:
            // it happens in place, on a screen that is otherwise ordinary, and it is the one state where
            // something on that screen has a reason to say what is wrong with it.
            if (!_ask.IsUp || _renaming >= 0)
                wanted = Under(Pointer, out says);

            _saying = says;

            if (wanted == _lit)
                return;

            if (_lit != null)
                _lit.IsHot = false;

            _lit = wanted;

            if (_lit != null)
                _lit.IsHot = true;
        }

        /// <summary>
        /// Whether a hit is the one a press or a hover at <paramref name="point" /> should reach.
        /// </summary>
        /// <remarks>
        /// Three conditions, in one place, because pressing and lighting have to agree about them
        /// exactly: a thing you can see light up and then cannot press is worse than one that never lit.
        /// <para>
        /// The scroll test is against the body rather than the row, because a row scrolled past the top
        /// of its column keeps its rectangle and would otherwise still answer. The overlay test is
        /// against the row's middle rather than the point, because an overlay narrows what the body will
        /// answer without taking the press itself.
        /// </para>
        /// </remarks>
        bool Answers(Hit hit, Vector2 point, bool overlaid)
        {
            if (!Contains(hit.Bounds, point))
                return false;

            if (hit.Scrolled && !Contains(_bodyDock.Bounds, point))
                return false;

            return !overlaid || _overlay.Covers(Middle(hit.Bounds));
        }

        /// <summary>What would be lit if the pointer were at <paramref name="point" />, or null.</summary>
        Element Under(Vector2 point, out string says)
        {
            says = null;

            bool overlaid = _overlay.IsUp;
            if (overlaid && !_overlay.Covers(point))
                return null;

            for (int i = _hits.Count - 1; i >= 0; i--)
            {
                Hit hit = _hits[i];

                if (hit.Hot == null || !Answers(hit, point, overlaid))
                    continue;

                says = hit.Tip;
                return hit.Hot;
            }

            return null;
        }

        /// <summary>
        /// Puts a note beside the pointer, if what it is resting on had anything to say.
        /// </summary>
        /// <remarks>
        /// Drawn last, after the popup and the overlay, because it is the one thing on the screen that is
        /// allowed over everything - including the list a person opened, whose rows it may well be
        /// describing.
        /// <para>
        /// It follows the pointer rather than hanging off the control, which is the opposite of what the
        /// overlay does and is right for the same reason the overlay is: a menu belongs to the thing that
        /// opened it, and a note belongs to the eye that is already looking at the pointer.
        /// </para>
        /// </remarks>
        void DrawTip(IGraphics g, RectangleF bounds)
        {
            if (string.IsNullOrEmpty(_saying))
                return;

            _tip.Text = _saying;
            _tip.Measure(g, new Vector2(Sizes.Popup, bounds.Height));

            Vector2 size = _tip.DesiredSize;

            // Below and to the right, where the arrow is not covering what was just read. Only flipped
            // when there is no room, so it does not dance about the pointer near an edge.
            float x = Pointer.X + Space.Item;
            float y = Pointer.Y + Space.Region;

            if (x + size.X > bounds.X + bounds.Width)
                x = Pointer.X - size.X - Space.Item;

            if (y + size.Y > bounds.Y + bounds.Height)
                y = Pointer.Y - size.Y - Space.Row;

            if (x < bounds.X)
                x = bounds.X;

            if (y < bounds.Y)
                y = bounds.Y;

            _tip.Arrange(new RectangleF(x, y, size.X, size.Y));
            _tip.Draw(g);
        }

        /// <summary>
        /// Hangs whatever a view asked for over the screen, under the thing it was anchored to.
        /// </summary>
        /// <remarks>
        /// The same arrangement the question popup uses, and for the same reason: it takes part in no
        /// measuring, so nothing moves to make room for it. Views cannot do this for themselves because a
        /// view draws where it was arranged, inside its neighbours rather than over them.
        /// </remarks>
        void DrawOverlay(IGraphics g, RectangleF bounds)
        {
            _overlay.Draw(g, bounds, PopupWidth);
        }

        public void ShowOverlay(Element content, Element anchor)
        {
            _overlay.Show(content, anchor);
        }

        public void HideOverlay()
        {
            _overlay.Hide();
        }

        public bool IsOverlay(Element content)
        {
            return _overlay.Is(content);
        }

        public void SetNumber(int command, int value)
        {
            if (command != 0)
                _editor.SetNumber(command, value);
        }

        public void SetToggle(int command, bool on)
        {
            if (command != 0)
                _editor.SetToggle(command, on);
        }

        /// <summary>The box the keyboard is in, or null when nothing is being typed into.</summary>
        public TextBox Focus
        {
            get { return _focus; }
        }

        /// <summary>
        /// Sends what has just been typed wherever it belongs.
        /// </summary>
        /// <remarks>
        /// Asked for afresh on every keystroke rather than on finishing, because a search that only
        /// narrowed when you stopped typing would feel broken, and because the command id is re-issued on
        /// every rebuild - one captured earlier would be dead by the time the typing ended.
        /// </remarks>
        public void Typed()
        {
            if (_focus != null && _focusCommand != 0)
                _editor.SetText(_focusCommand, _focus.Text);
        }

        void Unfocus()
        {
            if (_focus == null)
                return;

            _focus.IsEditing = false;
            _focus = null;
            _focusCommand = 0;
        }

        /// <summary>
        /// Puts the question over the screen, left-aligned under whatever opened it.
        /// </summary>
        /// <remarks>
        /// Drawn after the layout rather than inside it, which is the whole point: it takes part in no
        /// measuring, so nothing moves to make room for it. It follows its anchor because the anchor's
        /// rectangle is read every frame, so scrolling carries the popup along rather than stranding it.
        /// </remarks>
        void DrawPopup(IGraphics g, RectangleF bounds)
        {
            // Not while a heading is being renamed: that happens in place, in the heading's own words.
            if (_renaming >= 0)
                return;

            _ask.Draw(g, bounds, PopupWidth);
        }

        /// <summary>
        /// Moves whatever is under <paramref name="point" />, if anything there can move.
        /// </summary>
        /// <remarks>
        /// Offered to the views first, because a component that scrolls itself is not inside the
        /// screen's scroller and the screen's scroller is not even in the layout - so sending every
        /// turn of the wheel to it meant a picker could not be scrolled at all.
        /// </remarks>
        public void ScrollBy(Vector2 point, float amount)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i].ScrollBy(point, amount))
                    return;
            }

            if (_scroll.Parent != null)
                _scroll.ScrollBy(amount);
        }

        /// <summary>
        /// Offers the press to whatever might want to be dragged, before anything treats it as a click.
        /// </summary>
        public bool Grab(Vector2 point)
        {
            if (_overlay.IsUp || IsAsking)
                return false;

            for (int i = 0; i < _views.Count; i++)
            {
                if (!_views[i].TryGrab(point))
                    continue;

                _dragging = _views[i];
                return true;
            }

            // After the views, not before. A picker fills the body with its own columns and their bars sit
            // inside this one's track; offering the screen's bar first would take every one of them.
            if (_scroll.Parent == null || !_scroll.HasBar || !Contains(_scroll.Track, point))
                return false;

            if (!Contains(_scroll.Thumb, point))
                _scroll.ThumbTo(point.Y);

            _scroll.BarHeld = true;
            _held = point.Y;
            return true;
        }

        /// <summary>Where the thing being carried has got to. Ignored when nothing is being carried.</summary>
        public void Carry(Vector2 point)
        {
            if (_scroll.BarHeld)
            {
                _scroll.DragBy(point.Y - _held);
                _held = point.Y;
                return;
            }

            if (_dragging != null)
                _dragging.Carry(point);
        }

        /// <summary>Lets go of whatever was being carried.</summary>
        public void Drop()
        {
            _scroll.BarHeld = false;

            if (_dragging == null)
                return;

            _dragging.Drop();
            _dragging = null;
        }

        /// <summary>Whether something is being carried right now.</summary>
        public bool IsDragging
        {
            get { return _dragging != null || _scroll.BarHeld; }
        }

        /// <summary>
        /// Acts on whatever is under <paramref name="point" />, and answers whether anything was.
        /// </summary>
        /// <remarks>
        /// Hit rectangles are gathered after the layout rather than kept alongside it, because arranging is
        /// the only thing that knows where anything ended up. Anything inside the scrolling region is only
        /// pressable where it is actually on show, so a row scrolled up behind the header cannot be hit
        /// through it.
        /// </remarks>
        public bool Press(Vector2 point)
        {
            return Press(point, 1);
        }

        /// <param name="clicks">
        /// How many presses in quick succession this is. A second one on a row that says it confirms
        /// means "this one, and move on" - taking the choice and going in one gesture.
        /// </param>
        public bool Press(Vector2 point, int clicks)
        {
            // While a question is open it owns the screen. Pressing inside it is the only thing that can
            // reach anything, and pressing anywhere else puts it away and goes no further - the press that
            // dismisses is not also a press on whatever happened to be underneath.
            if (_ask.IsUp && _renaming < 0)
            {
                if (!_ask.Covers(point))
                {
                    CancelAsk();
                    return true;
                }

                if (_ask.OnConfirm(point) && !string.IsNullOrEmpty(_ask.Answer))
                    CommitAsk();

                return true;
            }

            // An unfinishable edit owns the screen too, and more firmly: nothing outside the box it is in
            // will answer at all. Letting the odd control through was the first shape of this and it does
            // not work - every one of them either navigates away from the box or ends up committing it,
            // and a rule with a list of exceptions is a rule nobody can predict.
            //
            // Blunt, and the screen is already saying why in the box itself. Escape is the way out.
            if (Blocked.Length > 0)
                return Contains(_title[_renaming].Bounds, point);

            // An overlay owns the screen the same way a question does, except that what is inside it is
            // ordinary and can be pressed through the usual list.
            bool overlaid = _overlay.IsUp;
            if (overlaid && !_overlay.Covers(point))
            {
                HideOverlay();
                return true;
            }

            // Last first, so a thing drawn over another is the one that gets pressed.
            for (int i = _hits.Count - 1; i >= 0; i--)
            {
                Hit hit = _hits[i];

                if (!Answers(hit, point, overlaid))
                    continue;

                if (hit.Overlay != null)
                {
                    Unfocus();

                    // Pressing the line a second time puts the list away, which is what a thing that
                    // opens downward is expected to do.
                    if (IsOverlay(hit.Overlay))
                        HideOverlay();
                    else
                        ShowOverlay(hit.Overlay, _hits[i].Anchor ?? Anchored(hit.Bounds));

                    return true;
                }

                if (hit.Box != null)
                {
                    Unfocus();
                    _focus = hit.Box;
                    _focusCommand = hit.Command;
                    _focus.IsEditing = true;
                    _focus.SelectAll();
                    return true;
                }

                Unfocus();

                // Before anything the press might do, and not only before an ordinary command. Pressing
                // a switch, or the way out, or a control that asks its own question, is pressing away from
                // the rename just as much as pressing a row is - and three of the branches below return
                // without reaching the end of this method, so anything placed there would be skipped and
                // the typing thrown away in silence.
                if (_renaming >= 0)
                    CommitAsk();

                if (hit.Closes)
                {
                    _wantsClose = true;
                    return true;
                }

                if (hit.Toggles)
                {
                    SetToggle(hit.Command, hit.ToggleTo);
                    return true;
                }

                if (!string.IsNullOrEmpty(hit.Question))
                {
                    BeginAsk(hit);
                    return true;
                }

                _editor.Invoke(hit.Command);

                // Taking a choice and moving on in one gesture, where one thing is being chosen. Where
                // several can be, a second press means un-choosing, and moving on from that would be
                // taking away what somebody had just done.
                if (clicks > 1 && hit.Confirms && _editor.Screen.HasPrimary)
                    _editor.Invoke(_editor.Screen.PrimaryCommand);

                return true;
            }

            // Nothing under the pointer, which is still an act rather than a miss. A caret left blinking
            // in a search box goes on taking every key typed after it, wherever somebody thought they were
            // typing; and a heading left standing as a box is one nobody asked to go on editing. Pressing
            // away from either is how they are put down, and it is the only way to put the first down at
            // all - there is nothing else to press that means "stop typing".
            //
            // A press inside the box being typed in is not away. Nothing collects a hit for it while it is
            // up, so without this it would fall through here and commit the very edit it was aiming into.
            //
            // Everywhere else, pressing away from a rename keeps what was typed. Enter and pressing
            // elsewhere mean the same thing - the name is whatever the box says - because there is no
            // confirm button to press and never was.
            if (_renaming >= 0 && Contains(_title[_renaming].Bounds, point))
                return true;

            if (_renaming >= 0)
                CommitAsk();

            Unfocus();
            return false;
        }

        /// <summary>
        /// Sends the answer and closes the question. An empty answer is sent anyway: core ignores one
        /// rather than refusing it, and deciding here that empty means cancel would be this target
        /// inventing a rule core deliberately did not make.
        /// </summary>
        public void CommitAsk()
        {
            if (!IsAsking)
                return;

            string answer = AskBox.Text;

            // Carried from when the box opened, which is safe: an id is derived from what a command is
            // for rather than counted out, so a rebuild between opening the box and answering it hands
            // the same control the same number. Worth stating because `SetText` ignores an id it does
            // not know without a word, so if this were ever wrong the answer would go missing in
            // silence rather than fail.
            int command = _renaming >= 0 ? _renameCommand : _ask.Command;

            // Empty means something when renaming - it hands the heading back to whatever the thing says
            // about itself - and means nothing at all when starting one. It also means nothing where core
            // says a name is required, which is where there is no worked-out heading to hand it back to.
            //
            // Refused by leaving the box open rather than by closing it and doing nothing. Somebody who
            // has just emptied a name they are required to give is mid-thought, and taking the box away
            // makes them find the pencil again to finish it.
            if (string.IsNullOrEmpty(answer) && (_renaming < 0 || Blocked.Length > 0))
                return;

            Close();

            if (command != 0)
                _editor.SetText(command, answer);
        }

        /// <summary>Abandons the question. Core was never told it was open, so nothing has to be undone.</summary>
        public void CancelAsk()
        {
            if (!IsAsking)
                return;

            Close();
        }

        void BeginAsk(Hit hit)
        {
            Close();


            // Which part of the heading this was, if it was one at all. Matched on the command rather
            // than carried on the hit, because the command is the one thing that already identifies a
            // part and a second way of saying it is a second way of being wrong.
            List<HeadingPart> parts = _editor.Screen.Heading;
            for (int i = 0; i < parts.Count && i < _title.Count; i++)
            {
                if (parts[i].RenameCommand != hit.Command || hit.Command == 0)
                    continue;

                // The words become the box, in the words' own place. What that takes is the plate's
                // business, not this one's.
                _renaming = i;
                _renameCommand = hit.Command;
                _title[i].Text = parts[i].Name;
                _title[i].Begin();
                return;
            }

            // Everything else hangs over the screen, under whatever was pressed.
            _ask.Open(hit.Question, hit.Anchor, hit.Command);
        }

        void Close()
        {
            if (_renaming >= 0)
            {
                _title[_renaming].End();
                _title[_renaming].Wrong = false;
                _renaming = -1;
            }

            _renameCommand = 0;
            _ask.Close();
        }

        void CollectHits()
        {
            _hits.Clear();

            EditorScreen screen = _editor.Screen;

            // One control, two acts: a command while there is somewhere to retreat to, and the door once
            // there is not. Which of the two it is gets said in the tip rather than in the picture - a
            // symbol that changed as somebody walked back up the pages would be a control moving under the
            // pointer without moving.
            //
            // It earns a tip at all because it has no word beside it any more, which is the case the tip
            // exists for exactly. The wording is this target's own: core publishes none for Back, and
            // putting one there is Design decision 11.
            Hit back = screen.CanGoBack
                ? Hit.OnFixed(_back, screen.BackCommand)
                : Hit.Leaves(_back);

            back.Tip = screen.CanGoBack ? "Back" : "Close the editor";
            _hits.Add(back);

            // While the name is one that cannot be given, the heading's note says so instead of saying
            // what the heading usually says. Not a timer and not a thing of its own: it is the ordinary
            // note, on the ordinary control, carrying different words for exactly as long as they are
            // true. Nothing else on the screen has to know that an error can exist.
            // Renaming hangs off the heading, because the heading is what the name is for - and off the
            // part of it that the name belongs to, which is the whole reason a heading is several things.
            for (int i = 0; i < screen.Heading.Count && i < _title.Count; i++)
            {
                HeadingPart part = screen.Heading[i];
                if (!part.CanRename || i == _renaming)
                    continue;

                Hit rename = Hit.Fixed(_title[i].Bounds, part.RenameCommand, part.Prompt);
                rename.Hot = _title[i];
                _hits.Add(rename);
            }

            // While the name in hand is one that cannot be given, that part's note says so instead of
            // saying what it usually says. Not a timer and not a thing of its own: the ordinary note, on
            // the ordinary control, carrying different words for exactly as long as they are true.
            string wrong = Blocked;
            if (wrong.Length > 0)
            {
                Hit says = Hit.Fixed(_title[_renaming].Bounds, 0);
                says.Hot = _title[_renaming];
                says.Tip = wrong;
                _hits.Add(says);
            }

            if (screen.HasSwitch)
            {
                Hit toggle = Hit.Switch(_switch.Bounds, screen.SwitchCommand, !screen.SwitchIsOn);
                toggle.Scrolled = false;
                toggle.Hot = _switch;
                // A system's, always - core says so where it declares the command, and a sequence's
                // switch lives on the sentence rather than up here.
                toggle.Tip = StatusRow.Switching(screen.Heading[0].Text, true, screen.SwitchIsOn);

                _hits.Add(toggle);
            }

            for (int i = 0; i < _views.Count; i++)
                _views[i].Collect(_hits);

            for (int i = 0; i < _actionHits.Count && i < _actions.Children.Count; i++)
                _hits.Add(Hit.OnFixed(_actions.Children[i], _actionHits[i].Command));

            if (screen.HasPrimary)
                _hits.Add(Hit.OnFixed(_primary, screen.PrimaryCommand));
        }

        /// <summary>
        /// Whether the screen is still made of the very same components as last time.
        /// </summary>
        /// <remarks>
        /// Identity, not equality. Core hands back the same component instances whenever it can, so this
        /// is false only when the screen genuinely changed shape - which is what lets a view, and the
        /// caret and the scroll position inside it, survive everything else.
        /// </remarks>
        bool ShowingSameComponents(EditorScreen screen)
        {
            if (screen.Components.Count != _shown.Count)
                return false;

            for (int i = 0; i < _shown.Count; i++)
            {
                if (!ReferenceEquals(screen.Components[i], _shown[i]))
                    return false;
            }

            return true;
        }

        void Rebuild(EditorScreen screen)
        {
            Close();
            HideOverlay();
            Unfocus();

            _views.Clear();
            _shown.Clear();
            _hadSubtitle = false;

            for (int i = 0; i < screen.Components.Count; i++)
            {
                EditorComponent component = screen.Components[i];

                ComponentView view;
                if (!_byComponent.TryGetValue(component, out view))
                {
                    view = ComponentView.For(component);
                    view.Host = this;
                    _byComponent.Add(component, view);
                }

                _shown.Add(component);
                _views.Add(view);
            }

            // Anything no longer on screen is dropped, so its view goes with it rather than accumulating
            // for the life of the session.
            if (_byComponent.Count > _shown.Count)
            {
                var stale = new List<EditorComponent>();

                foreach (KeyValuePair<EditorComponent, ComponentView> pair in _byComponent)
                {
                    if (!_shown.Contains(pair.Key))
                        stale.Add(pair.Key);
                }

                for (int i = 0; i < stale.Count; i++)
                    _byComponent.Remove(stale[i]);
            }

            Arrange(screen);
        }

        /// <summary>
        /// Stacks the fixed parts and gives the rest of the height to whichever component asked for it.
        /// </summary>
        /// <remarks>
        /// Core marks at most one component as the one a screen is really about, and that one takes the
        /// leftover height and scrolls inside itself. Without this the whole screen scrolls, which carries
        /// the search box and the filters off the top along with the rows they are filtering. A screen with
        /// nothing claiming the space simply scrolls as a whole, which is right for a form.
        /// </remarks>
        void Arrange(EditorScreen screen)
        {
            _body.Children.Clear();
            _above.Children.Clear();
            _below.Children.Clear();

            int filler = -1;
            for (int i = 0; i < screen.Components.Count && i < _views.Count; i++)
            {
                if (screen.Components[i].Fills)
                {
                    filler = i;
                    break;
                }
            }

            if (filler < 0)
            {
                for (int i = 0; i < _views.Count; i++)
                    _body.Children.Add(_views[i].Root);

                _bodyDock.Top = null;
                _bodyDock.Bottom = null;
                _bodyDock.Fill = _scroll;
                return;
            }

            for (int i = 0; i < filler; i++)
                _above.Children.Add(_views[i].Root);

            for (int i = filler + 1; i < _views.Count; i++)
                _below.Children.Add(_views[i].Root);

            _bodyDock.Top = _above;
            _bodyDock.Bottom = _below;

            // A component that scrolls itself is handed the space directly. Wrapping it would put a
            // second scroller around the first, and the two would fight over one gesture.
            if (_views[filler].ScrollsItself)
            {
                _bodyDock.Fill = _views[filler].Root;
            }
            else
            {
                _body.Children.Add(_views[filler].Root);
                _bodyDock.Fill = _scroll;
            }
        }

        void RebuildActions(EditorScreen screen)
        {
            _actions.Children.Clear();
            _actionHits.Clear();

            for (int i = 0; i < screen.Actions.Count; i++)
            {
                ChoiceEntry entry = screen.Actions[i];

                Label action = Widgets.Ghost(Widgets.Caps(entry.Text));
                action.Highlight = Palette.Panel;
                if (entry.IsCurrent)
                    action.Color = Palette.Ink;

                _actions.Children.Add(action);
                _actionHits.Add(new Hit(new RectangleF(), entry.Command));
            }
        }

        /// <summary>
        /// A stand-in for an anchor element, when all that is known is where the press landed.
        /// </summary>
        /// <remarks>
        /// Overlays follow their anchor every frame so that scrolling carries them along. A hit that
        /// names no element gets one that never moves, which is right for something in the chrome and
        /// merely acceptable for anything else.
        /// </remarks>
        Element Anchored(RectangleF bounds)
        {
            _pin.Arrange(bounds);
            return _pin;
        }

        /// <summary>The centre of a rectangle, for asking which region something belongs to.</summary>
        static Vector2 Middle(RectangleF bounds)
        {
            return new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
        }

        internal static bool Contains(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }
    }
}
