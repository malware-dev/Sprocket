using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Sandbox.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using IMyInput = VRage.ModAPI.IMyInput;

namespace Lumel.Screens
{
    /// <summary>A screen the game owns, showing an <see cref="IScreenView" /> and handing it the pointer, the wheel and the keys.</summary>
    /// <remarks>
    ///     The screen owns its input, so nothing blacklists controls, nothing fights the camera, and the pointer is simply a
    ///     pointer. None of the work a thing drawn over live gameplay would need is here.
    ///     <para>
    ///         A kind of screen with more to do as it opens and closes derives from this, and calls the base where it
    ///         overrides <see cref="OnOpened" />.
    ///     </para>
    /// </remarks>
    public class GameScreen : MyModScreen
    {
        const int DoubleClickFrames = 20;
        const float DoubleClickSlack = 12f;

        const float ScrollStep = 60f;

        const int BlinkTicks = 30;

        readonly MyKeys[] _editing =
        {
            MyKeys.Back, MyKeys.Delete, MyKeys.Left, MyKeys.Right, MyKeys.Home, MyKeys.End, MyKeys.Up, MyKeys.Down
        };

        readonly MyKeys[] _shortcuts = { MyKeys.A, MyKeys.C, MyKeys.X, MyKeys.V, MyKeys.Z, MyKeys.Y };

        readonly IScreenView _view;
        readonly IScreenSpace _screen;
        readonly ScreenSpaceGraphics _graphics;
        readonly Vector2 _size;
        readonly KeyRepeater _repeat = new KeyRepeater();

        Vector2 _pointer;
        bool _wasDown;
        int _lastDownFrame = int.MinValue;
        Vector2 _lastDownAt;
        int _presses;
        int _blinkFrom;

        /// <summary>
        ///     The screen called <paramref name="name" />, <paramref name="size" /> big in the game's normalized GUI units, showing
        ///     <paramref name="view" /> on <paramref name="screen" /> with <paramref name="fill" /> for flat fills and the pictures
        ///     <paramref name="images" /> resolves.
        /// </summary>
        public GameScreen(string name, Vector2 size, IScreenView view, IScreenSpace screen, MyStringId fill, IImageCatalog images)
            : base(name, null, size)
        {
            _view = view;
            _screen = screen;
            _size = size;

            // BelowHud because that is where a mod screen's own content goes. The scissor is pushed onto whatever layer this
            // names, so the two must agree or clipping silently does nothing.
            _graphics = new ScreenSpaceGraphics(screen, MyScreenSpaceLayer.BelowHud, fill, images);

            // Off, and the view draws its own cross instead. The game lays its controls down before it calls the mod, so the
            // first opaque thing this screen paints buries the button while leaving it live: an invisible way out that still
            // works is a corner that shuts the screen for no visible reason.
            CloseButtonEnabled = false;
            CloseOnEscape = true;
            EnabledBackgroundFade = true;

            // The game draws the cursor, so the view does not. A screen has a real cursor and hands it over, where something
            // drawn during play has to add up the mouse's movement itself.
            DrawMouseCursor = true;

            // Nothing drawn here is multiplied by the transition's alpha, so a fade would draw the screen fully opaque part way
            // through one.
            SkipTransition = true;
        }

        /// <summary>What this shows.</summary>
        protected IScreenView View => _view;

        /// <inheritdoc />
        protected override void OnOpened()
        {
            _wasDown = false;
            _blinkFrom = Now();
        }

        /// <summary>Escape, and the close the game asks for, both arrive here; the view says whether it used the key instead.</summary>
        protected override bool OnCancelling()
        {
            return _view.Escape();
        }

        /// <summary>Keeps the caret blinking, measured from the last keystroke so it is solid the moment somebody types.</summary>
        protected override void OnUpdate(bool hasFocus)
        {
            _view.CaretVisible = (Now() - _blinkFrom) / BlinkTicks % 2 == 0;
        }

        /// <inheritdoc />
        protected override void OnHandleInput()
        {
            IMyInput input = MyAPIGateway.Input;
            if (input == null)
                return;

            Vector2 pixels;
            if (_screen.TryCursorPixels(out pixels))
                _pointer = _graphics.FromPixels(pixels);

            _view.Pointer = _pointer;

            // A notch up is a positive delta and scrolls the content up, which is a negative amount.
            var wheel = input.DeltaMouseScrollWheelValue();
            if (wheel != 0)
                _view.ScrollBy(_pointer, -wheel / 120f * ScrollStep);

            var down = input.IsLeftMousePressed();

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

                // Straight out, and deliberately not down the road Escape takes: the view's cross is the quick exit.
                if (_view.TakeClose())
                    Close();
            }

            _wasDown = down;

            Type();
        }

        /// <inheritdoc />
        protected override void OnDraw(float transitionAlpha)
        {
            _graphics.Begin();
            _view.Update();
            _view.Draw(_graphics, Bounds());
        }

        /// <summary>The game's frame count, which is the clock a double press and the caret's blink are measured by.</summary>
        protected static int Now()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
        }

        /// <summary>Sends the keyboard where the view says it belongs.</summary>
        /// <remarks>
        ///     A question with answers has no box, so Enter is the whole of what it wants. Otherwise the keys go to whatever
        ///     holds the keyboard.
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

        /// <summary>Where the screen is, in UI units.</summary>
        /// <remarks>
        ///     <c>Position</c> is the center of the screen in the game's normalized GUI units and <c>Size</c> is normalized too,
        ///     while everything laid out is in UI units against the safe area. Read every frame, because a resolution or UI
        ///     scale change moves it.
        /// </remarks>
        RectangleF Bounds()
        {
            var size = Size ?? _size;
            var topLeft = Position - size * 0.5f;

            var origin = _graphics.FromPixels(_screen.NormalizedToPixels(topLeft));
            var extent = _graphics.FromPixelSize(_screen.NormalizedSizeToPixels(size));

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
        ///     Counted here rather than asked of the game, which reports double clicks only for its own controls. Slack as well
        ///     as time, because a double click that moved half the screen was two separate presses that happened to be quick.
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
