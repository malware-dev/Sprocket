using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
        readonly Dictionary<uint, Bitmap> _tinted = new Dictionary<uint, Bitmap>();

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

            using (var bitmap = new Bitmap(png))
            {
                int width = bitmap.PixelSize.Width;
                int height = bitmap.PixelSize.Height;

                var bgra = new byte[width * height * 4];

                unsafe
                {
                    fixed (byte* into = bgra)
                        bitmap.CopyPixels(
                            new PixelRect(0, 0, width, height), (IntPtr)into, bgra.Length, width * 4);
                }

                // Only the alpha is kept. The colour on the page is the same coverage said again, and
                // holding a megabyte of it to throw away on every tint would be keeping the wrong copy.
                var coverage = new byte[width * height];
                for (int i = 0; i < coverage.Length; i++)
                    coverage[i] = bgra[i * 4 + 3];

                var atlas = new GlyphAtlas(width, height, coverage);
                atlas.Source = png;
                return atlas;
            }
        }

        /// <summary>The page in one colour, made the first time that colour is asked for.</summary>
        public Bitmap Tinted(byte r, byte g, byte b)
        {
            uint key = (uint)((r << 16) | (g << 8) | b);

            Bitmap cached;
            if (_tinted.TryGetValue(key, out cached))
                return cached;

            var bitmap = new WriteableBitmap(
                new PixelSize(_width, _height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);

            using (ILockedFramebuffer buffer = bitmap.Lock())
            {
                unsafe
                {
                    var row = (byte*)buffer.Address;

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

                        row += buffer.RowBytes;
                    }
                }
            }

            _tinted.Add(key, bitmap);
            return bitmap;
        }
    }
}
