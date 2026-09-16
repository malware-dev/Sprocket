using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Reactor.Sim.Content;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// The game's own letterforms, ready to be blitted a glyph at a time.
    /// </summary>
    /// <remarks>
    /// The last approximation in the drawing path. Widths were already the game's own, taken from the
    /// font file, and only the shapes were a desktop face stretched sideways onto them - which was the
    /// right trade while it lasted, since a wrong width is invisible here and expensive in the game while
    /// a wrong shape is merely ugly. With the page decoded there is no trade left to make.
    /// <para>
    /// The page stores coverage in the alpha channel and its colour tracks it, so a tint is "alpha as the
    /// mask, colour from the palette". Tinted copies are made once per colour and kept, which costs four
    /// megabytes each and saves doing it per glyph per frame; a screen uses under a dozen colours and
    /// this is a development tool on a desktop.
    /// </para>
    /// </remarks>
    public sealed class GlyphAtlas
    {
        readonly int _width;
        readonly int _height;
        readonly byte[] _coverage;
        readonly Dictionary<uint, BitmapSource> _tinted = new Dictionary<uint, BitmapSource>();

        GlyphAtlas(int width, int height, byte[] coverage)
        {
            _width = width;
            _height = height;
            _coverage = coverage;
        }

        /// <summary>Where the page came from, for the line at the bottom of the window.</summary>
        public string Source { get; private set; }

        /// <summary>
        /// Loads the page a font file names, or answers null where it cannot be had.
        /// </summary>
        /// <remarks>
        /// Null rather than throwing. A machine without the game and without a cached page is the case
        /// this whole arrangement exists to survive, and it falls back to a desktop face that has been
        /// good enough to build the entire screen against.
        /// </remarks>
        public static GlyphAtlas Load(string pageFile)
        {
            if (string.IsNullOrEmpty(pageFile))
                return null;

            string relative = "Fonts/white/" + pageFile;
            string png;

            try
            {
                png = GameImages.Resolve(relative);
            }
            catch (Exception)
            {
                // A decoder that will not run is a reason to fall back, not a reason to stop. It is only
                // ever reachable on a machine that has the game, where the fallback is still correct.
                return null;
            }

            if (png == null || !File.Exists(png))
                return null;

            var decoded = new FormatConvertedBitmap(
                new BitmapImage(new Uri(png, UriKind.Absolute)), PixelFormats.Bgra32, null, 0d);

            int width = decoded.PixelWidth;
            int height = decoded.PixelHeight;

            var bgra = new byte[width * height * 4];
            decoded.CopyPixels(bgra, width * 4, 0);

            // Only the alpha is kept. The colour on the page is the same coverage said again, and
            // holding a megabyte of it to throw away on every tint would be keeping the wrong copy.
            var coverage = new byte[width * height];
            for (int i = 0; i < coverage.Length; i++)
                coverage[i] = bgra[i * 4 + 3];

            var atlas = new GlyphAtlas(width, height, coverage);
            atlas.Source = png;
            return atlas;
        }

        /// <summary>
        /// Loads a page straight out of a DDS, for a font this project ships rather than borrows.
        /// </summary>
        /// <remarks>
        /// No cache in front of it, unlike the game's own page. That one is decoded once and kept because
        /// the machine reading it may not have the game at all; this one is in the repository, is a
        /// fortieth of the size, and decoding it costs less than deciding whether to.
        /// </remarks>
        public static GlyphAtlas FromDds(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            DdsImage.Pixels pixels;
            try
            {
                pixels = DdsImage.Read(path);
            }
            catch (Exception)
            {
                return null;
            }

            if (pixels == null)
                return null;

            var coverage = new byte[pixels.Width * pixels.Height];
            for (int y = 0; y < pixels.Height; y++)
                for (int x = 0; x < pixels.Width; x++)
                    coverage[y * pixels.Width + x] = pixels.Bgra[y * pixels.Stride + x * 4 + 3];

            var atlas = new GlyphAtlas(pixels.Width, pixels.Height, coverage);
            atlas.Source = path;
            return atlas;
        }

        /// <summary>The page in one colour, made the first time that colour is asked for.</summary>
        public BitmapSource Tinted(byte r, byte g, byte b)
        {
            uint key = (uint)((r << 16) | (g << 8) | b);

            BitmapSource cached;
            if (_tinted.TryGetValue(key, out cached))
                return cached;

            var bitmap = new WriteableBitmap(_width, _height, 96d, 96d, PixelFormats.Bgra32, null);

            bitmap.Lock();
            try
            {
                unsafe
                {
                    var row = (byte*)bitmap.BackBuffer;

                    for (int y = 0; y < _height; y++)
                    {
                        int from = y * _width;

                        for (int x = 0; x < _width; x++)
                        {
                            byte* pixel = row + x * 4;
                            pixel[0] = b;
                            pixel[1] = g;
                            pixel[2] = r;
                            pixel[3] = _coverage[from + x];
                        }

                        row += bitmap.BackBufferStride;
                    }
                }

                bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            }
            finally
            {
                bitmap.Unlock();
            }

            // Frozen, so the canvas can draw it from whatever thread asks and nothing has to copy it
            // again: a page is four megabytes and a colour is asked for once.
            bitmap.Freeze();

            _tinted.Add(key, bitmap);
            return bitmap;
        }
    }
}
