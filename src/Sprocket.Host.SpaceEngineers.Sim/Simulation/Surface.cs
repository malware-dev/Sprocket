using System.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sprocket.Sim.Content;
using Sandbox.ModAPI;
using VRageMath;
using Color = VRageMath.Color;
using Matrix = System.Windows.Media.Matrix;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Ink = System.Windows.Media.Color;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// A window pretending to be the game's screen.
    /// </summary>
    /// <remarks>
    /// It sets up what the game would have supplied - a safe GUI area, a viewport - then runs a real
    /// <see cref="ScreenSpaceGraphics" /> over whatever the scene draws, and replays the calls it recorded.
    /// Nothing here reaches into the editor: the backend under test is the one the mod ships, unmodified, and
    /// the only thing swapped out from under it is the game.
    /// </remarks>
    public sealed class Surface : FrameworkElement
    {
        readonly ScreenSpaceDevice _device = new ScreenSpaceDevice();
        readonly FallbackFonts _shapes;

        readonly Typeface _letters = new Typeface("Segoe UI");
        readonly SeFontMetrics _exact;
        readonly GlyphAtlas _atlas;
        readonly Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
        readonly Dictionary<string, BitmapSource> _art = new Dictionary<string, BitmapSource>();
        readonly SeFontMetrics _symbols;
        readonly GlyphAtlas _symbolAtlas;

        readonly SeFontMetrics _mono;
        readonly GlyphAtlas _monoAtlas;

        ScreenSpaceGraphics _graphics;

        /// <summary>
        /// The resolution the screen is drawn at, whatever size the window happens to be.
        /// </summary>
        /// <remarks>
        /// <b>This is a harness concern and stays here.</b> The editor has no idea it exists, and the mod
        /// does not need it: in the game the screen is drawn at the monitor's resolution and there is
        /// nothing to fit it into.
        /// <para>
        /// It exists because the alternative was laying out against the window. Somebody drags a corner and
        /// the layout is suddenly being asked to fit six hundred and ninety units of height, which is not a
        /// height anybody plays at - so every judgement made while looking at it is a judgement about a
        /// screen that will never exist. The layout now always runs at this size and only the picture of it
        /// is scaled, which is the difference between a small window and a small screen.
        /// </para>
        /// </remarks>
        public PixelSize Target { get; set; } = new PixelSize(1920, 1080);

        /// <summary>
        /// Whether the simulated monitor takes the shape of whatever it is being shown in, keeping only
        /// its <see cref="Target" /> height.
        /// </summary>
        /// <remarks>
        /// On for a window somebody resizes, off for a shot at a stated resolution. A player's monitor is
        /// whatever shape it is and the game adapts to it, so pretending a fixed sixteen-by-nine monitor
        /// sits inside a differently shaped window means two letterboxes: one round the monitor, which is
        /// invented, and one round the four-by-three safe area inside it, which is real. This removes the
        /// invented one. The real one stays, because that margin is where the world shows through in the
        /// game and it is part of what the screen looks like.
        /// <para>
        /// <b>Never narrower than four by three</b>, whatever the window does. The game lays its GUI out in
        /// coordinates that run nought to one across the safe rectangle, and that rectangle is
        /// <c>min(width, height * 4/3)</c> - so on every monitor anybody plays on it is four by three and
        /// the layout simply scales. Letting a tall thin window narrow it would hand the layout less room
        /// than any player has, which is the harness inventing a screen again.
        /// </para>
        /// </remarks>
        public bool ShapedByWindow { get; set; }

        /// <summary>
        /// Where <see cref="Target" /> landed inside the control, letterboxed to keep its shape.
        /// </summary>
        /// <remarks>
        /// Public because the pointer has to come back the other way. A press arrives in window
        /// coordinates and the screen thinks in target pixels, and having one place that knows the
        /// mapping is what stops the two drifting apart.
        /// </remarks>
        public Rect Viewport { get; private set; }

        /// <summary>Turns a point on the control into the target pixel under it.</summary>
        public Point ToTarget(Point window)
        {
            if (Viewport.Width <= 0d || Viewport.Height <= 0d)
                return window;

            double scale = Viewport.Width / Target.Width;
            return new Point((window.X - Viewport.X) / scale, (window.Y - Viewport.Y) / scale);
        }

        public Surface()
        {
            // Two different jobs, deliberately kept apart. The game's font data decides how wide a run is,
            // because that is what a layout is judged against and being wrong there is invisible here and
            // expensive in the game. Something else draws the letters, because being wrong there is only
            // ugly.
            //
            // Both halves are now the game's own where it can be reached: the widths from the font file,
            // the shapes from the page it names. The desktop face stays as what happens when neither the
            // game nor the cache can supply them, which is the case this whole arrangement exists for.
            _shapes = new FallbackFonts(_device, "Segoe UI");
            _exact = SeFontMetrics.Load(_device);
            _atlas = _exact == null ? null : GlyphAtlas.Load(_exact.PageFile);

            // The symbol font is ours, so it is simply there - no cache, no game, no fallback. A missing
            // one is a broken build rather than a machine without Space Engineers, and it should look
            // like a fault instead of quietly drawing nothing.
            string here = AppContext.BaseDirectory;
            string maltek = Path.Combine(here, "Content", "Fonts", "Maltek");

            _symbols = SeFontMetrics.LoadFrom(
                _device, Path.Combine(maltek, "MaltekSymbols.xml"), "the mod's own content", true);
            _symbolAtlas = GlyphAtlas.FromDds(Path.Combine(maltek, "MaltekSymbols-0.dds"));

            _mono = SeFontMetrics.LoadFrom(
                _device, Path.Combine(maltek, "MaltekMono.xml"), "the mod's own content", true);
            _monoAtlas = GlyphAtlas.FromDds(Path.Combine(maltek, "MaltekMono-0.dds"));

            _device.Fonts = new FontSet((IFontMetrics)_exact ?? _shapes)
                .And(Symbols.Font, _symbols)
                .And(Lettering.Mono, _mono);
            _device.Install();
            Focusable = true;
        }

        /// <summary>Draws one frame. Handed the same graphics object the mod would hand its UI.</summary>
        public event Action<ScreenSpaceGraphics> Frame;

        /// <summary>Whether the numbers text is laid out against are the game's own.</summary>
        public bool MetricsAreExact => _exact != null;

        /// <summary>How true this frame is, in words, for the line at the bottom of the window.</summary>
        public string MetricsSource
        {
            get
            {
                if (_exact == null)
                    return "approximate - no game font data found, widths are a desktop face's";

                if (_atlas == null)
                {
                    return "the game's own advances, from " + _exact.Source
                        + "; letterforms are still a stand-in";
                }

                return "the game's own font, from " + _exact.Source + " - advances and letterforms both";
            }
        }

        protected override void OnRender(DrawingContext context)
        {
            Paint(context, RenderSize);
        }

        /// <summary>
        /// Draws a frame into whatever is handed in, at whatever size is asked for.
        /// </summary>
        /// <remarks>
        /// Public and explicit because a window and a headless shot want opposite things from this
        /// framework: a window wants to be told it is out of date and repainted when the dispatcher gets
        /// round to it, and a shot wants a frame now. Invalidating and rendering a control answers the
        /// first and silently gives the shot the frame before the one it asked for.
        /// </remarks>
        public void Paint(DrawingContext context, Size into)
        {
            double windowWidth = into.Width;
            double windowHeight = into.Height;

            if (windowWidth <= 0d || windowHeight <= 0d || Target.Width <= 0 || Target.Height <= 0)
                return;

            // The whole frame is built at Target and then fitted, rather than built at whatever the window
            // is. Letterboxed rather than stretched, because the shape of a screen is part of what is being
            // checked and a squashed one would quietly answer a different question.
            if (ShapedByWindow && windowHeight > 0d)
            {
                int square = (int)Math.Round(Target.Height * 4d / 3d);
                int wide = (int)Math.Round(Target.Height * windowWidth / windowHeight);

                Target = new PixelSize(wide > square ? wide : square, Target.Height);
            }

            double fit = Math.Min(windowWidth / Target.Width, windowHeight / Target.Height);
            double shown = Target.Width * fit;
            double tall = Target.Height * fit;

            Viewport = new Rect(
                (windowWidth - shown) / 2d, (windowHeight - tall) / 2d, shown, tall);

            double width = Target.Width;
            double height = Target.Height;

            // What MyScreenSpace.GetSafeGuiArea answers, and it answers it out of MyGuiManager, which
            // does exactly this: the full height, four by three wide, centred - and never wider than the
            // screen, which is the case a monitor taller than it is wide would otherwise get wrong.
            double safeWidth = Math.Min(width, height * 4d / 3d);
            _device.SafeArea = new RectangleF(
                (float)((width - safeWidth) / 2d), 0f, (float)safeWidth, (float)height);
            _device.ViewportSize = new Vector2((float)width, (float)height);

            _device.BeginFrame();

            // Held across frames, as the mod holds one. Begin re-samples the area, so a resized window is
            // picked up without building another.
            if (_graphics == null)
                _graphics = new ScreenSpaceGraphics(_device, MyScreenSpaceLayer.AboveHud, Materials.Fill);

            _graphics.Begin();

            Action<ScreenSpaceGraphics> frame = Frame;
            if (frame != null)
                frame(_graphics);

            // Everything below was recorded in target pixels, so the fit goes on here rather than being
            // folded into any of it. Clipped to the viewport as well, since a letterbox that let the
            // screen paint into its own margins would not be a letterbox.
            var fitted = new Matrix();
            fitted.Scale(fit, fit);
            fitted.Translate(Viewport.X, Viewport.Y);

            using (Clipped(context, Viewport))
            using (Moved(context, fitted))
            {
                Replay(context);
            }
        }

        /// <summary>
        /// One thing pushed onto the canvas, popped when it goes out of scope.
        /// </summary>
        /// <remarks>
        /// The desktop framework's canvas pushes and pops rather than handing back something to dispose,
        /// and a pop that is missed silently poisons every draw after it. So the pops are tied to scopes
        /// here, and the code that draws reads as what it is doing rather than as bookkeeping.
        /// </remarks>
        struct Pushed : IDisposable
        {
            readonly DrawingContext _context;

            public Pushed(DrawingContext context)
            {
                _context = context;
            }

            public void Dispose()
            {
                if (_context != null)
                    _context.Pop();
            }
        }

        static Pushed Clipped(DrawingContext context, Rect to)
        {
            context.PushClip(new RectangleGeometry(to));
            return new Pushed(context);
        }

        static Pushed Moved(DrawingContext context, Matrix by)
        {
            context.PushTransform(new MatrixTransform(by));
            return new Pushed(context);
        }

        static Pushed Faded(DrawingContext context, double opacity)
        {
            context.PushOpacity(opacity);
            return new Pushed(context);
        }

        void Replay(DrawingContext context)
        {
            var clip = new Stack<Pushed>();

            try
            {
                IList<DrawCommand> commands = _device.Commands;
                for (int i = 0; i < commands.Count; i++)
                {
                    DrawCommand command = commands[i];

                    switch (command.Kind)
                    {
                        case DrawKind.SetClip:
                            Unclip(clip);
                            clip.Push(Clipped(context, ToRect(command.Clip)));
                            break;

                        case DrawKind.ClearClip:
                            Unclip(clip);
                            break;

                        case DrawKind.Sprite:
                            DrawSprite(context, command);
                            break;

                        case DrawKind.Text:
                            DrawText(context, command);
                            break;
                    }
                }
            }
            finally
            {
                Unclip(clip);
            }
        }

        /// <summary>
        /// Lets go of whatever clip is in force.
        /// </summary>
        /// <remarks>
        /// A stack because pops have to happen in the order the pushes did, and the frame's own fit is
        /// pushed underneath all of this: popping one too many would take the letterbox with it.
        /// </remarks>
        static void Unclip(Stack<Pushed> clip)
        {
            while (clip.Count > 0)
                clip.Pop().Dispose();
        }

        void DrawSprite(DrawingContext context, DrawCommand command)
        {
            Rect destination = ToRect(command.Destination);
            string material = command.Material.String;

            // Radians on the wire, degrees on this canvas. The game says radians and so does the
            // recorded command; converting here keeps the conversion in one place.
            Pushed? rotation = null;
            if (Math.Abs(command.Rotation) > 0.0001f)
            {
                var spun = new Matrix();
                spun.RotateAt(command.Rotation * 180d / Math.PI,
                    destination.X + destination.Width / 2d, destination.Y + destination.Height / 2d);

                rotation = Moved(context, spun);
            }

            try
            {
                if (material == "Sprocket_Fill")
                {
                    context.DrawRectangle(BrushFor(command.Color), null, destination);
                    return;
                }

                if (material == "Sprocket_Cursor" || material == "Sprocket_CursorHand")
                {
                    DrawCursor(context, destination, command.Color, material == "Sprocket_CursorHand");
                    return;
                }

                // A block's picture. Missing is a fault like any other missing texture - the screen has
                // stopped drawing anything else in its place, so nothing here should whisper about it.
                // Two shapes, both of them block pictures: a definition id, which the game resolves to
                // that block's own picture by itself, and a prefixed word from a host with no blocks to
                // ask. See Icons.MaterialFor, which decides which one a kind gets.
                bool prefixed = material.StartsWith(Icons.Prefix, StringComparison.Ordinal);
                if (prefixed || material.IndexOf('/') >= 0)
                {
                    BitmapSource art = ArtFor(prefixed
                        ? material.Substring(Icons.Prefix.Length)
                        : material);

                    if (art != null)
                    {
                        Shrink(context, art, destination);
                        return;
                    }
                }

                DrawMissing(context, destination, material);
            }
            finally
            {
                if (rotation.HasValue)
                    rotation.Value.Dispose();
            }
        }

        /// <summary>
        /// Draws a picture into a box a good deal smaller than it is.
        /// </summary>
        /// <remarks>
        /// The sampling matters and is not asked for: this canvas averages what it skips by default,
        /// which is what these need. A ring is a five-hundred-pixel picture of a hairline and the mark it
        /// is drawn into is fifteen across, and sampled cheaply that reduction would keep whichever
        /// pixels it happened to land on and throw the rest away - a dotted circle, which is the
        /// simulator inventing a defect the mod does not have.
        /// </remarks>
        static void Shrink(DrawingContext context, BitmapSource picture, Rect destination)
        {
            context.DrawImage(picture, destination);
        }

        /// <summary>
        /// A material the simulator has no art for. Drawn loudly on purpose: a missing texture rendered as
        /// a tasteful grey box is indistinguishable from a design decision, and the one thing a stand-in
        /// must never do is look deliberate.
        /// </summary>
        void DrawMissing(DrawingContext context, Rect destination, string material)
        {
            var fill = new SolidColorBrush(Ink.FromArgb(90, 255, 0, 200));
            var edge = new Pen(new SolidColorBrush(Ink.FromArgb(220, 255, 0, 200)), 1d);

            context.DrawRectangle(fill, null, destination);
            context.DrawRectangle(null, edge, destination);
            context.DrawLine(edge, destination.TopLeft, destination.BottomRight);
            context.DrawLine(edge, destination.TopRight, destination.BottomLeft);

            if (destination.Width < 40d || destination.Height < 14d)
                return;

            FormattedText label = Letters(material, 11d, Brushes.White);
            context.DrawText(label, new Point(destination.X + 3d, destination.Y + 2d));
        }

        /// <summary>
        /// The pointer, drawn as geometry rather than as the game's texture. The real one lives in the
        /// game's content and arrives with the extracted assets; this keeps the shape and the hotspot right
        /// in the meantime, which is what pointing is actually about.
        /// </summary>
        void DrawCursor(DrawingContext context, Rect destination, Color color, bool hand)
        {
            double size = Math.Min(destination.Width, destination.Height);
            double x = destination.X;
            double y = destination.Y;

            var geometry = new StreamGeometry();
            using (StreamGeometryContext figure = geometry.Open())
            {
                figure.BeginFigure(new Point(x, y), true, true);
                figure.LineTo(new Point(x, y + size * 0.78), true, false);
                figure.LineTo(new Point(x + size * 0.22, y + size * 0.58), true, false);
                figure.LineTo(new Point(x + size * 0.38, y + size * 0.92), true, false);
                figure.LineTo(new Point(x + size * 0.52, y + size * 0.85), true, false);
                figure.LineTo(new Point(x + size * 0.36, y + size * 0.52), true, false);
                figure.LineTo(new Point(x + size * 0.62, y + size * 0.50), true, false);
            }

            Brush body = hand ? Brushes.Gold : BrushFor(color);
            context.DrawGeometry(body, new Pen(Brushes.Black, 1d), geometry);
        }

        void DrawText(DrawingContext context, DrawCommand command)
        {
            if (string.IsNullOrEmpty(command.Text))
                return;

            // Drawn as the game would draw it, boxes and all. Anything else here has glyphs Space
            // Engineers has never heard of, and letting them show would make this window flatter the mod.
            //
            // A face of ours falls back to nothing rather than to a desktop typeface: a symbol drawn as
            // the letter its codepoint happens to be is worse than a gap, and a listing drawn in a
            // proportional stand-in would say the columns line up when they do not.
            bool ours = command.Font == Symbols.Font || command.Font == Lettering.Mono;

            SeFontMetrics metrics = command.Font == Symbols.Font ? _symbols
                : command.Font == Lettering.Mono ? _mono : _exact;

            GlyphAtlas page = command.Font == Symbols.Font ? _symbolAtlas
                : command.Font == Lettering.Mono ? _monoAtlas : _atlas;

            string text = metrics == null ? command.Text : metrics.AsDrawn(command.Text);

            if (page != null && metrics != null && DrawGlyphs(context, command, text, metrics, page))
                return;

            // A symbol with no page behind it has no business falling back to a desktop typeface, which
            // would draw the letter the character happens to be - a literal "d" where a bin belongs. That
            // is worse than nothing, because it looks deliberate.
            if (ours)
                return;

            FormattedText formatted = Letters(text, _shapes.FontSizeFor(command.Font, command.Scale),
                BrushFor(command.Color));

            var origin = new Point(command.Position.X, command.Position.Y);

            // Stretched sideways onto the width the game's own advances say this run occupies. The
            // letterforms are a stand-in either way, and of the two things that could be wrong about them,
            // the shapes are cosmetic and the width is the one every "does this fit" judgement rests on.
            // Distorted letters that occupy the right space beat handsome ones that lie.
            double target = _device.Fonts.MeasurePixels(command.Font, command.Text, command.Scale).X;
            double stretch = formatted.Width > 0.01d && target > 0d ? target / formatted.Width : 1d;

            Pushed? scaled = null;
            if (Math.Abs(stretch - 1d) > 0.001d)
            {
                var wider = new Matrix();
                wider.ScaleAt(stretch, 1d, origin.X, 0d);

                scaled = Moved(context, wider);
            }

            try
            {
                context.DrawText(formatted, origin);
            }
            finally
            {
                if (scaled.HasValue)
                    scaled.Value.Dispose();
            }
        }

        /// <summary>Draws a run out of its font page, one glyph at a time, where the game's renderer puts each.</summary>
        /// <remarks>
        ///     The game rounds the run's start up to a whole pixel, then places each cell at the pen plus the
        ///     glyph's bearing across, and at the line's top plus <see cref="SeFontMetrics.CellTop" /> down.
        ///     Walked as <see cref="SeFontMetrics.MeasurePixels" /> walks, so a run draws as wide as it measures.
        /// </remarks>
        bool DrawGlyphs(DrawingContext context, DrawCommand command, string text,
            SeFontMetrics metrics, GlyphAtlas atlas)
        {
            float unit = metrics.DrawScale(command.Scale);
            if (unit <= 0f)
                return false;

            BitmapSource page = atlas.Tinted(command.Color.R, command.Color.G, command.Color.B);

            double start = Math.Ceiling(command.Position.X);
            double pen = start;
            double top = Math.Ceiling(command.Position.Y);
            char previous = '\0';

            // The whole run's alpha at once, rather than a paler tint per colour: the page is already a
            // coverage mask, so tinting and fading are separate concerns and only one of them is worth
            // keeping a copy of.
            Pushed? fade = command.Color.A < 255
                ? Faded(context, command.Color.A / 255d)
                : null;

            try
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    if (c == SeFontMetrics.Newline)
                    {
                        pen = start;
                        top += metrics.FontLineHeight * unit;
                        previous = '\0';
                        continue;
                    }

                    SeFontMetrics.Glyph glyph;
                    if (!metrics.TryDrawn(c, out glyph))
                        continue;

                    pen += metrics.Kerning(previous, c) * unit;
                    previous = c;

                    if (glyph.Width > 0 && glyph.Height > 0)
                    {
                        var destination = new Rect(pen + glyph.Bearing * unit,
                            top + SeFontMetrics.CellTop(glyph) * unit, glyph.Width * unit, glyph.Height * unit);

                        Cell(context, page, glyph, destination);
                    }

                    pen += glyph.Advance * unit;

                    if (i < text.Length - 1)
                        pen += SeFontMetrics.Spacing * unit;
                }
            }
            finally
            {
                if (fade.HasValue)
                    fade.Value.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Draws one cell of a font page into a box.
        /// </summary>
        /// <remarks>
        /// Through a brush because this canvas cannot draw part of a picture: <c>DrawImage</c> takes the
        /// whole thing. A brush with an absolute viewbox says which part, and the reduction is averaged
        /// rather than sampled, which is what keeps a one-texel diagonal from vanishing at half size.
        /// </remarks>
        static void Cell(DrawingContext context, BitmapSource page, SeFontMetrics.Glyph glyph,
            Rect destination)
        {
            var brush = new ImageBrush(page);
            brush.ViewboxUnits = BrushMappingMode.Absolute;
            brush.Viewbox = new Rect(glyph.X, glyph.Y, glyph.Width, glyph.Height);
            brush.Stretch = Stretch.Fill;

            context.DrawRectangle(brush, null, destination);
        }

        /// <summary>
        /// The picture for one kind of thing, decoded on first use and kept.
        /// </summary>
        /// <remarks>
        /// A miss is cached as null rather than retried. Failing to find a picture is a property of the
        /// machine and does not change while the window is open, and a lookup that failed once would
        /// otherwise go back to the disk sixty times a second for the rest of the session.
        /// </remarks>
        BitmapSource ArtFor(string name)
        {
            BitmapSource art;
            if (_art.TryGetValue(name, out art))
                return art;

            string relative = BlockArt.PathFor(name);
            string png = relative == null ? null : SafeResolve(relative);

            art = png == null ? null : new BitmapImage(new Uri(png, UriKind.Absolute));
            _art.Add(name, art);
            return art;
        }

        static string SafeResolve(string relative)
        {
            try
            {
                return GameImages.Resolve(relative);
            }
            catch (Exception)
            {
                // A decoder that will not run is a reason to show the initial, not a reason to stop
                // drawing the screen.
                return null;
            }
        }

        /// <summary>
        /// A run of text to draw, in whatever face this canvas has.
        /// </summary>
        /// <remarks>
        /// The canvas's own, not the metrics': what a run is measured at and what it is drawn with are
        /// two questions, and only the first one decides a layout. The second is a stand-in until the
        /// game's own page is behind it - see <see cref="GlyphAtlas" />.
        /// </remarks>
        FormattedText Letters(string text, double size, Brush ink)
        {
            // One pixel per unit, for the reason FallbackFonts gives: this canvas is in window pixels
            // and a frame must not change with the monitor it was drawn on.
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                _letters, size, ink, 1d);
        }

        Brush BrushFor(Color color)
        {
            string key = color.ToString();

            Brush brush;
            if (_brushes.TryGetValue(key, out brush))
                return brush;

            brush = new SolidColorBrush(Ink.FromArgb(color.A, color.R, color.G, color.B));
            _brushes.Add(key, brush);
            return brush;
        }

        static Rect ToRect(Rectangle rectangle)
        {
            return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
    }
}
