using Phosphor;
using Reactor.Editing;
using Reactor.Screens;
using Reactor.Ui;
using Sandbox.ModAPI;
using VRage.ModAPI;
using IMyInput = VRage.ModAPI.IMyInput;
using VRageMath;

namespace Reactor.Host.SpaceEngineers
{
    /// <summary>
    /// Reactor's editor, as a screen the game owns.
    /// <para>
    /// Three things meet here and none of them knows about the other two: core decides what is on screen,
    /// <see cref="EditorView" /> decides how this target draws it, and the game supplies a world and a
    /// clock. The simulator's <c>EditorScene</c> is the same three with a made-up ship in the third place,
    /// which is why the two can be kept honest against each other.
    /// </para>
    /// <para>
    /// Everything the old overlay needed in order to survive being drawn over live gameplay is gone. The
    /// screen owns its input, so nothing blacklists controls, nothing fights the camera, and the pointer is
    /// simply a pointer.
    /// </para>
    /// </summary>
    public sealed class ReactorScreen : MyModScreen
    {
        /// <summary>Frames within which a second press counts as a double, and how far it may wander.</summary>
        const int DoubleClickFrames = 20;
        const float DoubleClickSlack = 12f;

        /// <summary>UI units per notch of the wheel, matching what the simulator's window uses.</summary>
        const float ScrollStep = 60f;

        /// <summary>Frames the caret spends shown, then hidden.</summary>
        const int BlinkTicks = 30;

        readonly ReactorUnit _unit;
        readonly ReactorEditor _editor;
        readonly EditorView _view;
        readonly ScreenSpaceGraphics _graphics;
        readonly ScreenKeyboard _keyboard = new ScreenKeyboard();

        Vector2 _pointer;
        bool _grabbed;
        bool _wasDown;
        int _lastClickFrame = int.MinValue;
        Vector2 _lastClickAt;

        /// <summary>When the caret's blink was last restarted, so typing never hides it mid-blink.</summary>
        int _blinkFrom;

        public ReactorScreen(ReactorUnit unit)
            : base("Reactor", null, new Vector2(EditorView.ScreenWidth, EditorView.ScreenHeight))
        {
            _unit = unit;
            _editor = unit.Editor;
            _view = new EditorView(_editor);

            // BelowHud because that is where a mod screen's own content goes; the demo mod draws its screen
            // on the same layer. The scissor is pushed onto whatever layer this names, so the two must agree
            // or clipping silently does nothing.
            _graphics = new ScreenSpaceGraphics(MyScreenSpaceLayer.BelowHud);

            // Off, and the view draws its own cross instead. The harness lays its controls down before it
            // calls the mod - MyGuiScreenModHost.Draw is base.Draw then OnDraw - so the first opaque thing
            // this screen paints buries the button while leaving it live. An invisible way out is bad; an
            // invisible way out that still works is a corner that shuts the editor for no visible reason.
            CloseButtonEnabled = false;
            CloseOnEscape = true;
            EnabledBackgroundFade = true;

            // The game draws the cursor, so the mod does not. The old MousePointer integrated mouse movement
            // because during play the game holds the mouse still and reports only deltas; a screen has a real
            // cursor and hands it over.
            DrawMouseCursor = true;

            // Nothing here multiplies its colours by the transition alpha yet, so a fade would draw the
            // screen fully opaque part way through one. Skipping it is honest until the backend carries a
            // global alpha.
            SkipTransition = true;
        }

        protected override void OnOpened()
        {
            // A piston or rotor may have attached since this was last looked at, which changes which grids
            // are part of the construct without adding a block to any of them.
            _unit.World.Invalidate();

            _grabbed = false;
            _wasDown = false;

            _keyboard.Clear();
            _blinkFrom = Now();
        }

