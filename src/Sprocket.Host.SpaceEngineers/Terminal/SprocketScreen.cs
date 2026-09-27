using System;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Input;
using IMyInput = VRage.ModAPI.IMyInput;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Display;
using VRage.Game.GUI.TextPanel;
using IngameSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IngameSurfaces = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Terminal
{
    /// <summary>
    /// Sprocket's editor, as a screen the game owns.
    /// </summary>
    /// <remarks>
    /// Three things meet here and none of them knows about the other two: core decides what is on screen,
    /// <see cref="EditorView" /> decides how this target draws it, and the game supplies a world and a
    /// clock. The simulator's <c>EditorScene</c> is the same three with a made-up ship in the third place,
    /// which is why the two can be kept honest against each other.
    /// <para>
    /// The screen owns its input, so nothing blacklists controls, nothing fights the camera, and the
    /// pointer is simply a pointer. None of the work a thing drawn over live gameplay would need is here.
    /// </para>
    /// </remarks>
    public sealed class SprocketScreen : MyModScreen
    {
        const int DoubleClickFrames = 20;
        const float DoubleClickSlack = 12f;

        const float ScrollStep = 60f;

        const int BlinkTicks = 30;

        readonly MyKeys[] _editing =
        {
            MyKeys.Back, MyKeys.Delete, MyKeys.Left, MyKeys.Right, MyKeys.Home, MyKeys.End, MyKeys.Up, MyKeys.Down
        };

        readonly MyKeys[] _shortcuts = { MyKeys.A, MyKeys.C, MyKeys.X, MyKeys.V };

        readonly SprocketUnit _unit;
        readonly EditorView _view;
        readonly IScreenSpace _screen;
        readonly ScreenSpaceGraphics _graphics;

        Vector2 _pointer;
        bool _wasDown;
        int _lastDownFrame = int.MinValue;

        Vector2 _lastDownAt;
        int _presses;

        readonly KeyRepeater _repeat = new KeyRepeater();

        int _blinkFrom;

        readonly Action _closed;

        /// <summary>The editor on <paramref name="unit" />, telling <paramref name="closed" /> each time it closes.</summary>
        public SprocketScreen(SprocketUnit unit, Action closed)
            : base("Sprocket", null, new Vector2(EditorView.ScreenWidth, EditorView.ScreenHeight))
        {
            _unit = unit;
            _closed = closed;
            _view = new EditorView(unit.Editor);

            // BelowHud because that is where a mod screen's own content goes; the demo mod draws its screen
            // on the same layer. The scissor is pushed onto whatever layer this names, so the two must agree
            // or clipping silently does nothing.
            _screen = new GameScreenSpace();
            _graphics = new ScreenSpaceGraphics(_screen, MyScreenSpaceLayer.BelowHud, Materials.Fill);

            // Off, and the view draws its own cross instead. The harness lays its controls down before it
            // calls the mod - MyGuiScreenModHost.Draw is base.Draw then OnDraw - so the first opaque thing
            // this screen paints buries the button while leaving it live. An invisible way out is bad; an
            // invisible way out that still works is a corner that shuts the editor for no visible reason.
            CloseButtonEnabled = false;
            CloseOnEscape = true;
            EnabledBackgroundFade = true;

            // The game draws the cursor, so the mod does not. Integrating mouse deltas is what something
            // drawn during play has to do, because the game holds the mouse still and reports only
            // movement; a screen has a real cursor and hands it over.
            DrawMouseCursor = true;

            // Nothing here multiplies its colours by the transition alpha yet, so a fade would draw the
            // screen fully opaque part way through one. Skipping it is honest until the backend carries a
            // global alpha.
            SkipTransition = true;
        }

        protected override void OnOpened()
        {
            _unit.Showing = true;
            Hud(false);

            // A piston or rotor may have attached since this was last looked at, which changes which grids
            // are part of the construct without adding a block to any of them.
            _unit.World.Invalidate();

            // And the toolbars, which nothing reads unless something asks: a slot may have been
            // re-pointed since this screen was last open, by this player or another one.
            _unit.World.LookAgain();

            _wasDown = false;
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
        /// No guard here against a close arriving on an Enter or a Space release, because the cause is
        /// gone: the harness's close button is a real focusable control that takes focus on the screen's
        /// first update and fires on a key being released, so with it enabled every Enter finishing a
        /// rename and every space typed into a search box arrives as a request to leave. The button is
        /// switched off instead.
        /// </para>
        /// </remarks>
        protected override bool OnCancelling()
        {
            return _view.Escape();
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
        /// <summary>Hides the game's HUD while this is open, and gives it back as it closes.</summary>
        /// <remarks>
        /// Forced to its minimum rather than switched off, which leaves the player's own HUD setting
        /// alone: lifting the force puts back whatever they had. Player 0 is this machine's own player,
        /// done here and nowhere else, so no server is asked.
        /// </remarks>
        static void Hud(bool shown)
        {
            MyVisualScriptLogicProvider.ShowHud(shown, 0);
        }

        protected override void OnClosed(bool isUnloading)
        {
            _unit.Showing = false;
            Hud(true);

            _unit.Persist();

            if (!isUnloading)
                Offer();

            if (_closed != null)
                _closed();
        }

        /// <summary>
        /// Puts the status app on the Sprocket's own screen, where the screen is showing nothing.
        /// </summary>
        /// <remarks>
        /// Somebody who has just built their first rule has a block with automation in it and a blank
        /// panel on the front of it, and the connection between the two is not obvious from either. So
        /// the way out of the editor is where it is offered, once, by doing it.
        /// <para>
        /// <b>Only a surface set to nothing at all.</b> Anything else is a choice somebody made - a
        /// clock, an image, their own text - and taking that over would be the mod deciding it matters
        /// more than they do. A block with nothing built in it is left alone too: a status panel that
        /// says "nothing built yet" is worse than the blank screen it replaced.
        /// </para>
        /// </remarks>
        void Offer()
        {
            if (_unit.Editor.Program.Systems.Count == 0)
                return;

            var provider = _unit.Block as IngameSurfaces;
            if (provider == null)
                return;

            if (provider.SurfaceCount == 0)
                return;

            IngameSurface screen = provider.GetSurface(0);
            if (screen == null || screen.ContentType != ContentType.NONE)
                return;

            screen.ContentType = ContentType.SCRIPT;
            screen.Script = StatusApp.Id;
        }

        protected override void OnHandleInput()
        {
            IMyInput input = MyAPIGateway.Input;
            if (input == null)
                return;

            Vector2 pixels;
            if (_screen.TryCursorPixels(out pixels))
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
                _view.PointerDown(_pointer, Presses(), input.IsAnyShiftKeyPressed(), input.IsAnyCtrlKeyPressed());
            }
            else if (down)
            {
                _view.PointerMove(_pointer);
            }
            else if (_wasDown)
            {
                _view.PointerUp(_pointer);

                // Straight out, and deliberately not down the road Escape takes. The cross is the quick
                // exit: it does not retreat a page, it does not put a question away and wait to be pressed
                // again, it leaves. What that abandons is a question or a rename in progress, and neither is
                // sent to core until it is committed, so there is no half-finished edit to strand.
                if (_view.TakeClose())
                    Close();
            }

            _wasDown = down;

            Type();
        }

        /// <summary>Sends the keyboard wherever the screen says it belongs.</summary>
        /// <remarks>
        /// A question with answers has no box, so Enter is the whole of what it wants: the page said which answer that
        /// takes. Otherwise the keys go to whatever holds the keyboard, which is how a search box narrows a list as
        /// somebody types.
        /// </remarks>
        void Type()
        {
            if (_view.IsQuestioning)
            {
                if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Enter))
                    _view.TakeDefault();

                return;
            }

            if (_view.Input.Focused != null)
                KeysToInput();
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
        /// Where the screen actually is, in UI units.
        /// </summary>
        /// <remarks>
        /// <c>Position</c> is the centre of the panel in the game's normalized GUI coordinates and
        /// <c>Size</c> is normalized too, while everything laid out is in UI units against the safe
        /// area. Read every frame rather than cached because a resolution or UI-scale change moves it, and
        /// <c>OnLayoutChanged</c> is a notification that would then have to be remembered and honoured.
        /// </remarks>
        RectangleF Bounds()
        {
            Vector2 size = Size ?? new Vector2(EditorView.ScreenWidth, EditorView.ScreenHeight);
            Vector2 topLeft = Position - size * 0.5f;

            Vector2 origin = _graphics.FromPixels(_screen.NormalizedToPixels(topLeft));
            Vector2 extent = _graphics.FromPixelSize(_screen.NormalizedSizeToPixels(size));

            return new RectangleF(origin.X, origin.Y, extent.X, extent.Y);
        }

        /// <summary>Sends the keys to the view's input root: typed characters, the editing keys repeating while held, and the shortcuts.</summary>
        /// <remarks>Escape is not read here; it arrives at <see cref="OnCancelling" />.</remarks>
        void KeysToInput()
        {
            IMyInput input = MyAPIGateway.Input;
            var root = _view.Input;
            var shift = input.IsAnyShiftKeyPressed();
            var ctrl = input.IsAnyCtrlKeyPressed();
            var now = Now();

            if (ctrl)
            {
                for (var i = 0; i < _shortcuts.Length; i++)
                {
                    if (input.IsNewKeyPressed(_shortcuts[i]))
                        root.Key(_shortcuts[i], shift, true);
                }
            }
            else
            {
                var typed = input.TextInput;
                for (var i = 0; i < typed.Count; i++)
                    root.Text(typed[i]);

                if (typed.Count > 0)
                    _blinkFrom = now;
            }

            if (input.IsNewKeyPressed(MyKeys.Enter))
                root.Key(MyKeys.Enter, shift, ctrl);

            if (input.IsNewKeyPressed(MyKeys.Tab))
                root.Key(MyKeys.Tab, shift, ctrl);

            for (var i = 0; i < _editing.Length; i++)
            {
                var key = _editing[i];
                if (!_repeat.Fires((int)key, input.IsNewKeyPressed(key), input.IsKeyPress(key), now))
                    continue;

                root.Key(key, shift, ctrl);
                _blinkFrom = now;
            }
        }

        /// <summary>How many presses in quick succession the one going down now is, counted where the button goes down.</summary>
        /// <remarks>
        /// Counted here rather than asked of the game, which reports double clicks only for its own controls. Slack as well
        /// as time, because a double click that moved half the screen was two separate presses that happened to be quick.
        /// </remarks>
        int Presses()
        {
            var now = Now();

            var again = now - _lastDownFrame <= DoubleClickFrames
                        && Vector2.Distance(_lastDownAt, _pointer) <= DoubleClickSlack;

            _presses = again ? _presses + 1 : 1;
            _lastDownFrame = now;
            _lastDownAt = _pointer;
            return _presses;
        }
    }
}
