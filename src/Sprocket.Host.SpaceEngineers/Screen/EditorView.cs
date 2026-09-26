using System.Collections.Generic;
using System.ComponentModel;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using TextBox = Sprocket.Host.SpaceEngineers.Ui.Legacy.TextBox;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Rich;
using Sprocket.Host.SpaceEngineers.Ui.Windows;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Renders a <see cref="SprocketEditor" /> on the Space Engineers screen.
    /// </summary>
    /// <remarks>
    /// The whole of this target's editor. It decides nothing about what the editor does: core states
    /// what is on screen and this decides how it looks, which is the split <c>Conformance</c> exists to
    /// police.
    /// <para>
    /// <b>Nothing already on screen is allowed to move.</b> Controls that come and go keep their place
    /// whether or not they are showing, the heading turns into its own edit box rather than summoning one,
    /// and a question opens directly beneath whatever was pressed so that everything above the press stays
    /// exactly where the eye left it. A screen that rearranges itself in use costs its reader that place
    /// every time they act, and undoes the one thing a layout is for.
    /// </para>
    /// <para>
    /// Shared source rather than a library, because the mod compiles loose files and cannot load one. The
    /// simulator compiles the same files, so what is seen there is what ships.
    /// </para>
    /// </remarks>
    public sealed class EditorView : IScreenHost
    {
        readonly SprocketEditor _editor;

        /// <summary>What the editor is editing, for the views that turn ids into words.</summary>
        public GridWorld World
        {
            get { return _editor.World; }
        }

        /// <inheritdoc />
        public Texts Texts => _editor.Texts;

        readonly VStack _root;
        readonly Glyph _back;
        readonly HeadingRow _title;
        readonly Switch _switch;

        readonly Glyph _refresh;

        readonly Glyph _undo;
        readonly Glyph _redo;

        readonly Relay _undoes;
        readonly Relay _redoes;
        readonly Relay _refreshes;
        readonly Relay _flips;
        readonly Relay _finishes;

        readonly Arguments _undone = new Arguments();
        readonly Arguments _redone = new Arguments();
        readonly Label _subtitle;

        readonly Grid _page;
        readonly ScrollView _scroll;

        bool _wantsClose;
        readonly VStack _body;
        readonly VStack _above;

        readonly Banner _banner = new Banner { Padding = new Thickness(0f, 0f, Sizes.Bar, 0f) };
        readonly VStack _below;

        readonly HStack _actions;
        readonly Button _primary;

        readonly Ask _ask = new Ask();
        readonly Confirm _question = new Confirm();

        readonly WindowLayer _windows = new WindowLayer();

        readonly MessageBox _dialog;

        Element _raisedBy;

        readonly List<ComponentView> _views = new List<ComponentView>();

        EditorPage _shownPage;

        readonly List<EditorComponent> _shown = new List<EditorComponent>();
        readonly Dictionary<EditorComponent, ComponentView> _byComponent =
            new Dictionary<EditorComponent, ComponentView>();

        /// <summary>
        /// How many views are listening, which is one per component on screen and no more.
        /// </summary>
        /// <remarks>
        /// For a test that a view lets go of what it stops showing. <see cref="Observable" /> counts
        /// the other end of the same subscriptions, which is where one left behind piles up.
        /// </remarks>
        internal int Watching
        {
            get
            {
                int watching = 0;

                foreach (KeyValuePair<EditorComponent, ComponentView> pair in _byComponent)
                {
                    if (pair.Value.Watching)
                        watching++;
                }

                return watching;
            }
        }

        /// <summary>
        /// How many times the chrome and every view on it have written what their controls mean.
        /// </summary>
        /// <remarks>
        /// For a test to hold still. See <see cref="ComponentView.Wires" /> for why it is counted at
        /// all rather than reasoned about.
        /// </remarks>
        internal int Wires
        {
            get
            {
                int wires = _wires;

                for (int i = 0; i < _views.Count; i++)
                    wires += _views[i].Wires;

                return wires;
            }
        }

        int _wires;

        Element _lit;

        Document _saying;

        Arguments _sayingArguments;

        readonly RichText _tip = new RichText { Style = Styles.Tip };

        int _actionRevision = -1;

        int _renameCommand;

        string _checkedText;
        int _checkedCommand;
        bool _checkedTaken;
        int _renaming = -1;

        readonly Overlay _overlay = new Overlay();

        int _asked;

        bool _movingOn;

        TextBox _focus;
        int _focusCommand;
        int _focusTakes;

        string _focusTook = "";
        ComponentView _dragging;

        readonly InputRoot _input;

        HeadingPart _namedArrival;

        bool _pressing;

        Vector2 _pressedAt;

        public EditorView(SprocketEditor editor)
        {
            _editor = editor;
            _title = new HeadingRow(this, new Relay(CommitAsk), new Relay(CancelAsk), Retyped);

            _undoes = new Relay(() => Send(_undo, _editor.Screen.UndoCommand, null));
            _redoes = new Relay(() => Send(_redo, _editor.Screen.RedoCommand, null));
            _refreshes = new Relay(() => Send(_refresh, _editor.Screen.RefreshCommand, null));
            _flips = new Relay(() => Switch(_switch, _editor.Screen.SwitchCommand, !_editor.Screen.SwitchIsOn));
            _finishes = new Relay(() => Act(_primary, Finishing(_editor.Screen), 1));

            _scroll = new ScrollView { Style = Styles.ScrollView, Content = new VStack().As(out _body) };

            _root = new VStack
            {
                Background = Palette.Page,
                Children =
                {
                    new VStack
                    {
                        Background = Palette.Chrome,
                        Padding = new Thickness(16f, 8f),
                        Spacing = 4f,
                        Children =
                        {
                            new HStack
                            {
                                Spacing = 8f,
                                Children =
                                {
                                    new Glyph { Symbol = Symbols.Left, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f), Command = new Relay(Back) }.As(out _back),
                                    _title,
                                    new Glyph { Symbol = Symbols.Undo, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f), Command = _undoes }.As(out _undo),
                                    new Glyph { Symbol = Symbols.Redo, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f), Command = _redoes }.As(out _redo),
                                    new Switch { Style = Styles.Switch, Command = _flips }.As(out _switch),
                                    new Glyph { Symbol = Symbols.Refresh, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f), Command = _refreshes }.As(out _refresh)
                                }
                            },
                            new Label { Style = Styles.Detail }.As(out _subtitle)
                        }
                    },
                    new Rule(1f, Palette.Line),
                    new Grid
                    {
                        Padding = new Thickness(16f),
                        Placement = Flex.Grow,
                        Columns = { Length.Flex(1f) },
                        Rows = { Length.Auto, Length.Flex(1f), Length.Auto },
                        Cells =
                        {
                            [0, 0] = new VStack { Children = { _banner, new VStack().As(out _above) } },
                            [2, 0] = new VStack().As(out _below)
                        }
                    }.As(out _page),
                    new Rule(1f, Palette.Line),
                    new HStack
                    {
                        Background = Palette.Chrome,
                        Padding = new Thickness(16f, 8f),
                        Spacing = 16f,
                        Children =
                        {
                            new HStack { Spacing = 8f, Placement = Flex.Grow }.As(out _actions),
                            new Button { Style = Styles.Primary, Command = _finishes }.As(out _primary)
                        }
                    }
                }
            };

            _dialog = new MessageBox
            {
                Style = Styles.Dialog,
                Placement = Fixed.Wide(Sizes.Popup),
                MessageStyle = Styles.Question,
                ButtonStyle = Styles.Button,
                DefaultStyle = Styles.Primary,
                Cancel = new Relay(() => _editor.Unask()),
                ShowsCancel = false
            };

            _input = new InputRoot(_root);
            _input.FocusChanged += Refocused;

            Attach();
        }

        /// <summary>
        /// Starts listening to core, so this is told when what it shows changes.
        /// </summary>
        /// <remarks>
        /// Paired with <see cref="Release" />, and called again whenever a host shows this after
        /// putting it away: <b>closed is not destroyed.</b> A session keeps one screen per Sprocket and
        /// opens the same one again, so a view that let go and never took hold again is a screen
        /// showing whatever it happened to be showing the first time.
        /// <para>
        /// The screen core keeps is one object for the life of the editor, rewritten as somebody walks
        /// about, so it outlives this and has to be let go of rather than merely forgotten.
        /// </para>
        /// </remarks>
        public void Attach()
        {
            if (_listening)
                return;

            _listening = true;
            _editor.Screen.PropertyChanged += Said;
            _editor.Screen.Actions.Changed += Listed;

            // Nothing has been told to this since it last looked, whatever it may have been told
            // while it was not listening.
            _chromeStale = true;
        }

        /// <summary>
        /// Stops listening to core, and lets go of every view with it.
        /// </summary>
        /// <remarks>
        /// For a host putting a screen away. The views go entirely rather than merely falling silent:
        /// one kept in hand after it has let go of its component is a view nothing will ever tell
        /// anything again, and <see cref="Attach" /> has no way to find it. Dropping them means the
        /// next frame builds them afresh and takes hold properly.
        /// </remarks>
        public void Release()
        {
            if (!_listening)
                return;

            _listening = false;
            _editor.Screen.PropertyChanged -= Said;
            _editor.Screen.Actions.Changed -= Listed;

            foreach (KeyValuePair<EditorComponent, ComponentView> pair in _byComponent)
                pair.Value.Release();

            _byComponent.Clear();
            _views.Clear();
            _shown.Clear();
        }

        bool _listening;

        void Said(object sender, PropertyChangedEventArgs e)
        {
            // A flag only. The event arrives while core is still writing its screen.
            _chromeStale = true;
        }

        void Listed()
        {
            _chromeStale = true;
        }

        bool _chromeStale = true;

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

        /// <summary>The fields a view shows as part of the screen, or null where none does.</summary>
        Ask Given
        {
            get
            {
                for (int i = 0; i < _views.Count; i++)
                {
                    Ask given = _views[i].Given;
                    if (given != null)
                        return given;
                }

                return null;
            }
        }

        /// <summary>The way to finish that a view on the screen carries itself, or null where the footer's is the one.</summary>
        Label Carried
        {
            get
            {
                for (int i = 0; i < _views.Count; i++)
                {
                    Label done = _views[i].Done;
                    if (done != null)
                        return done;
                }

                return null;
            }
        }

        /// <summary>Whether a view on the screen gives values in a column of its own, shown or not yet.</summary>
        bool Gives
        {
            get
            {
                for (int i = 0; i < _views.Count; i++)
                {
                    if (_views[i].Gives)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Whether one of the fields shown as part of the screen has the keyboard.</summary>
        public bool IsFilling
        {
            get
            {
                Ask given = Given;
                return given != null && given.Typing;
            }
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
        /// Escape is deliberately not gated. Something has to lead out of a modal state or it is a trap,
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
                if (_renaming >= parts.Count)
                    return "";

                // Says what to do rather than what is wrong. "Needs a name" is a diagnosis and leaves
                // somebody to work out the cure; there is only one cure and it fits in three words.
                string text = _title.Box(_renaming).Text;
                if (parts[_renaming].NameRequired && string.IsNullOrEmpty(text.Trim()))
                    return Texts.RenameEmpty;

                return Taken(_renameCommand, text) ? Texts.RenameTaken : "";
            }
        }

        /// <summary>Whether core would refuse this text for this command, asked once per change of text.</summary>
        bool Taken(int command, string text)
        {
            if (command != _checkedCommand || !ReferenceEquals(text, _checkedText))
            {
                _checkedCommand = command;
                _checkedText = text;
                _checkedTaken = !string.IsNullOrEmpty(text) && !_editor.Accepts(command, text);
            }

            return _checkedTaken;
        }

        /// <summary>
        /// What is being held down, as what it means rather than as which key it is.
        /// </summary>
        /// <remarks>
        /// Set by whoever reads the input and read when a press lands, rather than travelling with
        /// the press. A press arrives here from the game's screen, from the simulator and from every
        /// script in the reference suite, and threading a parameter through all of them for the one
        /// control that cares would be churn that buys nothing.
        /// </remarks>
        public Held Held { get; set; }

        /// <summary>The box a question is being answered in. Only meaningful while <see cref="IsAsking" />.</summary>
        public TextBox AskBox
        {
            get { return _ask.Box; }
        }

        /// <summary>Whether the caret is drawn this frame. The blink belongs to whoever owns a clock.</summary>
        public bool CaretVisible
        {
            set
            {
                _ask.Box.CaretVisible = value;

                if (_focus != null)
                    _focus.CaretVisible = value;

                Ask given = Given;
                if (given != null && given.Typing)
                    given.Box.CaretVisible = value;

                var box = _input.Focused as Ui.Text.TextBox;
                if (box != null)
                    box.CaretVisible = value;
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

            // Opened and closed from what the screen says, rather than remembered here. Core raises a
            // question and clears it when it is answered, so there is one place that knows.
            // Opened again where a different question has been raised, and not only where none was
            // up. One question answered by raising another kept the first one's words and the first
            // one's answers, which is a lie the moment any two follow each other.
            if (screen.HasQuestion && !screen.CanGoBack)
            {
                if (!_windows.IsOpen(_dialog) || _asked != screen.Asked)
                {
                    _asked = screen.Asked;
                    Ask(screen);
                }
            }
            else if (screen.HasQuestion && (!_question.IsUp || _asked != screen.Asked))
            {
                _asked = screen.Asked;
                _question.Open(screen.Question, screen.Answers,
                    screen.QuestionIsHere ? _raisedBy : null, screen.Default);
            }

            if (!screen.HasQuestion && _question.IsUp)
                _question.Close();

            if (!screen.HasQuestion && _windows.IsOpen(_dialog))
                _windows.Close(_dialog);

            // A screen that cannot be renamed while the box is open takes the box away with it.
            if (_renaming >= 0 && !screen.CanRename)
                CancelAsk();

            if (!ShowingSameComponents(screen) || _editor.Current != _shownPage)
                Rebuild(screen);

            for (int i = 0; i < _views.Count; i++)
                _views[i].Tick(_editor.CurrentTick);

            _banner.Show(_editor.Notice, _editor.CurrentTick);

            // After the views, so the column is showing what is chosen now.
            DressGiven(screen);

            if (_actionRevision != screen.Actions.Revision)
            {
                _actionRevision = screen.Actions.Revision;
                RebuildActions(screen);
            }

            // The question hanging over the screen. What it shows depends on what has been typed into
            // it, which core knows nothing about, so it is dressed here rather than announced.
            if (_ask.IsUp && _renaming < 0)
            {
                // A name something else already has first, because that is the reason this view can
                // see and the form cannot. What a field will take is the form's own, and it says so.
                _ask.Refused = Taken(_ask.Command, _ask.Answer) ? Texts.RenameTaken : _ask.Wrong;
                _ask.Dress(Pointer);
            }

            if (!_chromeStale)
                return;

            _chromeStale = false;
            _wires++;
            Dress(screen);
            Wire(screen);
        }

        /// <summary>
        /// Puts this tick's words and states on the chrome.
        /// </summary>
        /// <remarks>
        /// Behind the same gate as <see cref="Wire" />, and counted with it, because both are the same
        /// act: taking what core says and putting it on the controls. Above the gate it would write a
        /// heading's four values and make a fresh capitalised string on every frame, where the idle
        /// count is placed to flatter and reads zero.
        /// </remarks>
        void Dress(EditorScreen screen)
        {
            _title.Update(screen.Heading, _renaming);
            NameArrival(screen);

            // Only the words. Whether there is room for them was settled when this screen went up - see
            // Rebuild - because changing it now would move the page under whoever is reading it.
            _subtitle.Text = screen.Subtitle;

            // While the name in hand is one that cannot be given, the box says so, in its look and in its note.
            if (_renaming >= 0)
            {
                string wrong = Blocked;
                _title.Refuse(_renaming, wrong.Length > 0 ? wrong : null);
            }

            _switch.IsVisible = screen.HasSwitch;
            _switch.IsOn = screen.SwitchIsOn;
            _switch.Says(Texts.SwitchOn, Texts.SwitchOff);

            // Where the screen is finished in a column of its own, that column's Done is the one.
            Label carried = Carried;
            bool here = screen.HasPrimary && Given == null && carried == null;
            _primary.Text = here ? Widgets.Caps(screen.PrimaryText) : "";
            _finishes.CanExecute = here;

            if (carried == null)
                return;

            // Its own words while there is somewhere to go on to, which is Next where a page follows.
            if (screen.HasPrimary)
            {
                carried.Text = screen.PrimaryText;
                Widgets.Go(carried);
            }
            else
                Widgets.Waiting(carried);
        }

        /// <summary>
        /// Tells each control in the chrome what pressing it does.
        /// </summary>
        /// <remarks>
        /// Whenever what the chrome shows has changed. An id never moves - <c>SprocketEditor.Register</c>
        /// says why - but which command a control carries does: the back arrow carries none on the front
        /// page, a refresh carries none where there is nothing to read again, and a control handed
        /// nothing is a control that does not answer the pointer either.
        /// </remarks>
        void Wire(EditorScreen screen)
        {
            _back.Tip = Tip.For(_back.Tip, screen.CanGoBack ? Texts.ChromeBack : Texts.ChromeClose);

            // A comparison each when nothing has changed, and this runs sixty times a second.
            _undone.Set("action", screen.UndoText);
            _redone.Set("action", screen.RedoText);

            _undo.IsVisible = screen.HasHistory;
            _redo.IsVisible = screen.HasHistory;

            _undoes.CanExecute = screen.UndoCommand != 0;
            _undo.Tip = Tip.For(_undo.Tip, screen.UndoCommand != 0 ? Texts.ChromeUndo : null, _undone);
            _redoes.CanExecute = screen.RedoCommand != 0;
            _redo.Tip = Tip.For(_redo.Tip, screen.RedoCommand != 0 ? Texts.ChromeRedo : null, _redone);

            _refreshes.CanExecute = screen.RefreshCommand != 0;
            _refresh.Tip = Tip.For(_refresh.Tip, screen.RefreshCommand != 0
                ? Texts.ChromeRefresh
                : null);

            _flips.CanExecute = screen.HasSwitch;

            // A system's, always - core says so where it declares the command, and a sequence's switch
            // lives on the sentence rather than up here. The words say the act and its reach, not what
            // it is on: the heading directly above says that.
            _switch.Tip = Tip.For(_switch.Tip, screen.HasSwitch ? StatusRow.Switching(Texts, true, screen.SwitchIsOn) : null);

            Label carried = Carried;
            if (carried != null)
                carried.Press = Finishing(screen);
        }

        /// <summary>What finishing <paramref name="screen" /> does: asking, where it cannot finish without a value, and sending otherwise.</summary>
        static Deed Finishing(EditorScreen screen)
        {
            if (!screen.HasPrimary)
                return Deed.Nothing;

            return screen.PrimaryAsks
                ? Deed.Values(screen.PrimaryCommand, screen.PrimaryQuestion, screen.PrimaryFields, screen.PrimaryFilled)
                : Deed.Sends(screen.PrimaryCommand);
        }

        /// <summary>Retreats a page, or leaves the editor from the front page.</summary>
        void Back()
        {
            EditorScreen screen = _editor.Screen;
            Act(_back, screen.CanGoBack ? Deed.Sends(screen.BackCommand) : Deed.Leaves(), 1);
        }

        const float PopupWidth = Sizes.Popup;

        /// <summary>
        /// How big one of the game's own screens is, as a share of the safe area.
        /// </summary>
        /// <remarks>
        /// Read out of the game rather than guessed at. Space Engineers sizes a GUI screen in normalized
        /// units where 1.0 is the safe GUI rectangle exactly - <c>MyGuiManager</c> multiplies by
        /// <c>m_safeGuiRectangle</c> and nothing else - and its terminal, which is the screen Sprocket is
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
        /// either side and exists because the game's screen background has soft edges to hide; Sprocket's has
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
            Ask given = Given;
            if (given != null)
                given.Settle(g, bounds.Width);

            _root.Measure(g, new Vector2(bounds.Width, bounds.Height));
            _root.Arrange(bounds);

            LightWhatIsUnderThePointer();

            _root.Draw(g);

            if (given != null)
                given.DrawHanging(g);

            DrawPopup(g, bounds);
            _windows.Draw(g, bounds);
            DrawOverlay(g, bounds);
            DrawTip(g, bounds);
        }

        /// <summary>
        /// Where the pointer is, in UI units. Somewhere outside the screen means there is not one.
        /// </summary>
        /// <remarks>
        /// A property the host sets rather than an argument to <see cref="Draw(IGraphics)" />, because a target
        /// without a pointer - a text panel, a screenshot, a test - should not have to pass one. What it
        /// leaves alone stays where it was, which is off the screen and therefore over nothing.
        /// </remarks>
        public Vector2 Pointer { get; set; }

        /// <summary>Where the new controls on the screen's body take the pointer and the keyboard.</summary>
        public InputRoot Input => _input;

        /// <summary>
        /// Lights the one thing the pointer is resting on, and puts out whatever was lit before.
        /// </summary>
        /// <remarks>
        /// Only one thing is ever lit, and it is found the same way a press finds what it acts on, so the
        /// thing that would be pressed is the thing that lights - a row under a popup cannot light up
        /// through it, and a control that lights can always be pressed.
        /// </remarks>
        void LightWhatIsUnderThePointer()
        {
            Element wanted = Under(Pointer);

            var speaker = SayingAt(Pointer);
            var tip = speaker == null ? null : speaker.Tip;
            _saying = tip == null ? null : tip.Document;
            _sayingArguments = tip == null ? null : tip.Arguments;

            if (wanted == _lit)
                return;

            if (_lit != null)
                _lit.IsHot = false;

            _lit = wanted;

            if (_lit != null)
                _lit.IsHot = true;
        }

        /// <summary>
        /// What a press or a hover at <paramref name="point" /> reaches, or null for nothing at all.
        /// </summary>
        /// <remarks>
        /// Four trees, in the order they are drawn over one another: the question with answers, the
        /// question with a box, then whatever a control has hung over the screen, then the screen
        /// itself. Each of the first three owns the screen while it is up, so nothing behind it is
        /// asked - which is the whole of a rule that was once spread across the press path and the
        /// highlight and had to be kept in step by hand.
        /// <para>
        /// A rename is not one of them. It happens in place, on a screen that is otherwise ordinary,
        /// and it is the one state where something on that screen has a reason to say what is wrong
        /// with it.
        /// </para>
        /// <para>
        /// The question popup and the overlay stand outside the layout: they are measured and placed as
        /// they are drawn, against an anchor whose position is only settled then. So they are walked
        /// from where they last landed, which is the same frame of lag the highlight has always had.
        /// </para>
        /// </remarks>
        internal Element Under(Vector2 point)
        {
            // An unfinishable edit owns the screen as firmly as a popup does: the press path answers
            // nothing but the box being typed into, so nothing else may light up either.
            if (Blocked.Length > 0)
                return _title.Plate(_renaming).Under(point);

            Element owner = Owner;

            return owner != null ? owner.Under(point) : _root.Under(point);
        }

        /// <summary>What the pointer at <paramref name="point" /> rests on that has something to say, or null, looked for in whatever owns the screen.</summary>
        internal Element SayingAt(Vector2 point)
        {
            if (Blocked.Length > 0)
                return _title.Plate(_renaming).SayingAt(point);

            Element owner = Owner;

            return owner != null ? owner.SayingAt(point) : _root.SayingAt(point);
        }

        /// <summary>
        /// What owns the screen right now, or null when the screen owns itself.
        /// </summary>
        /// <remarks>
        /// One answer, for everything that has to respect it: what the pointer reaches, what a press
        /// acts on, and whether a gesture may take hold of anything at all. Three separate lists of
        /// owners is what let a question be dragged through - the drag path had two of the three, and
        /// nothing said the lists were the same list.
        /// <para>
        /// In the order they are drawn over one another. A rename is not here: it happens in place, on
        /// a screen that is otherwise ordinary.
        /// </para>
        /// </remarks>
        Element Owner
        {
            get
            {
                Window modal = _windows.Modal;
                if (modal != null)
                    return modal;

                if (_question.IsUp)
                    return _question.Root;

                if (_ask.IsUp && _renaming < 0)
                    return _ask.Root;

                return _overlay.IsUp ? _overlay.Content : null;
            }
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
            if (_saying == null)
                return;

            _tip.Document = _saying;
            _tip.Arguments = _sayingArguments;
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
            _overlay.Draw(g, bounds);
        }

        /// <summary>
        /// Hangs something over the screen, under whatever was pressed to open it.
        /// </summary>
        /// <remarks>
        /// This screen's own business, not anything a view has to ask for. A control that opens a list
        /// says so on itself - see <see cref="Deed.Opens(Element)" /> - and the press path does the
        /// rest, which is why nothing here is on <see cref="IScreenHost" /> any more.
        /// </remarks>
        void ShowOverlay(Element content, Element anchor)
        {
            _overlay.Show(content, anchor);
        }

        void HideOverlay()
        {
            _overlay.Hide();
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

        public void SetText(int command, string text)
        {
            if (command != 0)
                _editor.SetText(command, text);
        }

        /// <summary>Lets the view showing the values column tell core what they hold now.</summary>
        void Entered()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i].Given != null)
                    _views[i].Entered();
            }
        }

        /// <summary>The box the keyboard is in, or null when nothing is being typed into.</summary>
        public TextBox Focus
        {
            get { return IsFilling ? Given.Box : _focus; }
        }

        /// <inheritdoc />
        /// <remarks>Only a box pressing would type into; any other is left as it is.</remarks>
        public void Take(TextBox box)
        {
            Deed deed = box.Press;
            if (deed.Means != Means.Type)
                return;

            Unfocus();
            Typing(box, deed.Command, deed.Takes);
        }

        /// <summary>
        /// Sends what has just been typed wherever it belongs.
        /// </summary>
        /// <remarks>
        /// Asked for afresh on every keystroke rather than on finishing, because a search that only
        /// narrowed when the typing stopped would feel broken. The box is asked for its own text rather
        /// than the text being captured, so what is sent is always what is in it.
        /// </remarks>
        public void Typed()
        {
            if (IsFilling)
                Entered();

            // What is typed decides whether the name in hand can be given, which is what the heading
            // says while somebody is typing it. Core knows nothing about the box, so nothing else
            // will say this has changed.
            _chromeStale = true;

            if (_focus == null || _focusCommand == 0)
                return;

            // A box whose command refuses what was typed goes back to what it last took, so what it
            // shows is always what Enter would take.
            if (!_editor.Accepts(_focusCommand, _focus.Text))
            {
                _focus.Text = _focusTook;
                return;
            }

            _focusTook = _focus.Text;
            _editor.SetText(_focusCommand, _focus.Text);
        }

        /// <summary>
        /// Whether the field with the keyboard is answered by choosing one of its words, so letters and
        /// arrows choose and nothing is typed into it.
        /// </summary>
        public bool Chooses
        {
            get { return IsFilling && Given.Chooses; }
        }

        /// <summary>
        /// Takes an arrow key for the field with the keyboard, where that field is stepped rather than
        /// typed through, and answers whether it did.
        /// </summary>
        /// <param name="across">One for right, minus one for left.</param>
        /// <param name="down">One for down, minus one for up.</param>
        /// <remarks>
        /// Words are stepped by any arrow, onward being right or down. A dial is turned by up and down
        /// only, and more by up, because left and right are what move the caret in the number it is
        /// typed as.
        /// </remarks>
        public bool Arrow(int across, int down)
        {
            if (!IsFilling)
                return false;

            Ask given = Given;
            var by = given.Chooses ? across + down : given.Turns ? -down : 0;
            if (by == 0)
                return false;

            if (given.Step(by))
                Entered();

            return true;
        }

        /// <summary>Takes a letter for a field answered by choosing, and answers whether it did.</summary>
        public bool Letter(char letter)
        {
            if (!Chooses)
                return false;

            if (Given.Letter(letter))
                Entered();

            return true;
        }

        /// <summary>Takes what is typed in the box with the caret, where Enter in it takes anything.</summary>
        public bool TakeTyped()
        {
            // Enter in a field of the values column is Done.
            if (IsFilling)
            {
                FinishGiven();
                return true;
            }

            if (_focus == null || _focusTakes == 0)
                return false;

            var takes = _focusTakes;
            Unfocus();
            _editor.Invoke(takes);
            return true;
        }

        /// <summary>Puts the caret in a box, and says where what is typed into it goes.</summary>
        void Typing(TextBox box, int command)
        {
            Typing(box, command, 0);
        }

        /// <summary>The same, and what Enter in it takes.</summary>
        void Typing(TextBox box, int command, int takes)
        {
            _input.Focus(null);
            _focus = box;
            _focusCommand = command;
            _focusTakes = takes;
            _focusTook = box.Text;
            _focus.IsEditing = true;
            _focus.SelectAll();
        }

        /// <summary>Puts down whatever was being typed into, a field of the values column as well.</summary>
        void Unfocus()
        {
            Ask given = Given;
            if (given != null)
                given.Release();

            UnfocusBox();
        }

        void UnfocusBox()
        {
            if (_focus == null)
                return;

            _focus.IsEditing = false;
            _focus = null;
            _focusCommand = 0;
            _focusTakes = 0;
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
            _question.Draw(g, bounds, PopupWidth);
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
            // Nothing behind a popup is moved, for the same reason nothing behind one is pressed. A
            // wheel that reached through took the rows out from under a pointer that was on the list
            // hanging over them, and left somebody somewhere else when it closed.
            if (Owner != null || IsAsking)
                return;

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
        public bool Grab(Vector2 point, int presses = 1)
        {
            // A question owns the screen, so it is asked first rather than not at all. What it may
            // have to drag is a slider on a colour, and only inside its own bounds - a drag started
            // outside one is a press that dismisses it, which is the press path's business.
            if (_ask.IsUp && _renaming < 0)
                return _ask.Covers(point) && _ask.Grab(point);

            // A new control takes the whole press, down, moves and release, through the input root: the box a
            // name is typed in, a dialog's answers, and any other control, which moving the keyboard off that box
            // gives the name. Found the way a press is, so whatever owns the screen is asked first.
            var target = Under(point);
            if (target != null && target.TakesInput)
            {
                UnfocusBox();
                _input.PointerDown(target, point, presses);
                _pressing = true;
                _pressedAt = point;
                return true;
            }

            // Nothing else is carried out from under a popup. The same owner a press respects, asked the same way.
            if (Owner != null)
                return false;

            // Nothing else is carried while a name is being typed.
            if (IsAsking)
                return false;

            _input.Focus(null);


            // A dial or a colour in the values column, which is turned or mixed by dragging.
            Ask given = Given;
            if (given != null && given.Covers(point) && given.Grab(point))
            {
                UnfocusBox();
                return true;
            }

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
            if (_pressing)
            {
                _pressedAt = point;
                _input.PointerMove(point);
                return;
            }

            if (_ask.IsDragging)
            {
                _ask.Carry(point);
                return;
            }

            // Told to core when it is let go rather than at every step of the drag.
            Ask given = Given;
            if (given != null && given.IsDragging)
            {
                given.Carry(point);
                return;
            }


            if (_dragging != null)
                _dragging.Carry(point);
        }

        /// <summary>Lets go of whatever was being carried.</summary>
        public void Drop()
        {
            if (_pressing)
            {
                _pressing = false;
                _input.PointerUp(_pressedAt);
                return;
            }

            _ask.Drop();

            Ask given = Given;
            if (given != null)
            {
                given.Drop();
                Entered();
            }

            if (_dragging == null)
                return;

            _dragging.Drop();
            _dragging = null;
        }

        /// <summary>Whether something is being carried right now.</summary>
        public bool IsDragging
        {
            get
            {
                Ask given = Given;
                return _dragging != null || _pressing || _ask.IsDragging
                    || given != null && given.IsDragging;
            }
        }

        /// <summary>
        /// Acts on whatever is under <paramref name="point" />, and answers whether anything was.
        /// </summary>
        /// <remarks>
        /// What a press reaches is settled by <see cref="Under" />, walking the elements themselves.
        /// What happens then is settled here, by what the element says pressing it means.
        /// </remarks>
        public bool Press(Vector2 point)
        {
            return Press(point, 1);
        }

        /// <summary>
        /// Presses at a point.
        /// </summary>
        /// <remarks>
        /// <paramref name="clicks" /> is how many presses in quick succession this is. A second one on
        /// a row that says it confirms means "this one, and move on" - taking the choice and going in
        /// one gesture.
        /// </remarks>
        public bool Press(Vector2 point, int clicks)
        {
            // A question with answers owns the screen absolutely: nothing outside it responds, and
            // pressing away does not dismiss it. Every answer leads somewhere, so there is nothing to
            // escape from, and a question that can be shrugged off is one that gets shrugged off.
            // A dialog owns the screen: a press on its answers is taken through the input root, and a press
            // anywhere else does nothing at all.
            if (_windows.Modal != null)
                return true;

            if (_question.IsUp && !_question.Covers(point))
            {
                _editor.Unask();
                return true;
            }

            // While a typed question is open it owns the screen. Pressing inside it is the only thing
            // that can reach anything, and pressing anywhere else puts it away and goes no further - the
            // press that dismisses is not also a press on whatever happened to be underneath.
            if (_ask.IsUp && _renaming < 0)
            {
                if (!_ask.Covers(point))
                {
                    CancelAsk();
                    return true;
                }

                // A press on one of the fields moves the caret to it, so several values can be
                // filled in any order.
                if (_ask.Touch(point))
                    return true;

                // Two controls in there end it: one sends what has been typed and one abandons it.
                // Everything else inside is a box, which already has the keyboard.
                Element inside = Under(point);
                if (inside != null)
                {
                    if (inside.Press.Means == Means.Commit)
                        CommitAsk();
                    else if (inside.Press.Means == Means.Cancel)
                        CancelAsk();
                }

                return true;
            }

            // An unfinishable edit owns the screen too, and more firmly: nothing outside the box it is in
            // will answer at all. Letting the odd control through was the first shape of this and it does
            // not work - every one of them either navigates away from the box or ends up committing it,
            // and a rule with a list of exceptions is a rule nobody can predict.
            //
            // Blunt, and the screen is already saying why in the box itself. Escape is the way out.
            if (Blocked.Length > 0)
                return _title.Plate(_renaming).Reaches(point);

            // An overlay owns the screen the same way a question does, except that what is inside it is
            // ordinary and is pressed the ordinary way.
            if (_overlay.IsUp && !_overlay.Covers(point))
            {
                HideOverlay();
                return true;
            }

            if (PressedGiven(point))
                return true;

            Element target = Under(point);
            if (target == null)
                return PressedNothing(point);

            return Act(target, target.Press, clicks);
        }

        /// <inheritdoc />
        public void Send(Element from, int command, string question)
        {
            if (command != 0)
                Act(from, Deed.Asks(command, question), 1);
        }

        /// <inheritdoc />
        public void Switch(Element from, int command, bool on)
        {
            if (command != 0)
                Act(from, Deed.Switches(command, on), 1);
        }

        /// <summary>Carries out <paramref name="deed" /> for <paramref name="target" />, pressed <paramref name="clicks" /> times in quick succession.</summary>
        bool Act(Element target, Deed deed, int clicks)
        {
            if (deed.Means == Means.Open)
            {
                Unfocus();

                // Hung under whatever was pressed, which is why nothing has to be told where to put it.
                // Pressing that line a second time is a press outside the list - it hangs below its
                // anchor, never over it - so putting it away is already settled above.
                ShowOverlay(deed.Overlay, target);

                // Opened ready to be typed into, where what opened it said so. The command is the
                // box's own, so what happens next is exactly what pressing it would have done.
                if (deed.Typing != null && deed.Typing.Press.Means == Means.Type)
                    Typing(deed.Typing, deed.Typing.Press.Command);

                return true;
            }

            if (deed.Means == Means.Empty)
            {
                // Emptied and reported in one go, and the caret left where it was. The box may well
                // be the one being typed into, so what is showing has to be put right as well as
                // what core holds.
                if (deed.Box != null)
                    deed.Box.Text = "";

                _editor.SetText(deed.Command, "");
                return true;
            }

            if (deed.Means == Means.Type)
            {
                Unfocus();
                Typing(deed.Box, deed.Command, deed.Takes);
                return true;
            }

            // Taking something from a list puts the list away. Not every press inside one does: a box
            // that narrows the list is pressed to type into, and closing the list under the pointer
            // would be answering a question nobody asked. So it is the answering presses and no
            // others - which is also why this sits after the one above rather than before it.
            if (_overlay.IsUp)
                HideOverlay();

            Unfocus();

            // Before anything the press might do, and not only before an ordinary command. Pressing a
            // switch, or the way out, or a control that asks its own question, is pressing away from the
            // rename just as much as pressing a row is - and most of what follows returns without
            // reaching the end of this method, so anything placed there would be skipped and the typing
            // thrown away in silence.
            if (_renaming >= 0)
                CommitAsk();

            if (deed.Means == Means.Leave)
            {
                _wantsClose = true;
                return true;
            }

            if (deed.Means == Means.Switch)
            {
                SetToggle(deed.Command, deed.ToggleTo);
                return true;
            }

            // Which way it was taken, answered as a number because that is the channel core already
            // has for "a control with a value". The deed says whether to ask what is held down or to
            // mean one thing regardless, which is what the tick at the head of a row does.
            if (deed.Means == Means.Pick)
            {
                Held held = deed.Asked ? Held : deed.Held;
                SetNumber(deed.Command, (int)held);

                // A plain press takes this one and nothing else, so pressing it twice is taking one
                // thing and moving on - the same gesture as in a list where only one can be chosen.
                // With something held, or on the tick, a second press is building a set, and moving
                // on would cut that short.
                if (clicks > 1 && deed.Asked && held == Held.Alone && _editor.Screen.HasPrimary)
                    Moving(target);

                return true;
            }

            if (deed.Means == Means.Ask)
            {
                BeginAsk(target, deed);
                return true;
            }

            // The answer that means "finish this page" goes where the button goes, because that is
            // what it means. Only where finishing needs a question of its own: everywhere else the
            // answer's own action finishes, and sending it is the shorter way to the same place.
            if (deed.Command != 0 && deed.Command == _editor.Screen.Finishing
                && _editor.Screen.PrimaryAsks)
            {
                _editor.Unask();
                Finish(target);
                return true;
            }

            // What a question raised by this press will hang under. Held rather than worked out
            // afterwards, because by then the press is over and nothing remembers what it was on -
            // and a question about a row belongs under that row rather than over the middle of the
            // screen saying which row it was about.
            _raisedBy = target;
            _editor.Invoke(deed.Command);

            // Taking a choice and moving on in one gesture, where one thing is being chosen. Where
            // several can be, a second press means un-choosing, and moving on from that would be
            // taking away what somebody had just done.
            //
            // Moving on can mean asking rather than finishing - an action that will not do anything
            // until it is given values. Then the question opens under the row that was double
            // pressed, because that is where the gesture happened and the button was never touched.
            // Sending the command instead would reach a command that takes text with none to give
            // it, which is nothing happening at all.
            if (clicks > 1 && deed.Confirms && _editor.Screen.HasPrimary)
                Moving(target);

            return true;
        }

        /// <summary>
        /// Puts down whatever was being typed into, and answers whether the press meant anything.
        /// </summary>
        /// <remarks>
        /// Nothing under the pointer is still an act rather than a miss. A caret left blinking in a
        /// search box goes on taking every key typed after it, wherever somebody thought they were
        /// typing; and a heading left standing as a box is one nobody asked to go on editing. Pressing
        /// away from either is how they are put down, and it is the only way to put the first down at
        /// all - there is nothing else to press that means "stop typing".
        /// <para>
        /// A press inside the box being typed into is not away. The plate holding it is not a control
        /// while it is open, so without this the press would fall through here and commit the very edit
        /// it was aiming into.
        /// </para>
        /// <para>
        /// Everywhere else, pressing away from a rename keeps what was typed. Enter and pressing
        /// elsewhere mean the same thing - the name is whatever the box says - because there is no
        /// confirm button to press and never was.
        /// </para>
        /// </remarks>
        bool PressedNothing(Vector2 point)
        {
            if (_renaming >= 0 && _title.Plate(_renaming).Reaches(point))
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
        /// <summary>
        /// Moves the caret to the next field of the form, and round to the first after the last.
        /// </summary>
        /// <remarks>
        /// So a form can be filled without the mouse. Nothing to do while a name is being typed:
        /// there is one field and going onward from it means finishing, which Enter already does.
        /// </remarks>
        public void NextField()
        {
            if (_renaming >= 0)
                return;

            if (!_ask.IsUp && IsFilling)
            {
                Given.Next();
                return;
            }

            _ask.Next();
        }

        public void CommitAsk()
        {
            if (!IsAsking)
                return;

            // The whole answer, not the field the caret happens to be in. A form of three fields
            // sent one of them, which is how three values became one - and it looked like the other
            // two had been thrown away, because they had.
            string answer = _renaming >= 0 ? _title.Box(_renaming).Text : _ask.Answer;

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
            //
            // A name something else already has is refused the same way, in both kinds of box, because the
            // box is already saying so.
            if (Blocked.Length > 0)
                return;

            if (_renaming < 0
                && (string.IsNullOrEmpty(answer) || Taken(command, answer) || _ask.Wrong.Length > 0
                    || _ask.Missing))
                return;

            Close();

            if (command != 0)
                _editor.SetText(command, answer);
        }

        /// <summary>Abandons the question. Core was never told it was open, so nothing has to be undone.</summary>
        public void CancelAsk()
        {
            // A field of the values column is put down, and everything in it stays.
            if (!IsAsking)
            {
                Unfocus();
                return;
            }

            Close();
        }

        /// <summary>Whether a question with answers is waiting, which owns Escape.</summary>
        public bool IsQuestioning
        {
            get { return _question.IsUp || _windows.IsOpen(_dialog); }
        }

        /// <summary>
        /// Takes the answer the page said Enter takes, and says whether there was one.
        /// </summary>
        /// <remarks>
        /// Nothing at all where the page named none. A key that guesses an answer is worse than a
        /// key that does nothing, because the guess is only noticed after it has been acted on.
        /// </remarks>
        public bool TakeDefault()
        {
            if (_windows.IsOpen(_dialog))
                return _dialog.TakeAccept();

            return _question.IsUp && Default();
        }

        /// <summary>Gives the answer the page named for Enter, and says whether it named one.</summary>
        bool Default()
        {
            EditorScreen screen = _editor.Screen;
            if (screen.Default == 0)
                return false;

            // The same detour a press on it takes. A key that finished differently from the row it
            // points at would be two answers wearing one set of words.
            if (screen.Default == screen.Finishing && screen.PrimaryAsks)
            {
                _editor.Unask();
                Finish(null);
                return true;
            }

            _editor.Invoke(screen.Default);
            return true;
        }

        /// <summary>
        /// Dismisses the standing question without answering it.
        /// </summary>
        /// <remarks>
        /// Closing is the whole of it, and that is not a gap. Nothing has happened yet when a
        /// question is up: a page that asks one has not moved, and its answers are what move it. So
        /// dismissing already means "I did not mean to leave" wherever the question is about
        /// leaving, without the page having to say so.
        /// </remarks>
        public void Dismiss()
        {
            if (_windows.IsOpen(_dialog))
            {
                _dialog.TakeCancel();
                return;
            }

            if (!_question.IsUp)
                return;

            _editor.Unask();
        }

        /// <summary>Puts <paramref name="screen" />'s question in the dialog, its first answer as Yes and its second as No, and opens it.</summary>
        /// <remarks>Escape puts it away without an answer, which has no button of its own.</remarks>
        void Ask(EditorScreen screen)
        {
            _dialog.Message = Markdown.Parse(screen.Question);

            var yes = screen.Answers.Count > 0 ? screen.Answers[0] : null;
            var no = screen.Answers.Count > 1 ? screen.Answers[1] : null;

            _dialog.YesText = yes != null ? yes.Text : "";
            _dialog.YesCommand = Answering(yes);
            _dialog.NoText = no != null ? no.Text : "";
            _dialog.NoCommand = Answering(no);

            _dialog.Default = yes != null && yes.Command == screen.Default ? MessageButton.Yes
                : no != null && no.Command == screen.Default ? MessageButton.No
                : (MessageButton?)null;

            _windows.Open(_dialog);
        }

        /// <summary>The command that gives <paramref name="answer" />, the way pressing it would, or null for none.</summary>
        ICommand Answering(ChoiceEntry answer)
        {
            if (answer == null)
                return null;

            int command = answer.Command;
            return new Relay(() => Send(_dialog, command, null));
        }

        /// <summary>
        /// Finishes a screen, or opens the question it cannot finish without.
        /// </summary>
        /// <remarks>
        /// The screen decides which, so a double press and the button it stands in for behave the
        /// same way. What differs is where a question hangs: under the row somebody pressed twice,
        /// which is what they were looking at.
        /// </remarks>
        void Moving(Element target)
        {
            EditorScreen screen = _editor.Screen;
            if (!screen.PrimaryAsks)
            {
                _editor.Invoke(screen.PrimaryCommand);
                return;
            }

            // Where the values are a column, a double press goes to them rather than raising them - once
            // the column has caught up with what the press chose, which may be what puts it there.
            if (Gives)
            {
                _movingOn = true;
                return;
            }

            // Whatever was open goes first, because closing is what puts a name being typed away -
            // and it closes the form as well, so opening before it would open and shut in one press.
            Close();

            _raisedBy = target;
            _ask.Values(screen.PrimaryQuestion, screen.PrimaryCommand, screen.PrimaryFields,
                screen.PrimaryFilled);
            _chromeStale = true;
        }

        /// <summary>
        /// Finishes a screen the way its Done does: through the values column where there is one.
        /// </summary>
        void Finish(Element target)
        {
            if (Given != null)
            {
                FinishGiven();
                return;
            }

            Moving(target);
        }

        /// <summary>
        /// Takes a press on the values column, and answers whether it was one.
        /// </summary>
        /// <remarks>
        /// A field takes the keyboard, and Done tries to finish. Anywhere else in the column is still
        /// the column, and goes no further.
        /// </remarks>
        bool PressedGiven(Vector2 point)
        {
            Ask given = Given;
            if (given == null || !given.Covers(point))
                return false;

            UnfocusBox();

            if (_renaming >= 0)
                CommitAsk();

            if (given.Touch(point))
            {
                Entered();
                return true;
            }

            Element inside = Under(point);
            if (inside != null && inside.Press.Means == Means.Commit)
                FinishGiven();

            return true;
        }

        /// <summary>
        /// Finishes with what is in the values column, or says what is wrong with it.
        /// </summary>
        /// <remarks>
        /// Every field is held to what it takes from here on, so the reason shows at the foot and the
        /// field it is about is marked, and the caret goes to the first field that needs mending. What
        /// is chosen taking nothing is finished as it is.
        /// </remarks>
        void FinishGiven()
        {
            EditorScreen screen = _editor.Screen;
            Ask given = Given;
            if (given == null || !screen.HasPrimary)
                return;

            if (!screen.PrimaryAsks)
            {
                given.Release();
                _editor.Invoke(screen.PrimaryCommand);
                return;
            }

            given.Judge();
            if (given.Wrong.Length > 0 || given.Missing)
            {
                UnfocusBox();
                given.Take();
                return;
            }

            given.Release();
            _editor.SetText(screen.PrimaryCommand, given.Answer);
        }

        /// <summary>
        /// Puts this tick's state on the values column, and carries out a double press waiting on it.
        /// </summary>
        /// <remarks>
        /// A double press finishes where there is nothing left to fill in - a step whose values are
        /// already there - and otherwise takes the caret to the first field that needs something.
        /// </remarks>
        void DressGiven(EditorScreen screen)
        {
            Ask given = Given;
            bool movingOn = _movingOn;
            _movingOn = false;

            if (given == null)
                return;

            if (movingOn)
            {
                if (given.Complete)
                {
                    FinishGiven();
                    return;
                }

                UnfocusBox();
                given.Take();
            }

            given.Finishable = screen.HasPrimary;
            given.Refused = given.Wrong;
            given.Dress(Pointer);
        }

        void BeginAsk(Element control, Deed deed)
        {
            Close();
            _chromeStale = true;

            // Which part of the heading this was, if it was one at all. Matched on the command rather
            // than on the element, because the command is the one thing that already identifies a part
            // and a second way of saying it is a second way of being wrong.
            List<HeadingPart> parts = _editor.Screen.Heading;
            for (int i = 0; i < parts.Count && i < _title.Count; i++)
            {
                if (parts[i].RenameCommand != deed.Command || deed.Command == 0)
                    continue;

                // The words become the box, in the words' own place. What that takes is the plate's
                // business, not this one's.
                Rename(i, deed.Command, parts[i].Name);
                return;
            }

            // Everything else hangs over the screen, under whatever was pressed.
            if (deed.IsForm)
                _ask.Values(deed.Question, deed.Command, deed.Fields, deed.Filled);
            else
                _ask.Open(deed.Question, control, deed.Command);
        }

        /// <summary>Begins renaming the part of the heading that arrives to be named, once for that part.</summary>
        void NameArrival(EditorScreen screen)
        {
            if (_renaming >= 0)
                return;

            for (int i = 0; i < screen.Heading.Count && i < _title.Count; i++)
            {
                HeadingPart part = screen.Heading[i];
                if (!part.NamedOnArrival || !part.CanRename || part == _namedArrival)
                    continue;

                _namedArrival = part;
                Rename(i, part.RenameCommand, part.Name);
                return;
            }
        }

        /// <summary>Opens the box of the heading's part at <paramref name="index" /> on <paramref name="name" />, to be sent to <paramref name="command" />.</summary>
        void Rename(int index, int command, string name)
        {
            _renaming = index;
            _renameCommand = command;
            _title.Begin(index, name);
            _input.Focus(_title.Box(index));
        }

        /// <summary>Says the name in hand changed, which decides whether it can be given.</summary>
        void Retyped()
        {
            _chromeStale = true;
        }

        /// <summary>
        /// Takes the keyboard leaving a name being typed as the press away it is, which gives the name.
        /// </summary>
        /// <remarks>A name that cannot be given keeps the keyboard, because the box is already saying why.</remarks>
        void Refocused()
        {
            if (_renaming < 0)
                return;

            var box = _title.Box(_renaming);
            if (_input.Focused == box)
                return;

            CommitAsk();

            if (_renaming >= 0)
                _input.Focus(box);
        }

        void Close()
        {
            _chromeStale = true;

            if (_renaming >= 0)
            {
                var renamed = _renaming;
                _renaming = -1;
                _title.End(renamed);

                if (_input.Focused == _title.Box(renamed))
                    _input.Focus(null);
            }


            _renameCommand = 0;
            _ask.Close();
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
            _chromeStale = true;
            Close();
            HideOverlay();
            Unfocus();

            _views.Clear();
            _shown.Clear();
            _shownPage = _editor.Current;

            // Settled here, once, for the life of this screen: a page either says something under its
            // heading or it never does, and the header holds whichever shape that is.
            _subtitle.IsVisible = !string.IsNullOrEmpty(screen.Subtitle);

            for (int i = 0; i < screen.Components.Count; i++)
            {
                EditorComponent component = screen.Components[i];

                ComponentView view;
                if (!_byComponent.TryGetValue(component, out view))
                {
                    view = ComponentView.For(component);
                    view.Host = this;
                    view.Watch(component);
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
                {
                    // Let go before letting fall. The component is core's and outlives this, so a view
                    // dropped while still listening would be kept alive by the thing it was showing.
                    _byComponent[stale[i]].Release();
                    _byComponent.Remove(stale[i]);
                }
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

            // A page that draws itself is its own body, and a page described in components is built of them.
            var drawn = _shownPage.View;

            if (filler < 0)
            {
                for (int i = 0; i < _views.Count; i++)
                    _body.Children.Add(_views[i].Root);

                if (drawn != null)
                    _body.Children.Add(drawn);

                _page.Cells[1, 0] = _scroll;
                return;
            }

            for (int i = 0; i < filler; i++)
                _above.Children.Add(_views[i].Root);

            for (int i = filler + 1; i < _views.Count; i++)
                _below.Children.Add(_views[i].Root);

            // A component that scrolls itself is handed the space directly. Wrapping it would put a
            // second scroller around the first, and the two would fight over one gesture.
            if (_views[filler].ScrollsItself)
            {
                _page.Cells[1, 0] = _views[filler].Root;
            }
            else
            {
                _body.Children.Add(_views[filler].Root);
                _page.Cells[1, 0] = _scroll;
            }
        }

        void RebuildActions(EditorScreen screen)
        {
            _actions.Children.Clear();

            for (int i = 0; i < screen.Actions.Count; i++)
            {
                ChoiceEntry entry = screen.Actions[i];
                int at = i;

                _actions.Children.Add(new Button
                {
                    Style = entry.IsCurrent ? Styles.Current : Styles.Plain,
                    Text = Widgets.Caps(entry.Text),
                    Command = new Relay(() => Take(at))
                });
            }
        }

        /// <summary>Carries out the footer's action at <paramref name="index" />.</summary>
        void Take(int index)
        {
            Send(_actions.Children[index], _editor.Screen.Actions[index].Command, null);
        }
    }
}
