using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Reactor.Sim.Simulation;
using VRageMath;

namespace Reactor.Sim.Shell
{
    /// <summary>
    /// The simulator window: a screen to draw on, and one line saying how much of what is on it is true.
    /// </summary>
    public sealed class MainWindow : Window
    {
        /// <summary>
        /// Whether windows opened from now on should come up behind everything, without taking focus.
        /// </summary>
        /// <remarks>
        /// Static because the lifetime builds the window itself and there is nowhere to hand an argument
        /// in. One process, one window, one answer - and the alternative is threading a flag through
        /// three classes that have no other reason to know about it.
        /// </remarks>
        public static bool OpensQuietly;

        readonly PhosphorSurface _surface = new PhosphorSurface();
        readonly EditorScene _editor = new EditorScene();
        readonly ProbeScene _probe = new ProbeScene();
        readonly TextBlock _status = new TextBlock();

        Avalonia.Point _pointer;
        bool _caretVisible = true;
        bool _showProbe;
        string _lastStatus;

        // The last frame's window-pixels-to-UI-units scale, kept so a click can be placed without
        // recomputing what the backend already worked out.
        float _scale = 1f;
        RectangleF _screen;

        public MainWindow()
        {
            // Set before the window is shown, so the first appearance is already the quiet one rather
            // than a flash of focus followed by a correction.
            ShowActivated = !OpensQuietly;

            Title = "Reactor - Space Engineers screen";
            Width = 1280;
            Height = 720;
            Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(28, 34, 40));

            _status.Margin = new Thickness(10, 6);
            _status.FontSize = 12;
            _status.Foreground = Brushes.Gainsboro;
            _status.HorizontalAlignment = HorizontalAlignment.Left;

            var root = new DockPanel();
            DockPanel.SetDock(_status, Dock.Bottom);
            root.Children.Add(_status);

            Control strip = BuildSimulateStrip();
            DockPanel.SetDock(strip, Dock.Bottom);
            root.Children.Add(strip);

            // The same stand-in for the world the headless shot draws on, and for the same reason: the
            // veil the screen is drawn behind cannot be judged against a flat colour.
            var stage = new Panel();
            stage.Children.Add(new Backdrop());
            stage.Children.Add(_surface);

            root.Children.Add(stage);
            Content = root;

            // Closing is the one moment a timer cannot cover.
            Closing += (s, e) => _editor.Save();

            // After it exists, because there is no handle to push around until then. ShowActivated on
            // its own keeps the keyboard; this also puts it behind whatever is being worked in.
            Opened += (s, e) =>
            {
                if (!OpensQuietly)
                    return;

                var handle = TryGetPlatformHandle();
                if (handle != null)
                    Attention.StandBack(handle.Handle);
            };

            _surface.Frame += OnFrame;
            _surface.PointerMoved += OnPointerMoved;
            _surface.PointerPressed += OnPointerPressed;
            _surface.PointerWheelChanged += (s, e) =>
            {
                // A notch moves about three rows, which is what a list this size wants. The editor clamps
                // it, so pushing past either end simply stops.
                if (!_showProbe)
                    _editor.ScrollBy(ToUi(e.GetPosition(_surface)), (float)-e.Delta.Y * 60f);

                _surface.InvalidateVisual();
            };
            _surface.PointerReleased += (s, e) =>
            {
                _probe.SetPressed(false);
                _editor.Drop();
                _surface.InvalidateVisual();
            };

            // The ship's clock. Core counts no time for itself: the game passes its update counter, this
            // passes a timer, a test passes whatever it likes.
            var clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            clock.Tick += (s, e) =>
            {
                _editor.Tick();
                _surface.InvalidateVisual();
            };
            clock.Start();

            // The caret blinks on the window's clock too. The box has none of its own, on purpose: a widget
            // that knew what a tick was would be a widget that knew which target it was on.
            var blink = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
            blink.Tick += (s, e) =>
            {
                _caretVisible = !_caretVisible;
                _surface.InvalidateVisual();
            };
            blink.Start();

            Opened += (s, e) => _surface.Focus();
        }

        void OnFrame(Phosphor.ScreenSpaceGraphics g)
        {
            // Window pixels to UI units, derived from what the backend reports rather than recomputed
            // alongside it, so a mistake in there shows up here instead of being mirrored.
            _screen = g.ScreenBounds;
            _scale = _screen.Width > 0f ? (float)_surface.Bounds.Width / _screen.Width : 1f;

            if (_showProbe)
            {
                _probe.CaretVisible = _caretVisible;
                _probe.MovePointer(ToUi(_pointer), _screen);
                _probe.Draw(g);
            }
            else
            {
                _editor.CaretVisible = _caretVisible;
                _editor.Draw(g);
            }

            Vector2 size = g.Size;
            string status = string.Format(
                "{0}   UI {1:0}x{2:0}   window {3:0}x{4:0}   text: {5}   [F1] {6}",
                _showProbe ? "probe" : "Reactor",
                size.X, size.Y, _surface.Bounds.Width, _surface.Bounds.Height,
                _surface.MetricsSource,
                _showProbe ? "show Reactor" : "show the drawing probe");

            // Posted rather than assigned. This runs inside the render pass, and touching another control's
            // properties there invalidates a visual that has already been walked, which Avalonia refuses.
            if (status != _lastStatus)
            {
                _lastStatus = status;
                Dispatcher.UIThread.Post(() => _status.Text = status);
            }
        }

