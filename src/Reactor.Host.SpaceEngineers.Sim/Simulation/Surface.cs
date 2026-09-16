using System.IO;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Reactor.Sim.Content;
using Sandbox.ModAPI;
using VRageMath;
using AvColor = Avalonia.Media.Color;
using Color = VRageMath.Color;
using Matrix = Avalonia.Matrix;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Ui;

namespace Reactor.Sim.Simulation
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
    public sealed class Surface : Control
    {
        readonly ScreenSpaceDevice _device = new ScreenSpaceDevice();
        readonly FallbackFonts _shapes;
        readonly SeFontMetrics _exact;
        readonly GlyphAtlas _atlas;
        readonly Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>();
        readonly Dictionary<string, Bitmap> _art = new Dictionary<string, Bitmap>();
        readonly SeFontMetrics _symbols;
        readonly GlyphAtlas _symbolAtlas;

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

            _device.Fonts = new FontSet((IFontMetrics)_exact ?? _shapes, _symbols);
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

        public override void Render(DrawingContext context)
        {
            if (Bounds.Width <= 0d || Bounds.Height <= 0d || Target.Width <= 0 || Target.Height <= 0)
                return;

            // The whole frame is built at Target and then fitted, rather than built at whatever the window
            // is. Letterboxed rather than stretched, because the shape of a screen is part of what is being
            // checked and a squashed one would quietly answer a different question.
            if (ShapedByWindow && Bounds.Height > 0d)
            {
                int square = (int)Math.Round(Target.Height * 4d / 3d);
                int wide = (int)Math.Round(Target.Height * Bounds.Width / Bounds.Height);

                Target = new PixelSize(wide > square ? wide : square, Target.Height);
            }

            double fit = Math.Min(Bounds.Width / Target.Width, Bounds.Height / Target.Height);
            double shown = Target.Width * fit;
            double tall = Target.Height * fit;

            Viewport = new Rect(
                (Bounds.Width - shown) / 2d, (Bounds.Height - tall) / 2d, shown, tall);

            double width = Target.Width;
            double height = Target.Height;

            // What MyScreenSpace.GetSafeGuiArea answers, and it answers it out of MyGuiManager, which
            // does exactly this: the full height, four by three wide, centred - and never wider than the
            // screen, which is the case a monitor taller than it is wide would otherwise get wrong.
            double safeWidth = Math.Min(width, height * 4d / 3d);
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
                if (material == "Reactor_Fill")
                {
                    context.FillRectangle(BrushFor(command.Color), destination);
                    return;
                }

                if (material == "Reactor_Cursor" || material == "Reactor_CursorHand")
                {
                    DrawCursor(context, destination, command.Color, material == "Reactor_CursorHand");
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
            bool symbolic = command.Font == Symbols.Font;

            SeFontMetrics metrics = symbolic ? _symbols : _exact;
            GlyphAtlas page = symbolic ? _symbolAtlas : _atlas;

            string text = metrics == null ? command.Text : metrics.AsDrawn(command.Text);

            if (page != null && metrics != null && DrawGlyphs(context, command, text, metrics, page))
                return;

            // A symbol with no page behind it has no business falling back to a desktop typeface, which
            // would draw the letter the character happens to be - a literal "d" where a bin belongs. That
            // is worse than nothing, because it looks deliberate.
            if (symbolic)
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
        bool DrawGlyphs(DrawingContext context, DrawCommand command, string text,
            SeFontMetrics metrics, GlyphAtlas atlas)
        {
            float unit = metrics.EffectiveScale(command.Scale);
            if (unit <= 0f)
                return false;

            Bitmap page = atlas.Tinted(command.Color.R, command.Color.G, command.Color.B);

            double pen = command.Position.X;
            double top = command.Position.Y + metrics.CellTop * unit;
            char previous = '\0';

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
                    if (!metrics.TryGlyph(c, out glyph) && !metrics.TryReplacement(out glyph))
                    {
                        previous = c;
                        continue;
                    }

                    if (previous != '\0')
                        pen += metrics.Kerning(previous, c) * unit;

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