        /// <summary>
        /// Escape, and the cross in the header, both arrive here.
        /// </summary>
        /// <remarks>
        /// A question open on the screen owns Escape: dismissing it is what somebody pressing Escape meant,
        /// and closing the whole editor out from under them is not. Refusing the close is how that is said,
        /// and it is the reason Escape is not read as a key anywhere else - acting on it in two places would
        /// cancel the question and shut the screen in the same breath.
        /// <para>
        /// There used to be a guard at the top of this that threw away any close arriving on an Enter or a
        /// Space release. It was needed because the harness's close button is a real focusable control that
        /// takes focus on the screen's first update and fires on a key being released - so every Enter that
        /// finished a rename, and every space typed into a search box, arrived here as a request to leave.
        /// Switching the button off took the cause away, and the guard with it.
        /// </para>
        /// </remarks>
        protected override bool OnCancelling()
        {
            if (_view.IsAsking)
            {
                _view.CancelAsk();
                return false;
            }

            // Escape is the key everybody already reads as "back", and docs/Editor.md is firm that going
            // back always means going back. So it retreats one page at a time and only shuts the editor
            // once there is nowhere left to retreat to. Closing the whole thing from three pages deep is
            // never what somebody backing out of a picker meant.
            EditorScreen screen = _editor.Screen;
            if (screen.CanGoBack)
            {
                _editor.Invoke(screen.BackCommand);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Keeps the caret blinking. The unit does the ticking, open or not.
        /// </summary>
        /// <remarks>
        /// The blink belongs to whoever owns a clock, and here that is the game's frame counter. Measured
        /// from the last keystroke rather than from zero, so the caret is solid the moment somebody types.
        /// </remarks>
        protected override void OnUpdate(bool hasFocus)
        {
            _view.CaretVisible = (Now() - _blinkFrom) / BlinkTicks % 2 == 0;
        }

        /// <summary>
        /// Writes the program back to the block on the way out.
        /// </summary>
        /// <remarks>
        /// Closing the editor is the moment somebody has finished saying something, so it is the cheapest
        /// honest place to put the program where a world save will find it.
        /// </remarks>
        protected override void OnClosed(bool isUnloading)
        {
            _unit.Persist();
        }

        protected override void OnHandleInput()
        {
            IMyInput input = MyAPIGateway.Input;
            if (input == null)
                return;

            Vector2 pixels;
            if (MyScreenSpace.TryGetCursorPositionPixels(out pixels))
                _pointer = _graphics.FromPixels(pixels);

            _view.Pointer = _pointer;

            int wheel = input.DeltaMouseScrollWheelValue();
            if (wheel != 0)
            {
                // A notch up is a positive delta and scrolls the content up, which is a negative amount.
                // The wheel is routed by where the pointer is rather than to the screen's own scroller,
                // because a picker's columns are not inside it.
                _view.ScrollBy(_pointer, -wheel / 120f * ScrollStep);
            }

            bool down = input.IsLeftMousePressed();

            if (input.IsNewLeftMousePressed())
            {
                // Offered as a grab first, so that dragging a step out of a sequence is not also a click on
                // whatever it was sitting on.
                _grabbed = _view.Grab(_pointer);
            }
            else if (down)
            {
                if (_grabbed)
                    _view.Carry(_pointer);
            }
            else if (_wasDown)
            {
                if (_grabbed)
                    _view.Drop();
                else
                    _view.Press(_pointer, Clicks());

                _grabbed = false;

                // Straight out, and deliberately not down the road Escape takes. The cross is the quick
                // exit: it does not retreat a page, it does not put a question away and wait to be pressed
                // again, it leaves. Somebody four screens deep who wants to be gone has asked to be gone.
                //
                // What that abandons is a question or a rename in progress, and abandoning those costs
                // nothing: neither is sent to core until it is committed, so there is no half-finished
                // edit to strand. The program itself is written out on the way through OnClosed.
                if (_view.TakeClose())
                    Close();
            }

            _wasDown = down;

            Type();
        }

        /// <summary>
        /// Sends the keyboard wherever the screen says it belongs.
        /// </summary>
        /// <remarks>
        /// A question owns the keyboard while it is open; otherwise it goes to whatever was focused, which
        /// is how a search box narrows a list as somebody types. Asked of the view rather than worked out
        /// here, because the simulator has to arrive at the same answer and two hosts guessing separately is
        /// how they drift.
        /// </remarks>
        void Type()
        {
            TextBox box = _view.IsAsking ? _view.AskBox : _view.Focus;
            if (box == null)
                return;

            KeyboardOutcome outcome = _keyboard.HandleInput(box);

            if (outcome.Commit && _view.IsAsking)
            {
                _view.CommitAsk();
                _blinkFrom = Now();
                return;
            }

            if (!outcome.Changed)
                return;

            // Every keystroke, not just the last. A search that only narrowed once somebody stopped typing
            // would feel broken.
            _view.Typed();
            _blinkFrom = Now();
        }

        static int Now()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
        }

        protected override void OnDraw(float transitionAlpha)
        {
            _graphics.Begin();
            _view.Update();
            _view.Draw(_graphics, Bounds());
        }

        /// <summary>
        /// Where the screen actually is, in Phosphor's units.
        /// </summary>
        /// <remarks>
        /// <c>Position</c> is the centre of the panel in the game's normalized GUI coordinates and
        /// <c>Size</c> is normalized too, while everything Phosphor lays out is in UI units against the safe
        /// area. Read every frame rather than cached because a resolution or UI-scale change moves it, and
        /// <c>OnLayoutChanged</c> is a notification we would then have to remember to honour.
        /// </remarks>
        RectangleF Bounds()
        {
            Vector2 size = Size ?? new Vector2(EditorView.ScreenWidth, EditorView.ScreenHeight);
            Vector2 topLeft = Position - size * 0.5f;

            Vector2 origin = _graphics.FromPixels(MyScreenSpace.NormalizedToPixels(topLeft));
            Vector2 extent = _graphics.FromPixelSize(MyScreenSpace.NormalizedSizeToPixels(size));

            return new RectangleF(origin.X, origin.Y, extent.X, extent.Y);
        }

        /// <summary>
        /// One click, or two when the last one was recent and near enough.
        /// </summary>
        /// <remarks>
        /// Counted here rather than asked of the game, which reports double clicks only for its own
        /// controls. Slack as well as time, because a double click that moved half the screen was two
        /// separate presses that happened to be quick.
        /// </remarks>
        int Clicks()
        {
            int now = MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;

            bool doubled = now - _lastClickFrame <= DoubleClickFrames
                && Vector2.Distance(_lastClickAt, _pointer) <= DoubleClickSlack;

            _lastClickFrame = doubled ? int.MinValue : now;
            _lastClickAt = _pointer;

            return doubled ? 2 : 1;
        }
    }
}