        /// <summary>
        /// The controls that stand in for the world.
        /// </summary>
        /// <remarks>
        /// Scaffolding, and emphatically not Reactor. Tripping a sensor or draining a tank is something
        /// the world does, and in the game a ship actually flies up to the sensor - so none of this exists
        /// in core, none of it exists in the screens the mod compiles, and it lives here in the window
        /// rather than anywhere it could be mistaken for part of the product.
        /// <para>
        /// Drawn in ordinary desktop controls for the same reason. The screen above it is the thing being
        /// built; anything sitting underneath in the same clothes would read as part of it.
        /// </para>
        /// </remarks>
        Control BuildSimulateStrip()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            row.Children.Add(new TextBlock
            {
                Text = "Prototype - stands in for the game:",
                Foreground = Brushes.DimGray,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });

            foreach (Reactor.Host.System.SimEntity entity in _editor.Pokeable())
            {
                var button = entity as Reactor.Host.System.SimButton;
                if (button != null)
                {
                    row.Children.Add(Poke("press " + button.Name, () => button.Press()));
                    continue;
                }

                var sensor = entity as Reactor.Host.System.SimSensor;
                if (sensor != null)
                {
                    bool tripped = false;
                    Button control = null;
                    control = Poke("trip " + sensor.Name, () =>
                    {
                        tripped = !tripped;
                        sensor.SetDetecting(tripped);
                        control.Foreground = tripped ? Brushes.LightGreen : Brushes.Gainsboro;
                    });

                    row.Children.Add(control);
                    continue;
                }

                var tank = entity as Reactor.Host.System.SimTank;
                if (tank != null)
                {
                    Button control = null;
                    control = Poke("drain " + tank.Name, () =>
                    {
                        tank.Draining = !tank.Draining;
                        control.Foreground = tank.Draining ? Brushes.LightGreen : Brushes.Gainsboro;
                    });

                    row.Children.Add(control);
                }
            }

            return new Border
            {
                Child = row,
                Padding = new Thickness(12, 6),
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Opacity = 0.75
            };
        }

        Button Poke(string text, Action act)
        {
            var button = new Button
            {
                Content = text,
                Background = Brushes.Transparent,
                Foreground = Brushes.Gainsboro,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 3),
                FontSize = 12
            };

            button.Click += (s, e) =>
            {
                act();
                _surface.InvalidateVisual();
            };

            return button;
        }

        /// <summary>A point in the window, in the units a layout was measured in.</summary>
        Vector2 ToUi(Avalonia.Point point)
        {
            if (_scale <= 0f)
                return Vector2.Zero;

            return new Vector2((float)point.X / _scale + _screen.X, (float)point.Y / _scale + _screen.Y);
        }

        void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_showProbe)
            {
                _probe.SetPressed(true);
            }
            else
            {
                Vector2 at = ToUi(e.GetPosition(_surface));

                // A grab first. Something that can be picked up must not also count as a press on
                // whatever was underneath it.
                if (!_editor.Grab(at))
                    _editor.Press(at, e.ClickCount);
            }

            _surface.InvalidateVisual();
        }

        void OnPointerMoved(object sender, PointerEventArgs e)
        {
            _pointer = e.GetPosition(_surface);

            if (_showProbe)
            {
                _surface.InvalidateVisual();
                return;
            }

            if (_editor.IsDragging)
            {
                _editor.Carry(ToUi(_pointer));
                _surface.InvalidateVisual();
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            if (string.IsNullOrEmpty(e.Text))
                return;

            Phosphor.TextBox box = Typing();
            if (box == null)
                return;

            for (int i = 0; i < e.Text.Length; i++)
            {
                char c = e.Text[i];
                if (c >= ' ')
                    box.InsertChar(c);
            }

            Wake();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.F1)
            {
                _showProbe = !_showProbe;
                Wake();
                return;
            }

            Phosphor.TextBox box = Typing();
            if (box == null)
                return;

            bool shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
            bool control = (e.KeyModifiers & KeyModifiers.Control) != 0;

            switch (e.Key)
            {
                case Key.Enter:
                    if (!_showProbe)
                        _editor.CommitAsk();
                    break;
                case Key.Escape:
                    if (!_showProbe)
                        _editor.CancelAsk();
                    break;
                case Key.Back:
                    box.Backspace();
                    break;
                case Key.Delete:
                    box.DeleteForward();
                    break;
                case Key.Left:
                    box.MoveCaretLeft(shift);
                    break;
                case Key.Right:
                    box.MoveCaretRight(shift);
                    break;
                case Key.Home:
                    box.MoveCaretHome(shift);
                    break;
                case Key.End:
                    box.MoveCaretEnd(shift);
                    break;
                case Key.A:
                    if (control)
                        box.SelectAll();
                    break;
                default:
                    return;
            }

            Wake();
        }

        /// <summary>
        /// Whichever box is currently taking keys, or null when nothing is. The probe always has one; the
        /// editor only while a question is open, so typing does not silently land somewhere invisible.
        /// </summary>
        Phosphor.TextBox Typing()
        {
            if (_showProbe)
                return _probe.Box;

            return _editor.Typing;
        }

        /// <summary>Shows the caret immediately after an edit, so typing never hides it mid-blink.</summary>
        void Wake()
        {
            if (!_showProbe)
                _editor.Typed();

            _caretVisible = true;
            _surface.InvalidateVisual();
        }
    }
}
