using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Phosphor;
using Reactor.Sim.Content;
using Reactor.Screens;
using Sandbox.ModAPI;
using VRageMath;
using AvColor = Avalonia.Media.Color;
using Color = VRageMath.Color;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// A window pretending to be the game's screen.
    /// <para>
    /// It sets up what the game would have supplied - a safe GUI area, a viewport - then runs a real
    /// <see cref="ScreenSpaceGraphics" /> over whatever the scene draws, and replays the calls it recorded.
    /// Nothing here reaches into Phosphor: the backend under test is the one the mod ships, unmodified, and
    /// the only thing swapped out from under it is the game.
    /// </para>
    /// </summary>
    public sealed class PhosphorSurface : Control
    {
        readonly ScreenSpaceDevice _device = new ScreenSpaceDevice();
        readonly FallbackFonts _shapes;
        readonly SeFontMetrics _exact;
        readonly GlyphAtlas _atlas;
        readonly Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>();
        readonly Dictionary<string, Bitmap> _art = new Dictionary<string, Bitmap>();
        readonly Dictionary<string, Bitmap> _symbols = new Dictionary<string, Bitmap>();

        ScreenSpaceGraphics _graphics;

        /// <summary>
        /// The resolution the screen is drawn at, whatever size the window happens to be.
        /// </summary>
        /// <remarks>
        /// <b>This is a harness concern and stays here.</b> Phosphor has no idea it exists, and the mod
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

        public PhosphorSurface()
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
            _device.Fonts = (IFontMetrics)_exact ?? _shapes;
            _device.Install();
            Focusable = true;
        }

        /// <summary>Draws one frame. Handed the same graphics object the mod would hand its UI.</summary>
        public event Action<ScreenSpaceGraphics> Frame;

        /// <summary>Whether the numbers text is laid out against are the game's own.</summary>
        public bool MetricsAreExact
        {
            get { return _exact != null; }
        }

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

        public override void Render(DrawingContext context)
        {
            if (Bounds.Width <= 0d || Bounds.Height <= 0d || Target.Width <= 0 || Target.Height <= 0)
                return;

            // The whole frame is built at Target and then fitted, rather than built at whatever the window
            // is. Letterboxed rather than stretched, because the shape of a screen is part of what is being
            // checked and a squashed one would quietly answer a different question.
            double fit = Math.Min(Bounds.Width / Target.Width, Bounds.Height / Target.Height);
            double shown = Target.Width * fit;
            double tall = Target.Height * fit;

            Viewport = new Rect(
                (Bounds.Width - shown) / 2d, (Bounds.Height - tall) / 2d, shown, tall);

            double width = Target.Width;
            double height = Target.Height;

            // What MyScreenSpace.GetSafeGuiArea answers: four by three at the full height of the screen,
            // centred. Anything wider than that is outside a layout and reachable only by a pointer.
            double safeWidth = height * 4d / 3d;
            _device.SafeGuiArea = new RectangleF(
                (float)((width - safeWidth) / 2d), 0f, (float)safeWidth, (float)height);
            _device.ViewportSize = new Vector2((float)width, (float)height);

            _device.BeginFrame();

            // Held across frames, as the mod holds one. Begin re-samples the area, so a resized window is
            // picked up without building another.
            if (_graphics == null)
                _graphics = new ScreenSpaceGraphics(MyScreenSpaceLayer.AboveHud);

            _graphics.Begin();

            Action<ScreenSpaceGraphics> frame = Frame;
            if (frame != null)
                frame(_graphics);

            // Everything below was recorded in target pixels, so the fit goes on here rather than being
            // folded into any of it. Clipped to the viewport as well, since a letterbox that let the
            // screen paint into its own margins would not be a letterbox.
            using (context.PushClip(Viewport))
            using (context.PushTransform(
                Matrix.CreateScale(fit, fit) * Matrix.CreateTranslation(Viewport.X, Viewport.Y)))
            {
                Replay(context);
            }
        }

        void Replay(DrawingContext context)
        {
            IDisposable clip = null;

            try
            {
                IList<DrawCommand> commands = _device.Commands;
                for (int i = 0; i < commands.Count; i++)
                {
                    DrawCommand command = commands[i];

                    switch (command.Kind)
                    {
                        case DrawKind.SetClip:
                            if (clip != null)
                                clip.Dispose();
                            clip = context.PushClip(ToRect(command.Clip));
                            break;

                        case DrawKind.ClearClip:
                            if (clip != null)
                                clip.Dispose();
                            clip = null;
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
                if (clip != null)
                    clip.Dispose();
            }
        }

        void DrawSprite(DrawingContext context, DrawCommand command)
        {
            Rect destination = ToRect(command.Destination);
            string material = command.Material.String;

            IDisposable rotation = null;
            if (Math.Abs(command.Rotation) > 0.0001f)
            {
                Point centre = destination.Center;
                rotation = context.PushTransform(
                    Matrix.CreateTranslation(-centre.X, -centre.Y)
                    * Matrix.CreateRotation(command.Rotation)
                    * Matrix.CreateTranslation(centre.X, centre.Y));
            }

            try
            {
                if (material == "Phosphor_Fill")
                {
                    context.FillRectangle(BrushFor(command.Color), destination);
                    return;
                }

                if (material == "Phosphor_Cursor" || material == "Phosphor_CursorHand")
                {
                    DrawCursor(context, destination, command.Color, material == "Phosphor_CursorHand");
                    return;
                }

                // A block's picture. Missing is a fault like any other missing texture - the screen has
                // stopped drawing anything else in its place, so nothing here should whisper about it.
                if (material.StartsWith(Icons.Prefix, StringComparison.Ordinal))
                {
                    Bitmap art = ArtFor(material.Substring(Icons.Prefix.Length));
                    if (art != null)
                    {
                        Shrink(context, art, destination);
                        return;
                    }
                }

                // A symbol, in whatever colour was asked for.
                Bitmap symbol = SymbolFor(material, command.Color);
                if (symbol != null)
                {
                    Shrink(context, symbol, destination);
                    return;
                }

                DrawMissing(context, destination, material);
            }
            finally
            {
                if (rotation != null)
                    rotation.Dispose();
            }
        }

        /// <summary>
        /// Draws a picture into a box a good deal smaller than it is.
        /// </summary>
        /// <remarks>
        /// The sampling is asked for by name because the default is not good enough for what these are. A
        /// ring is a five-hundred-pixel picture of a hairline and the mark it is drawn into is fifteen
        /// across, and sampled cheaply that reduction keeps whichever pixels it happens to land on and
        /// throws the rest away - which draws a dotted circle. The game's own sampler averages what it
        /// skips, so cheap scaling here would be the simulator inventing a defect the mod does not have,
        /// and that is the one thing it must never do.
        /// </remarks>
        static void Shrink(DrawingContext context, Bitmap picture, Rect destination)
        {
            using (context.PushRenderOptions(new RenderOptions
            {
                BitmapInterpolationMode = BitmapInterpolationMode.HighQuality
            }))
            {
                context.DrawImage(picture, destination);
            }
        }

        /// <summary>
        /// A material the simulator has no art for. Drawn loudly on purpose: a missing texture rendered as
        /// a tasteful grey box is indistinguishable from a design decision, and the one thing a stand-in
        /// must never do is look deliberate.
        /// </summary>
        void DrawMissing(DrawingContext context, Rect destination, string material)
        {
            var fill = new SolidColorBrush(AvColor.FromArgb(90, 255, 0, 200));
            var edge = new Pen(new SolidColorBrush(AvColor.FromArgb(220, 255, 0, 200)), 1d);

            context.FillRectangle(fill, destination);
            context.DrawRectangle(null, edge, destination);
            context.DrawLine(edge, destination.TopLeft, destination.BottomRight);
            context.DrawLine(edge, destination.TopRight, destination.BottomLeft);

            if (destination.Width < 40d || destination.Height < 14d)
                return;

            FormattedText label = _shapes.Build(material, 11d);
            label.SetForegroundBrush(Brushes.White);
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
                figure.BeginFigure(new Point(x, y), true);
                figure.LineTo(new Point(x, y + size * 0.78));
                figure.LineTo(new Point(x + size * 0.22, y + size * 0.58));
                figure.LineTo(new Point(x + size * 0.38, y + size * 0.92));
                figure.LineTo(new Point(x + size * 0.52, y + size * 0.85));
                figure.LineTo(new Point(x + size * 0.36, y + size * 0.52));
                figure.LineTo(new Point(x + size * 0.62, y + size * 0.50));
                figure.EndFigure(true);
            }

            IBrush body = hand ? Brushes.Gold : BrushFor(color);
            context.DrawGeometry(body, new Pen(Brushes.Black, 1d), geometry);
        }

        void DrawText(DrawingContext context, DrawCommand command)
        {
            if (string.IsNullOrEmpty(command.Text))
                return;

            // Drawn as the game would draw it, boxes and all. Anything else here has glyphs Space
            // Engineers has never heard of, and letting them show would make this window flatter the mod.
            string text = _exact == null ? command.Text : _exact.AsDrawn(command.Text);

            if (_atlas != null && DrawGlyphs(context, command, text))
                return;

            FormattedText formatted = _shapes.Build(
                text, _shapes.FontSizeFor(command.Font, command.Scale));

            formatted.SetForegroundBrush(BrushFor(command.Color));

            var origin = new Point(command.Position.X, command.Position.Y);

            // Stretched sideways onto the width the game's own advances say this run occupies. The
            // letterforms are a stand-in either way, and of the two things that could be wrong about them,
            // the shapes are cosmetic and the width is the one every "does this fit" judgement rests on.
            // Distorted letters that occupy the right space beat handsome ones that lie.
            double target = _device.Fonts.MeasurePixels(command.Font, command.Text, command.Scale).X;
            double stretch = formatted.Width > 0.01d && target > 0d ? target / formatted.Width : 1d;

            IDisposable scaled = null;
            if (Math.Abs(stretch - 1d) > 0.001d)
            {
                scaled = context.PushTransform(
                    Matrix.CreateTranslation(-origin.X, 0d)
                    * Matrix.CreateScale(stretch, 1d)
                    * Matrix.CreateTranslation(origin.X, 0d));
            }

            try
            {
                context.DrawText(formatted, origin);
            }
            finally
            {
                if (scaled != null)
                    scaled.Dispose();
            }
        }

        /// <summary>
        /// Draws a run out of the game's own font page, one glyph at a time.
        /// </summary>
        /// <remarks>
        /// Deliberately walked the same way <see cref="SeFontMetrics.WidthInUnits" /> walks it - kern
        /// first, then advance, then the game's per-character spacing - because the two answering
        /// differently is the one failure that would be invisible. A layout is measured by that method
        /// and drawn by this one, and a run that measured one width and drew another would look like a
        /// wrapping bug for as long as it took somebody to suspect the font.
        /// </remarks>
        bool DrawGlyphs(DrawingContext context, DrawCommand command, string text)
        {
            float unit = _exact.EffectiveScale(command.Scale);
            if (unit <= 0f)
                return false;

            Bitmap page = _atlas.Tinted(command.Color.R, command.Color.G, command.Color.B);

            double pen = command.Position.X;
            double top = command.Position.Y + _exact.CellTop * unit;
            char previous = ' ';

            // The whole run's alpha at once, rather than a paler tint per colour: the page is already a
            // coverage mask, so tinting and fading are separate concerns and only one of them is worth
            // keeping a copy of.
            IDisposable fade = command.Color.A < 255
                ? context.PushOpacity(command.Color.A / 255d)
                : null;

            // Once for the whole run, for the reason spelled out on Shrink: a cell on the page is
            // forty-five tall and a small line of text draws it at half that, and sampled cheaply a
            // one-texel diagonal falls between the samples and disappears. It is why ON came out broken
            // where OFF beside it did not - N is the only letter of the three with a diagonal in it.
            IDisposable sampling = context.PushRenderOptions(new RenderOptions
            {
                BitmapInterpolationMode = BitmapInterpolationMode.HighQuality
            });

            try
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    SeFontMetrics.Glyph glyph;
                    if (!_exact.TryGlyph(c, out glyph) && !_exact.TryReplacement(out glyph))
                    {
                        previous = c;
                        continue;
                    }

                    if (previous != ' ')
                        pen += _exact.Kerning(previous, c) * unit;

                    if (glyph.Width > 0 && glyph.Height > 0)
                    {
                        var source = new Rect(glyph.X, glyph.Y, glyph.Width, glyph.Height);
                        var destination = new Rect(
                            pen + glyph.Bearing * unit, top, glyph.Width * unit, glyph.Height * unit);

                        context.DrawImage(page, source, destination);
                    }

                    pen += (glyph.Advance + SeFontMetrics.Spacing) * unit;
                    previous = c;
                }
            }
            finally
            {
                sampling.Dispose();

                if (fade != null)
                    fade.Dispose();
            }

            return true;
        }

        /// <summary>
        /// One symbol in one colour, made the first time that pairing is asked for.
        /// </summary>
        /// <remarks>
        /// The colour is taken as a flat tint over the picture's own coverage rather than multiplied
        /// through it, which is what the glyph page does and for the same reason: every symbol here is
        /// white to within a few points, so the two answers differ by nothing anybody could see, and a
        /// flat fill cannot end up a shade off the colour that was asked for.
        /// <para>
        /// Kept per pairing. A screen asks for perhaps a dozen, where doing it at draw time would be four
        /// hundred small bitmaps a second to arrive at the same twelve.
        /// </para>
        /// </remarks>
        Bitmap SymbolFor(string material, Color color)
        {
            string key = material + "#" + color.R + "," + color.G + "," + color.B + "," + color.A;

            Bitmap tinted;
            if (_symbols.TryGetValue(key, out tinted))
                return tinted;

            string relative = SymbolArt.PathFor(material);
            string png = relative == null ? null : SafeResolve(relative);

            tinted = png == null ? null : Tint(png, color);
            _symbols.Add(key, tinted);
            return tinted;
        }

        /// <summary>Paints <paramref name="color" /> through the coverage of the picture at <paramref name="png" />.</summary>
        static Bitmap Tint(string png, Color color)
        {
            using (var source = new Bitmap(png))
            {
                var size = new PixelSize(source.PixelSize.Width, source.PixelSize.Height);
                var pixels = new byte[size.Width * size.Height * 4];

                unsafe
                {
                    fixed (byte* start = pixels)
                    {
                        source.CopyPixels(
                            new PixelRect(0, 0, size.Width, size.Height),
                            (IntPtr)start, pixels.Length, size.Width * 4);
                    }
                }

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = color.B;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.R;
                    pixels[i + 3] = (byte)(pixels[i + 3] * color.A / 255);
                }

                var bitmap = new WriteableBitmap(
                    size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

                using (ILockedFramebuffer buffer = bitmap.Lock())
                {
                    for (int y = 0; y < size.Height; y++)
                        System.Runtime.InteropServices.Marshal.Copy(
                            pixels, y * size.Width * 4,
                            buffer.Address + y * buffer.RowBytes, size.Width * 4);
                }

                return bitmap;
            }
        }

        /// <summary>
        /// The picture for one kind of thing, decoded on first use and kept.
        /// </summary>
        /// <remarks>
        /// A miss is cached as null rather than retried. Failing to find a picture is a property of the
        /// machine and does not change while the window is open, and a lookup that failed once would
        /// otherwise go back to the disk sixty times a second for the rest of the session.
        /// </remarks>
        Bitmap ArtFor(string name)
        {
            Bitmap art;
            if (_art.TryGetValue(name, out art))
                return art;

            string relative = BlockArt.PathFor(name);
            string png = relative == null ? null : SafeResolve(relative);

            art = png == null ? null : new Bitmap(png);
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

        IBrush BrushFor(Color color)
        {
            string key = color.ToString();

            IBrush brush;
            if (_brushes.TryGetValue(key, out brush))
                return brush;

            brush = new SolidColorBrush(AvColor.FromArgb(color.A, color.R, color.G, color.B));
            _brushes.Add(key, brush);
            return brush;
        }

        static Rect ToRect(Rectangle rectangle)
        {
            return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
    }
}
