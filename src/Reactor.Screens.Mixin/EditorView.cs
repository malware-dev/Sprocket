using System.Collections.Generic;
using Phosphor;
using Reactor.Editing;
using Reactor.Ui;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>
    /// Renders a <see cref="ReactorEditor" /> on the Space Engineers screen.
    /// <para>
    /// The whole of this target's editor, and the counterpart to the Avalonia host's file of the same
    /// name. It decides nothing about what the editor does: core states what is on screen and this decides
    /// how it looks, which is the split <c>Conformance</c> exists to police.
    /// </para>
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
    /// </summary>
    public sealed class EditorView : IScreenHost
    {
        readonly ReactorEditor _editor;

        readonly Dock _root = new Dock();
        readonly Bar _headerBar = new Bar();
        readonly Label _back = Widgets.Ghost(Widgets.Caps("< Back"));
        readonly Stack _titleRow = Widgets.RowOf(8f);
        readonly Label _title = Widgets.Label("", Palette.Ink, Type.Title);
        readonly TextBox _titleBox = new TextBox();
        readonly Label _pencil = Widgets.Label("rename", Palette.Faint, Type.Tiny);
        readonly Label _switch = Widgets.Label("", Palette.Ink, Type.Small);
        readonly Label _subtitle = Widgets.Label("", Palette.Faint, Type.Small);

        readonly Dock _bodyDock = new Dock();
        readonly Scroll _scroll = new Scroll();
        readonly Stack _body = Widgets.Column(0f);
        readonly Stack _above = Widgets.Column(0f);
        readonly Stack _below = Widgets.Column(0f);

        readonly Stack _actions = Widgets.RowOf(6f);
        readonly Label _primary = Widgets.Label("", Palette.Chrome, Type.Body);

        readonly Stack _popup = Widgets.Column(6f);
        readonly Label _askLabel = Widgets.Label("", Palette.Muted, Type.Tiny);
        readonly TextBox _askBox = new TextBox();
        readonly Label _confirm = Widgets.Label("Continue", Palette.Ink, Type.Small);

        readonly List<ComponentView> _views = new List<ComponentView>();
        readonly List<EditorComponent> _shown = new List<EditorComponent>();
        readonly Dictionary<EditorComponent, ComponentView> _byComponent =
            new Dictionary<EditorComponent, ComponentView>();

        readonly List<Hit> _hits = new List<Hit>();
        readonly List<Hit> _actionHits = new List<Hit>();

        int _actionRevision = -1;
        int _askCommand;
        bool _asking;
        bool _renaming;
        Element _anchor;
        RectangleF _popupBounds;

        readonly Pin _pin = new Pin();

        Element _overlay;
        Element _overlayAnchor;
        RectangleF _overlayBounds;

        TextBox _focus;
        int _focusCommand;
        ComponentView _dragging;

        public EditorView(ReactorEditor editor)
        {
            _editor = editor;

            _switch.Padding = new Thickness(10f, 4f);

            // The heading holds one line whatever the name. Trimming it to an ellipsis is what the other
            // host does and what this wants; until Phosphor can trim, it overflows rather than growing the
            // header, which is the lesser of the two wrongs.
            _title.Wraps = false;
            _title.Trims = true;
            _switch.Wraps = false;
            _back.Wraps = false;
            _pencil.Wraps = false;

            // The heading and the box that edits it share a place and a size, so swapping them moves
            // nothing. A popup would put a second copy of the name over the top of the first and ask which
            // one is real; the heading already says it, so it becomes the box instead of summoning one.
            _titleBox.Scale = Type.Title;
            _titleBox.Color = Palette.Ink;
            _titleBox.Background = Palette.PanelHot;
            _titleBox.IsEditing = true;

            _titleRow.Children.Add(_title);
            _titleRow.Children.Add(_pencil);

            // Held to the width of the longer word, with both centred in it, because the two are not the
            // same length in a proportional face and the title would step sideways every time somebody
            // stopped a system. The fill is the slot rather than the words, so the shape does not change
            // either - only what it says.
            _switch.Centres = true;
            var switchSlot = new Sized(104f);
            switchSlot.Content = _switch;

            _headerBar.Spacing = 10f;
            _headerBar.Left = _back;
            _headerBar.Fill = _titleRow;
            _headerBar.Right = switchSlot;

            // The subtitle's line is reserved whether or not there is one. A picker's subtitle empties as
            // the picking finishes, and a header that resized when it did would be the screen moving under
            // somebody's hands.
            Stack headerBlock = Widgets.Column(2f);
            headerBlock.Background = Palette.Chrome;
            headerBlock.Padding = new Thickness(12f, 9f);
            headerBlock.Children.Add(_headerBar);
            headerBlock.Children.Add(_subtitle);

            Stack header = Widgets.Column(0f);
            header.Children.Add(headerBlock);
            header.Children.Add(Widgets.Line());

            _askBox.Scale = Type.Body;
            _askBox.Background = Palette.Page;
            _askBox.Padding = new Thickness(8f, 5f);
            _askBox.Edge = Palette.Line;
            _askBox.IsEditing = true;

            _confirm.Padding = new Thickness(14f, 5f);

            var entry = new Bar();
            entry.Spacing = 8f;
            entry.Fill = _askBox;
            entry.Right = _confirm;

            // A popup rather than a panel in the flow: it hangs over the screen instead of shoving it
            // down, and it is anchored to whatever opened it. Core knows none of that and should not.
            _popup.Background = Palette.PanelHot;
            _popup.Edge = Palette.Line;
            _popup.Padding = new Thickness(10f, 8f);
            _popup.Children.Add(_askLabel);
            _popup.Children.Add(entry);

            _scroll.Content = _body;
            _bodyDock.Padding = new Thickness(12f);

            _primary.Padding = new Thickness(14f, 5f);

            var footerBar = new Bar();
            footerBar.Spacing = 10f;
            footerBar.Left = _actions;
            footerBar.Right = _primary;

            Stack footerBlock = Widgets.Column(0f);
            footerBlock.Background = Palette.Chrome;
            footerBlock.Padding = new Thickness(10f, 7f);
            footerBlock.Children.Add(footerBar);

            Stack footer = Widgets.Column(0f);
            footer.Children.Add(Widgets.Line());
            footer.Children.Add(footerBlock);

            _root.Background = Palette.Page;
            _root.Top = header;
            _root.Fill = _bodyDock;
            _root.Bottom = footer;
        }

        /// <summary>Whether a question is open and wants the keyboard.</summary>
        public bool IsAsking
        {
            get { return _asking; }
        }

        /// <summary>The box a question is being answered in. Only meaningful while <see cref="IsAsking" />.</summary>
        public TextBox AskBox
        {
            get { return _renaming ? _titleBox : _askBox; }
        }

        /// <summary>Whether the caret is drawn this frame. The blink belongs to whoever owns a clock.</summary>
        public bool CaretVisible
        {
            set
            {
                _askBox.CaretVisible = value;
                _titleBox.CaretVisible = value;

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

            // Whatever is being typed is the truth while it is being typed. Writing the screen's own name
            // back into the box every tick would fight the caret for it.
            if (!_renaming)
                _title.Text = screen.Title;

            _subtitle.Text = screen.Subtitle;

            // Kept in place and merely dimmed out rather than emptied. An element with no text still holds
            // its line, but it does not hold its width, and a title that slid sideways whenever Back came
            // and went would be exactly the fidgeting this screen is trying not to do.
            _back.Color = screen.CanGoBack ? Palette.Muted : Palette.None;
            _pencil.Color = screen.CanRename && !_renaming ? Palette.Faint : Palette.None;

            // Both words are padded to one width, so toggling it does not shuffle the title along. Worded
            // by what pressing it does rather than by what state it is in, so nobody has to work out which
            // way round it is before daring to press.
            _switch.Text = Widgets.Caps(screen.SwitchIsOn ? "Pause" : "Run");
            _switch.Background = screen.HasSwitch
                ? (screen.SwitchIsOn ? Palette.Notice : Palette.Live)
                : Palette.None;
            _switch.Color = screen.HasSwitch ? Palette.Chrome : Palette.None;

            // Dark words on the lit fill, the way the game does it: its focus colour is pale, so a
            // control wearing it reads as lit only if what it says goes dark.
            _primary.Text = screen.HasPrimary ? Widgets.Caps(screen.PrimaryText) : "";
            _primary.Background = screen.HasPrimary ? Palette.Accent : Palette.None;
            _primary.Color = screen.HasPrimary ? Palette.Chrome : Palette.None;

            // A screen that cannot be renamed while the box is open takes the box away with it.
            if (_renaming && !screen.CanRename)
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
        const float PopupWidth = 320f;

        public void Draw(IGraphics g, RectangleF bounds)
        {
            _root.Measure(g, new Vector2(bounds.Width, bounds.Height));
            _root.Arrange(bounds);
            _root.Draw(g);

            DrawPopup(g, bounds);
            DrawOverlay(g, bounds);
            CollectHits();
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
            if (_overlay == null || _overlayAnchor == null)
            {
                _overlayBounds = new RectangleF();
                return;
            }

            _overlay.Measure(g, new Vector2(PopupWidth, bounds.Height));

            float height = _overlay.DesiredSize.Y;
            float width = _overlay.DesiredSize.X;
            float x = _overlayAnchor.Bounds.X;
            float y = _overlayAnchor.Bounds.Y + _overlayAnchor.Bounds.Height + 2f;

            if (y + height > bounds.Y + bounds.Height)
                y = bounds.Y + bounds.Height - height;
            if (y < bounds.Y)
                y = bounds.Y;

            if (x + width > bounds.X + bounds.Width)
                x = bounds.X + bounds.Width - width;
            if (x < bounds.X)
                x = bounds.X;

            _overlayBounds = new RectangleF(x, y, width, height);
            _overlay.Arrange(_overlayBounds);
            _overlay.Draw(g);
        }

        public void ShowOverlay(Element content, Element anchor)
        {
            _overlay = content;
            _overlayAnchor = anchor;
        }

        public void HideOverlay()
        {
            _overlay = null;
            _overlayAnchor = null;
            _overlayBounds = new RectangleF();
        }

        public bool IsOverlay(Element content)
        {
            return _overlay == content;
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
        /// <para>
        /// Drawn after the layout rather than inside it, which is the whole point: it takes part in no
        /// measuring, so nothing moves to make room for it. It follows its anchor because the anchor's
        /// rectangle is read every frame, so scrolling carries the popup along rather than stranding it.
        /// </para>
        /// </summary>
        void DrawPopup(IGraphics g, RectangleF bounds)
        {
            if (!_asking || _renaming || _anchor == null)
            {
                _popupBounds = new RectangleF();
                return;
            }

            _popup.Measure(g, new Vector2(PopupWidth, bounds.Height));

            float height = _popup.DesiredSize.Y;
            float x = _anchor.Bounds.X;
            float y = _anchor.Bounds.Y + _anchor.Bounds.Height + 2f;

            // Kept on the screen. A popup that opened half off the bottom would be a question nobody
            // could answer, which is worse than one that sits above its anchor instead.
            if (y + height > bounds.Y + bounds.Height)
                y = _anchor.Bounds.Y - height - 2f;

            if (x + PopupWidth > bounds.X + bounds.Width)
                x = bounds.X + bounds.Width - PopupWidth;
            if (x < bounds.X)
                x = bounds.X;

            _popupBounds = new RectangleF(x, y, PopupWidth, height);
            _popup.Arrange(_popupBounds);

            // Dead until there is something to send, because an empty answer here is not a meaningful
            // one. Renaming is the opposite case and allows it, which is why the two are told apart.
            bool ready = !string.IsNullOrEmpty(_askBox.Text);
            _confirm.Background = ready ? Palette.Accent : Palette.Dim;
            _confirm.Color = ready ? Palette.Chrome : Palette.Faint;

            _popup.Draw(g);
        }

        /// <summary>Moves the scrolling region, if there is one and there is anything out of sight.</summary>
        public void ScrollBy(float amount)
        {
            if (_scroll.Parent != null)
                _scroll.ScrollBy(amount);
        }

        /// <summary>
        /// Offers the press to whatever might want to be dragged, before anything treats it as a click.
        /// </summary>
        public bool Grab(Vector2 point)
        {
            if (_overlay != null || _asking)
                return false;

            for (int i = 0; i < _views.Count; i++)
            {
                if (!_views[i].TryGrab(point))
                    continue;

                _dragging = _views[i];
                return true;
            }

            return false;
        }

        /// <summary>Where the thing being carried has got to. Ignored when nothing is being carried.</summary>
        public void Carry(Vector2 point)
        {
            if (_dragging != null)
                _dragging.Carry(point);
        }

        /// <summary>Lets go of whatever was being carried.</summary>
        public void Drop()
        {
            if (_dragging == null)
                return;

            _dragging.Drop();
            _dragging = null;
        }

        /// <summary>Whether something is being carried right now.</summary>
        public bool IsDragging
        {
            get { return _dragging != null; }
        }

        /// <summary>
        /// Acts on whatever is under <paramref name="point" />, and answers whether anything was.
        /// <para>
        /// Hit rectangles are gathered after the layout rather than kept alongside it, because arranging is
        /// the only thing that knows where anything ended up. Anything inside the scrolling region is only
        /// pressable where it is actually on show, so a row scrolled up behind the header cannot be hit
        /// through it.
        /// </para>
        /// </summary>
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
            if (_asking && !_renaming)
            {
                if (!Contains(_popupBounds, point))
                {
                    CancelAsk();
                    return true;
                }

                if (Contains(_confirm.Bounds, point) && !string.IsNullOrEmpty(_askBox.Text))
                    CommitAsk();

                return true;
            }

            // An overlay owns the screen the same way a question does, except that what is inside it is
            // ordinary and can be pressed through the usual list.
            bool overlaid = _overlay != null;
            if (overlaid && !Contains(_overlayBounds, point))
            {
                HideOverlay();
                return true;
            }

            // Last first, so a thing drawn over another is the one that gets pressed.
            for (int i = _hits.Count - 1; i >= 0; i--)
            {
                Hit hit = _hits[i];

                if (!Contains(hit.Bounds, point))
                    continue;

                if (hit.Scrolled && !Contains(_bodyDock.Bounds, point))
                    continue;

                if (overlaid && !Contains(_overlayBounds, Middle(hit.Bounds)))
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

                // Pressing away from a rename keeps what was typed. Enter and clicking elsewhere mean
                // the same thing - the name is what the box says - because there is no confirm to press.
                if (_renaming)
                    CommitAsk();

                _editor.Invoke(hit.Command);

                // Taking a choice and moving on in one gesture, where one thing is being chosen. Where
                // several can be, a second press means un-choosing, and moving on from that would be
                // taking away what somebody had just done.
                if (clicks > 1 && hit.Confirms && _editor.Screen.HasPrimary)
                    _editor.Invoke(_editor.Screen.PrimaryCommand);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sends the answer and closes the question. An empty answer is sent anyway: core ignores one
        /// rather than refusing it, and deciding here that empty means cancel would be this target
        /// inventing a rule core deliberately did not make.
        /// </summary>
        public void CommitAsk()
        {
            if (!_asking)
                return;

            string answer = AskBox.Text;
            int command = _askCommand;

            // Empty means something when renaming - it hands the heading back to whatever the thing says
            // about itself - and means nothing at all when starting one, so only the first accepts it.
            if (!_renaming && string.IsNullOrEmpty(answer))
                return;

            Close();

            // Read at the moment of use. Command ids are re-issued on a rebuild, so one captured when the
            // box opened can be stale by the time the typing has finished.
            if (command != 0)
                _editor.SetText(command, answer);
        }

        /// <summary>Abandons the question. Core was never told it was open, so nothing has to be undone.</summary>
        public void CancelAsk()
        {
            if (!_asking)
                return;

            Close();
        }

        void BeginAsk(Hit hit)
        {
            Close();

            _askCommand = hit.Command;
            _asking = true;

            if (hit.Command == _editor.Screen.RenameCommand)
            {
                // The heading becomes the box, in the heading's own place.
                _renaming = true;
                _titleBox.Text = _editor.Screen.CustomName;
                _titleBox.SelectAll();
                _pencil.Color = Palette.None;

                _titleRow.Children.Clear();
                _titleRow.Children.Add(_titleBox);
                _titleRow.Children.Add(_pencil);
                return;
            }

            // Everything else hangs over the screen, under whatever was pressed.
            _anchor = hit.Anchor;
            _askLabel.Text = hit.Question;
            _askBox.Text = "";
        }

        void Close()
        {
            if (_renaming)
            {
                _renaming = false;
                _titleRow.Children.Clear();
                _titleRow.Children.Add(_title);
                _titleRow.Children.Add(_pencil);
            }

            _anchor = null;
            _asking = false;
            _askCommand = 0;
        }

        void CollectHits()
        {
            _hits.Clear();

            EditorScreen screen = _editor.Screen;

            if (screen.CanGoBack)
                _hits.Add(Hit.Fixed(_back.Bounds, screen.BackCommand));

            // Renaming hangs off the heading, because the heading is what the name is for.
            if (screen.CanRename && !_renaming)
                _hits.Add(Hit.Fixed(_title.Bounds, screen.RenameCommand, screen.RenamePrompt));

            if (screen.HasSwitch)
            {
                Hit toggle = Hit.Switch(_switch.Bounds, screen.SwitchCommand, !screen.SwitchIsOn);
                toggle.Scrolled = false;
                _hits.Add(toggle);
            }

            for (int i = 0; i < _views.Count; i++)
                _views[i].Collect(_hits);

            for (int i = 0; i < _actionHits.Count && i < _actions.Children.Count; i++)
                _hits.Add(Hit.Fixed(_actions.Children[i].Bounds, _actionHits[i].Command));

            if (screen.HasPrimary)
                _hits.Add(Hit.Fixed(_primary.Bounds, screen.PrimaryCommand));
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

    /// <summary>An element that is nothing but a rectangle somebody put somewhere.</summary>
    public sealed class Pin : Element
    {
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        protected override void DrawContent(IGraphics g)
        {
        }
    }

    /// <summary>Somewhere that can be pressed, and what pressing it means.</summary>
    /// <remarks>
    /// A hit carrying a <see cref="Question" /> is not a command to send, it is a command to send an
    /// answer to, and it names who to open the box beneath. Keeping all of that in one structure means the
    /// press path has one shape rather than three.
    /// </remarks>
    public struct Hit
    {
        public RectangleF Bounds;
        public int Command;
        public string Question;

        /// <summary>Whether this sits inside the scrolling region and can therefore be out of sight.</summary>
        public bool Scrolled;

        /// <summary>What the question should hang under. Null when it has no particular home.</summary>
        public Element Anchor;

        /// <summary>
        /// A box this hit puts the keyboard into, rather than a command to send at once.
        /// </summary>
        /// <remarks>
        /// A search box is the case: pressing it starts typing, and every keystroke sends the text to
        /// <see cref="Command" />. Core re-issues command ids on every rebuild, so the id is read at the
        /// moment of use rather than captured when the box was made.
        /// </remarks>
        public TextBox Box;

        /// <summary>Something to hang over the screen when this is pressed, anchored to it.</summary>
        public Element Overlay;

        /// <summary>
        /// Whether this is a switch rather than a command, and what state to put it in.
        /// </summary>
        /// <remarks>
        /// A switch is not a button that happens to look different. Core takes the state it should be in,
        /// so two presses arriving out of order still agree about where it ends up, where two invocations
        /// would each mean "the other one from whatever you have".
        /// </remarks>
        public bool Toggles;

        public bool ToggleTo;

        /// <summary>Whether pressing this a second time means "and move on".</summary>
        public bool Confirms;

        public Hit(RectangleF bounds, int command)
        {
            Bounds = bounds;
            Command = command;
            Question = null;
            Scrolled = true;
            Anchor = null;
            Box = null;
            Overlay = null;
            Toggles = false;
            ToggleTo = false;
            Confirms = false;
        }

        public Hit(RectangleF bounds, int command, string question)
        {
            Bounds = bounds;
            Command = command;
            Question = question;
            Scrolled = true;
            Anchor = null;
            Box = null;
            Overlay = null;
            Toggles = false;
            ToggleTo = false;
            Confirms = false;
        }

        public Hit(RectangleF bounds, int command, string question, Element anchor)
        {
            Bounds = bounds;
            Command = command;
            Question = question;
            Scrolled = true;
            Anchor = anchor;
            Box = null;
            Overlay = null;
            Toggles = false;
            ToggleTo = false;
            Confirms = false;
        }

        /// <summary>A hit that puts a switch into <paramref name="on" />.</summary>
        public static Hit Switch(RectangleF bounds, int command, bool on)
        {
            var hit = new Hit(bounds, command);
            hit.Toggles = true;
            hit.ToggleTo = on;
            return hit;
        }

        /// <summary>A hit that opens <paramref name="list" /> over the screen, under itself.</summary>
        public static Hit Opens(RectangleF bounds, Element list)
        {
            var hit = new Hit(bounds, 0);
            hit.Overlay = list;
            hit.Anchor = null;
            return hit;
        }

        /// <summary>A hit that puts the keyboard into <paramref name="box" />.</summary>
        public static Hit Typing(RectangleF bounds, int command, TextBox box)
        {
            var hit = new Hit(bounds, command);
            hit.Box = box;
            return hit;
        }

        /// <summary>A hit in the chrome, which is never scrolled away.</summary>
        public static Hit Fixed(RectangleF bounds, int command)
        {
            var hit = new Hit(bounds, command);
            hit.Scrolled = false;
            return hit;
        }

        public static Hit Fixed(RectangleF bounds, int command, string question)
        {
            var hit = new Hit(bounds, command, question);
            hit.Scrolled = false;
            return hit;
        }
    }
}
