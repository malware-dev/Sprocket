using System.Collections.Generic;
using System.ComponentModel;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;

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
        readonly SprocketEditor _editor;

        /// <summary>What the editor is editing, for the views that turn ids into words.</summary>
        public GridWorld World
        {
            get { return _editor.World; }
        }

        readonly Dock _root = new Dock();
        readonly Bar _headerBar = new Bar();
        // The chevron alone. It carried the word BACK beside it for a while, which cost most of the room
        // between the left edge and the heading to say what the arrow had already said. What replaces the
        // word is the hover tip, which is where a control that cannot speak for itself is supposed to say
        // what it is - so nothing is actually lost except the space.
        readonly Glyph _back = Widgets.Symbol(Symbols.Left, Palette.Muted);
        readonly HeadingRow _title = new HeadingRow();

        readonly Switch _switch = new Switch(Lettering.Small);

        /// <summary>The header's "look again".</summary>
        readonly Glyph _refresh = Widgets.Symbol(Symbols.Refresh, Palette.Muted);

        // Not chevrons. An arrow that turns back on itself is what undo is; a chevron is where a
        // press goes, and Back already owns that shape in this very row.
        readonly Glyph _undo = Widgets.Symbol(Symbols.Undo, Palette.Muted);
        readonly Glyph _redo = Widgets.Symbol(Symbols.Redo, Palette.Muted);

        /// <summary>What the two arrows were last told to say, and the words they were made from.</summary>
        string _undone;
        string _undoSays;
        string _redone;
        string _redoSays;
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
        readonly Confirm _question = new Confirm();

        /// <summary>What was last pressed, so a question that press raises hangs under it.</summary>
        Element _raisedBy;

        readonly List<ComponentView> _views = new List<ComponentView>();
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

        /// <summary>What is lit right now, so it can be put out without searching for it.</summary>
        Element _lit;

        /// <summary>What the lit thing had to say, if anything.</summary>
        string _saying;

        readonly Label _tip = Widgets.Tip();

        int _actionRevision = -1;

        /// <summary>What an open rename will send its answer to.</summary>
        int _renameCommand;

        /// <summary>The text and command core was last asked about, and whether it said the text was taken.</summary>
        /// <remarks>
        /// Asked every tick while a box is open, so the answer is kept until the text changes. A box hands back
        /// the same string until somebody types, which is what makes comparing references enough.
        /// </remarks>
        string _checkedText;
        int _checkedCommand;
        bool _checkedTaken;
        /// <summary>Which part of the heading is being typed into, or -1 when none is.</summary>
        int _renaming = -1;

        readonly Overlay _overlay = new Overlay();

        TextBox _focus;
        int _focusCommand;
        ComponentView _dragging;

        public EditorView(SprocketEditor editor)
        {
            _editor = editor;

            Attach();

            // Inside a stack that takes the slack, because Fill stretches whatever it is given and a
            // heading lit across the whole header would be a highlight the size of the screen. A
            // horizontal stack hands each child its own width, which is exactly the hug wanted here.
            // Lit on every screen, the root included, where it leaves the editor rather than
            // retreating a page. Not two controls: a chevron means "leave what I am looking at", and on
            // the front door what you are looking at is the editor. That is the rule Escape has always
            // followed here, and nobody has ever found Escape confusing.
            //
            // Set once. Neither of these has ever depended on anything, and writing them on every frame
            // was the same reading habit this whole arrangement is here to break.
            _back.Tint = Palette.Muted;
            _back.Highlight = Palette.Panel;

            // The other glyph in the header, and it answers a press, so it lights like the first one. A
            // control that can be pressed and cannot be lit is the pair of rules in Highlight and Press
            // disagreeing with each other.
            _refresh.Highlight = Palette.Panel;

            var titleLane = Widgets.RowOf(0f);
            titleLane.Children.Add(_title.Root);

            // No slot holding it to a width any more. A switch showing both of its positions is already
            // the width of the longer word twice over whichever way it is set, so the title cannot be
            // shifted sideways by somebody stopping a system.
            // The switch and the refresh sit together on the right, in that order, so the switch stays
            // where it has always been and the refresh reads as furniture rather than as part of it.
            // The way back sits with the rest of the furniture rather than in a row of its own: it
            // is what you do to the screen you are on, like Back and the switch, and a strip holding
            // nothing but two arrows was most of a line spent on them.
            //
            // Left of the switch, and both hold their place on a screen that has a history at all, so
            // the switch cannot be moved sideways by somebody making their first edit.
            var rightLane = Widgets.RowOf(Space.Row);
            rightLane.Children.Add(_undo);
            rightLane.Children.Add(_redo);
            rightLane.Children.Add(_switch);
            rightLane.Children.Add(_refresh);

            _headerBar.Spacing = Space.Row;
            _headerBar.Left = _back;
            _headerBar.Fill = titleLane;
            _headerBar.Right = rightLane;

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

        /// <summary>Whether this is listening to core right now.</summary>
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

        /// <summary>
        /// Whether anything the chrome shows has changed since it was last written.
        /// </summary>
        /// <remarks>
        /// Core's own values announce themselves. What does not is this target's own state - which part
        /// of the heading is being typed into, and whether what is typed can be given - so the places
        /// that change it say so.
        /// </remarks>
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
                if (_renaming >= parts.Count)
                    return "";

                // Says what to do rather than what is wrong. "Needs a name" is a diagnosis and leaves
                // somebody to work out the cure; there is only one cure and it fits in three words.
                string text = AskBox.Text;
                if (parts[_renaming].NameRequired && string.IsNullOrEmpty(text.Trim()))
                    return "Give it a name first";

                return Taken(_renameCommand, text) ? TakenSays : "";
            }
        }

        /// <summary>What a box says while its name belongs to something else.</summary>
        const string TakenSays = "That name is taken";

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

            // Opened and closed from what the screen says, rather than remembered here. Core raises a
            // question and clears it when it is answered, so there is one place that knows.
            if (screen.HasQuestion && !_question.IsUp)
                _question.Open(screen.Question, screen.Answers,
                    screen.QuestionIsHere ? _raisedBy : null);
            else if (!screen.HasQuestion && _question.IsUp)
                _question.Close();

            // A screen that cannot be renamed while the box is open takes the box away with it.
            if (_renaming >= 0 && !screen.CanRename)
                CancelAsk();

            if (!ShowingSameComponents(screen))
                Rebuild(screen);

            for (int i = 0; i < _views.Count; i++)
                _views[i].Tick(_editor.CurrentTick);

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
                _ask.Refused = Taken(_ask.Command, _ask.Answer) ? TakenSays : _ask.Wrong;
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
        /// act: taking what core says and putting it on the controls. It used to sit above the gate,
        /// where it wrote a heading's four values and made a fresh capitalised string on every frame -
        /// and the count said zero, which is the way an instrument placed to flatter always reads.
        /// </remarks>
        void Dress(EditorScreen screen)
        {
            _title.Update(screen.Heading, _renaming);

            // Latched rather than read straight off, so the line does not come and go inside one screen.
            // Cleared in Rebuild, which is where one screen becomes another.
            if (!string.IsNullOrEmpty(screen.Subtitle))
                _hadSubtitle = true;

            _subtitle.Text = screen.Subtitle;
            _subtitle.Collapsed = !_hadSubtitle;

            // Which part wears the pencil is the row's business now, because it is a per-part answer.
            if (_renaming >= 0)
                _title[_renaming].Wrong = Blocked.Length > 0;

            // Governs everything on the screen under it, which is what a switch in the header means.
            if (screen.HasSwitch)
                _switch.Show(screen.SwitchIsOn, true);
            else
                _switch.Hide();

            _primary.Text = screen.HasPrimary ? Widgets.Caps(screen.PrimaryText) : "";

            if (screen.HasPrimary)
                Widgets.Go(_primary);
            else
                Widgets.Absent(_primary);
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
            // One control, two acts: a command while there is somewhere to retreat to, and the door once
            // there is not. Which of the two it is gets said in the note rather than in the picture - a
            // symbol that changed as somebody walked back up the pages would be a control moving under
            // the pointer without moving.
            //
            // It earns a note at all because it has no word beside it any more, which is the case the
            // note exists for exactly. The wording is this target's own: core publishes none for Back,
            // and putting one there is Design decision 11.
            _back.Press = screen.CanGoBack ? Deed.Sends(screen.BackCommand) : Deed.Leaves();
            _back.Tip = screen.CanGoBack ? "Back" : "Close the editor";

            string wrong = Blocked;

            // Renaming hangs off the heading, because the heading is what the name is for - and off the
            // part of it that the name belongs to, which is the whole reason a heading is several things.
            // Not the part being typed into: that box is not a control to be pressed while it is open.
            for (int i = 0; i < _title.Count; i++)
            {
                bool named = i < screen.Heading.Count;

                _title[i].Press = named && screen.Heading[i].CanRename && i != _renaming
                    ? Deed.Asks(screen.Heading[i].RenameCommand, screen.Heading[i].Prompt)
                    : Deed.Nothing;

                // While the name in hand is one that cannot be given, that part says so instead of
                // saying what it usually says. Not a timer and not a thing of its own: the ordinary
                // note, on the ordinary control, carrying different words for exactly as long as they
                // are true. Nothing else on the screen has to know that an error can exist.
                _title[i].Tip = i == _renaming && wrong.Length > 0 ? wrong : null;
            }

            // Built only when those words change. Joining two strings is cheap and this runs sixty
            // times a second, for a note that is read when the pointer happens to rest on an arrow.
            if (!ReferenceEquals(_undone, screen.UndoText))
            {
                _undone = screen.UndoText;
                _undoSays = "Undo " + screen.UndoText;
            }

            if (!ReferenceEquals(_redone, screen.RedoText))
            {
                _redone = screen.RedoText;
                _redoSays = "Redo " + screen.RedoText;
            }

            _undo.Collapsed = !screen.HasHistory;
            _redo.Collapsed = !screen.HasHistory;

            Widgets.Waiting(_undo, screen.UndoCommand, _undoSays);
            Widgets.Waiting(_redo, screen.RedoCommand, _redoSays);

            _refresh.Press = Deed.Sends(screen.RefreshCommand);
            _refresh.Tip = screen.RefreshCommand != 0
                ? "Read the ship again, in case somebody has changed a button"
                : null;

            _switch.Press = screen.HasSwitch
                ? Deed.Switches(screen.SwitchCommand, !screen.SwitchIsOn)
                : Deed.Nothing;

            // A system's, always - core says so where it declares the command, and a sequence's switch
            // lives on the sentence rather than up here. The words say the act and its reach, not what
            // it is on: the heading directly above says that.
            _switch.Tip = screen.HasSwitch ? StatusRow.Switching(true, screen.SwitchIsOn) : null;

            for (int i = 0; i < _actions.Children.Count && i < screen.Actions.Count; i++)
                _actions.Children[i].Press = Deed.Sends(screen.Actions[i].Command);

            // Asking rather than sending where the screen cannot finish without a value: the field
            // opens under this button, which is the one somebody just pressed to finish.
            _primary.Press = !screen.HasPrimary
                ? Deed.Nothing
                : screen.PrimaryAsks
                    ? Deed.Values(screen.PrimaryCommand, screen.PrimaryQuestion,
                        screen.PrimaryFields)
                    : Deed.Sends(screen.PrimaryCommand);
        }

        /// <summary>How wide a question's popup is, whatever it is hanging under.</summary>
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

            _saying = wanted != null ? wanted.Tip : null;

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
                return _title[_renaming].Under(point);

            Element owner = Owner;

            return owner != null ? owner.Under(point) : _root.Under(point);
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
            _overlay.Draw(g, bounds);
        }

        /// <summary>
        /// Hangs something over the screen, under whatever was pressed to open it.
        /// </summary>
        /// <remarks>
        /// This screen's own business, not anything a view has to ask for. A control that opens a list
        /// says so on itself - see <see cref="Deed.Opens" /> - and the press path does the rest, which
        /// is why nothing here is on <see cref="IScreenHost" /> any more.
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
        /// narrowed when you stopped typing would feel broken. The box is asked for its own text rather
        /// than the text being captured, so what is sent is always what is in it.
        /// </remarks>
        public void Typed()
        {
            // What is typed decides whether the name in hand can be given, which is what the heading
            // says while somebody is typing it. Core knows nothing about the box, so nothing else
            // will say this has changed.
            _chromeStale = true;

            if (_focus != null && _focusCommand != 0)
                _editor.SetText(_focusCommand, _focus.Text);
        }

        /// <summary>Puts the caret in a box, and says where what is typed into it goes.</summary>
        void Typing(TextBox box, int command)
        {
            _focus = box;
            _focusCommand = command;
            _focus.IsEditing = true;
            _focus.SelectAll();
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
        public bool Grab(Vector2 point)
        {
            // A question owns the screen, so it is asked first rather than not at all. What it may
            // have to drag is a slider on a colour, and only inside its own bounds - a drag started
            // outside one is a press that dismisses it, which is the press path's business.
            if (_ask.IsUp && _renaming < 0)
                return _ask.Covers(point) && _ask.Grab(point);

            // Nothing is carried out from under a popup, and nothing at all while a name is being
            // typed. The same owner a press respects, asked the same way.
            if (Owner != null || IsAsking)
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
            if (_scroll.Parent == null || !_scroll.OnBar(point))
                return false;

            if (!_scroll.OnThumb(point))
                _scroll.ThumbTo(point.Y);

            _scroll.BarHeld = true;
            _held = point.Y;
            return true;
        }

        /// <summary>Where the thing being carried has got to. Ignored when nothing is being carried.</summary>
        public void Carry(Vector2 point)
        {
            if (_ask.IsDragging)
            {
                _ask.Carry(point);
                return;
            }

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
            _ask.Drop();
            _scroll.BarHeld = false;

            if (_dragging == null)
                return;

            _dragging.Drop();
            _dragging = null;
        }

        /// <summary>Whether something is being carried right now.</summary>
        public bool IsDragging
        {
            get { return _dragging != null || _scroll.BarHeld || _ask.IsDragging; }
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
                return _title[_renaming].Reaches(point);

            // An overlay owns the screen the same way a question does, except that what is inside it is
            // ordinary and is pressed the ordinary way.
            if (_overlay.IsUp && !_overlay.Covers(point))
            {
                HideOverlay();
                return true;
            }

            Element target = Under(point);
            if (target == null)
                return PressedNothing(point);

            Deed deed = target.Press;

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
                Typing(deed.Box, deed.Command);
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
                SetNumber(deed.Command, (int)(deed.Asked ? Held : deed.Held));
                return true;
            }

            if (deed.Means == Means.Ask)
            {
                BeginAsk(target);
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
            if (_renaming >= 0 && _title[_renaming].Reaches(point))
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

            _ask.Next();
        }

        public void CommitAsk()
        {
            if (!IsAsking)
                return;

            // The whole answer, not the field the caret happens to be in. A form of three fields
            // sent one of them, which is how three values became one - and it looked like the other
            // two had been thrown away, because they had.
            string answer = _renaming >= 0 ? AskBox.Text : _ask.Answer;

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
                && (string.IsNullOrEmpty(answer) || Taken(command, answer) || _ask.Wrong.Length > 0))
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

        /// <summary>Whether a question with answers is waiting, which owns Escape.</summary>
        public bool IsQuestioning
        {
            get { return _question.IsUp; }
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

            // Whatever was open goes first, because closing is what puts a name being typed away -
            // and it closes the form as well, so opening before it would open and shut in one press.
            Close();

            _raisedBy = target;
            _ask.Values(screen.PrimaryQuestion, screen.PrimaryCommand, screen.PrimaryFields);
            _chromeStale = true;
        }

        void BeginAsk(Element control)
        {
            Deed deed = control.Press;
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
                _renaming = i;
                _renameCommand = deed.Command;
                _title[i].Text = parts[i].Name;
                _title[i].Begin();
                return;
            }

            // Everything else hangs over the screen, under whatever was pressed.
            if (deed.IsForm)
                _ask.Values(deed.Question, deed.Command, deed.Fields, deed.Filled);
            else
                _ask.Open(deed.Question, control, deed.Command);
        }

        void Close()
        {
            _chromeStale = true;

            if (_renaming >= 0)
            {
                _title[_renaming].End();
                _title[_renaming].Wrong = false;
                _renaming = -1;
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
            _hadSubtitle = false;

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

            for (int i = 0; i < screen.Actions.Count; i++)
            {
                ChoiceEntry entry = screen.Actions[i];

                Label action = Widgets.Ghost(Widgets.Caps(entry.Text));
                if (entry.IsCurrent)
                    action.Color = Palette.Ink;

                _actions.Children.Add(action);
            }
        }
    }
}
