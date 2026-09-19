using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Sim.Simulation;
using VRageMath;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using TextBox = Sprocket.Host.SpaceEngineers.Layout.TextBox;

namespace Sprocket.Sim.Shell
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

        /// <summary>What one notch of the wheel scrolls, in UI units.</summary>
        /// <remarks>
        /// About three rows, which is what a list this size wants. The framework counts a notch as 120,
        /// and the editor clamps the result, so pushing past either end simply stops.
        /// </remarks>
        const float NotchUnits = 60f;

        const float PerNotch = 120f;

        readonly Surface _surface = new Surface();
        readonly EditorScene _editor = new EditorScene();
        readonly ProbeScene _probe = new ProbeScene();
        readonly TextBlock _status = new TextBlock();

        Point _pointer;
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

            Title = "Sprocket - Space Engineers screen";

            // Where it was last time, if that is still somewhere it can be seen. These two are the
            // fallback for a first run or a monitor that has gone away.
            Width = 1280;
            Height = 720;
            WindowPlace.Restore(this);
            Background = new SolidColorBrush(Color.FromRgb(28, 34, 40));

            _status.Margin = new Thickness(10, 6, 10, 6);
            _status.FontSize = 12;
            _status.Foreground = Brushes.Gainsboro;
            _status.HorizontalAlignment = HorizontalAlignment.Left;

            var root = new DockPanel();
            DockPanel.SetDock(_status, Dock.Bottom);
            root.Children.Add(_status);

            FrameworkElement strip = BuildSimulateStrip();
            DockPanel.SetDock(strip, Dock.Bottom);
            root.Children.Add(strip);

            // A window is resized, so the monitor it stands in for takes its shape. A shot names a
            // resolution instead and keeps it.
            _surface.ShapedByWindow = true;
            _surface.Focusable = true;

            // The same stand-in for the world the headless shot draws on, and for the same reason: the
            // veil the screen is drawn behind cannot be judged against a flat colour.
            var stage = new Grid();
            stage.Children.Add(new Backdrop());
            stage.Children.Add(_surface);

            root.Children.Add(stage);
            Content = root;

            // Closing is the one moment a timer cannot cover. On Closing rather than Closed for the
            // position, because by the time it has closed its position is gone.
            Closing += delegate
            {
                _editor.Save();
                WindowPlace.Remember(this);
            };

            // After the handle exists, because there is nothing to push around until then.
            // ShowActivated on its own keeps the keyboard; this also puts it behind whatever is being
            // worked in.
            SourceInitialized += delegate
            {
                if (!OpensQuietly)
                    return;

                IntPtr handle = new WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero)
                    Attention.StandBack(handle);
            };

            Loaded += delegate { _surface.Focus(); };

            _surface.Frame += OnFrame;
            _surface.MouseMove += OnMouseMove;
            _surface.MouseDown += OnMouseDown;
            _surface.MouseWheel += delegate(object sender, MouseWheelEventArgs e)
            {
                if (!_showProbe)
                {
                    _editor.ScrollBy(ToUi(e.GetPosition(_surface)),
                        -e.Delta / PerNotch * NotchUnits);
                }

                _surface.InvalidateVisual();
            };
            _surface.MouseUp += delegate
            {
                _probe.SetPressed(false);
                _editor.Drop();
                _surface.InvalidateVisual();
            };

            // The ship's clock. Core counts no time for itself: the game passes its update counter, this
            // passes a timer, a test passes whatever it likes.
            var clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            clock.Tick += delegate
            {
                _editor.Tick();
                _surface.InvalidateVisual();
            };
            clock.Start();

            // The caret blinks on the window's clock too. The box has none of its own, on purpose: a widget
            // that knew what a tick was would be a widget that knew which target it was on.
            var blink = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
            blink.Tick += delegate
            {
                _caretVisible = !_caretVisible;
                _surface.InvalidateVisual();
            };
            blink.Start();
        }

        void OnFrame(ScreenSpaceGraphics g)
        {
            // Window pixels to UI units, derived from what the backend reports rather than recomputed
            // alongside it, so a mistake in there shows up here instead of being mirrored.
            _screen = g.ScreenBounds;

            // Against the target rather than against the control. The frame is drawn at the target and
            // then fitted into the window, so this is UI units to target pixels; the window part of the
            // journey is ToTarget's business and belongs in one place.
            _scale = _screen.Width > 0f ? _surface.Target.Width / _screen.Width : 1f;

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
                "{0}   UI {1:0}x{2:0}   drawn at {3}x{4}   shown at {5:0}%   text: {6}   [F1] {7}",
                _showProbe ? "probe" : "Sprocket",
                size.X, size.Y, _surface.Target.Width, _surface.Target.Height,
                _surface.Target.Width > 0 ? _surface.Viewport.Width / _surface.Target.Width * 100d : 0d,
                _surface.MetricsSource,
                _showProbe ? "show Sprocket" : "show the drawing probe");

            // Posted rather than assigned. This runs inside the render pass, and touching another
            // control's properties there invalidates a visual that has already been walked.
            if (status != _lastStatus)
            {
                _lastStatus = status;
                Dispatcher.BeginInvoke(new Action(delegate { _status.Text = status; }));
            }
        }

        /// <summary>
        /// The controls that stand in for the world.
        /// </summary>
        /// <remarks>
        /// Scaffolding, and emphatically not Sprocket. Pressing a button is something a person does to the
        /// ship, and in the game they walk up to the panel - so none of this exists in core, none of it
        /// exists in the screens the mod compiles, and it lives here in the window rather than anywhere
        /// it could be mistaken for part of the product.
        /// <para>
        /// Drawn in ordinary desktop controls for the same reason. The screen above it is the thing being
        /// built; anything sitting underneath in the same clothes would read as part of it.
        /// </para>
        /// </remarks>
        FrameworkElement BuildSimulateStrip()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };

            row.Children.Add(new TextBlock
            {
                Text = "Prototype - stands in for the game:",
                Foreground = Brushes.DimGray,
                FontSize = 11,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            foreach (Machine.Panel panel in _editor.Panels())
            {
                // One poke per button, because that is what a panel is: several buttons, each of
                // which may run something different. Pressing one whose slot is empty is a press
                // nobody hears, exactly as on a real panel.
                for (var button = 0; button < panel.Buttons; button++)
                {
                    var which = button;
                    var name = panel.Buttons == 1
                        ? "press " + panel.Name
                        : "press " + panel.Name + " " + (which + 1);

                    row.Children.Add(Poke(name, delegate { panel.Press(which); }));
                }
            }

            return new Border
            {
                Child = row,
                Padding = new Thickness(12, 6, 12, 6),
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
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12
            };

            button.Click += delegate
            {
                act();
                _surface.InvalidateVisual();
            };

            return button;
        }

        /// <summary>A point in the window, in the units a layout was measured in.</summary>
        Vector2 ToUi(Point point)
        {
            if (_scale <= 0f)
                return Vector2.Zero;

            Point at = _surface.ToTarget(point);
            return new Vector2((float)at.X / _scale + _screen.X, (float)at.Y / _scale + _screen.Y);
        }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // The keyboard follows the screen: a click on it is how somebody starts typing into a box
            // the screen is showing.
            _surface.Focus();

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

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            _pointer = e.GetPosition(_surface);
            _editor.MoveTo(ToUi(_pointer));

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

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (string.IsNullOrEmpty(e.Text))
                return;

            TextBox box = Typing();
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

            TextBox box = Typing();
            if (box == null)
                return;

            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

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
        TextBox Typing()
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
