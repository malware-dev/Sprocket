using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Reactor.Sim.Content
{
    /// <summary>
    /// Hands back the game's artwork as PNG, decoding it once on a machine that has the game.
    /// </summary>
    /// <remarks>
    /// The shape <see cref="GameContent" /> describes, applied to pictures: a step that runs where the
    /// game is, producing a file that stands in where it is not. The DDS is never cached - only what
    /// comes out of it - so the machine without the game needs no decoder, no native library and no
    /// knowledge that block compression exists.
    /// <para>
    /// It is also simply faster. A font page is two thousand pixels square and block-compressed, and
    /// unpacking it on every start to arrive at the same answer would be work done sixty times to be
    /// thrown away fifty-nine.
    /// </para>
    /// </remarks>
    public static class GameImages
    {
        /// <summary>
        /// A path to <paramref name="relativePath" /> as a PNG, or null where neither the cache nor the
        /// game can supply it.
        /// </summary>
        /// <param name="relativePath">Where the DDS lives under the game's Content folder.</param>
        public static string Resolve(string relativePath)
        {
            string cached = CachedPath(relativePath);
            if (File.Exists(cached))
                return cached;

            string content = GameContent.ContentFolder;
            if (content == null)
                return null;

            string source = Path.Combine(content, relativePath);
            if (!File.Exists(source))
                return null;

            DdsImage.Pixels pixels = DdsImage.Read(source);
            if (pixels == null)
                return null;

            Directory.CreateDirectory(Path.GetDirectoryName(cached));
            Write(pixels, cached);
            return cached;
        }

        /// <summary>Where the decoded copy of <paramref name="relativePath" /> lives, whether or not it exists.</summary>
        public static string CachedPath(string relativePath)
        {
            string png = Path.ChangeExtension(relativePath, ".png");
            return Path.Combine(GameContent.CacheDirectory, png);
        }

        /// <summary>Whether this machine could produce <paramref name="relativePath" /> if asked.</summary>
        public static bool CanExtract(string relativePath)
        {
            string content = GameContent.ContentFolder;
            return content != null && File.Exists(Path.Combine(content, relativePath));
        }

        static void Write(DdsImage.Pixels pixels, string path)
        {
            var size = new PixelSize(pixels.Width, pixels.Height);

            using (var bitmap = new WriteableBitmap(
                size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul))
            {
                using (ILockedFramebuffer buffer = bitmap.Lock())
                {
                    // Row by row rather than one copy, because the two strides need not agree: the
                    // decoder packs its rows tight and the bitmap is free to pad them.
                    for (int y = 0; y < pixels.Height; y++)
                    {
                        IntPtr destination = buffer.Address + y * buffer.RowBytes;
                        System.Runtime.InteropServices.Marshal.Copy(
                            pixels.Bgra, y * pixels.Stride, destination, pixels.Width * 4);
                    }
                }

                // Written beside itself and moved into place, so a run that dies halfway through cannot
                // leave a half-written page in the cache to be trusted on the next start.
                string partial = path + ".partial";
                bitmap.Save(partial);

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(partial, path);
            }
        }
    }
}
