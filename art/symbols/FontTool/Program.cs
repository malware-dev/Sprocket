using System;
using System.IO;
using DirectXTexNet;

namespace Maltek.FontTool
{
    /// <summary>
    /// Writes a glyph page as the texture the game loads.
    /// </summary>
    /// <remarks>
    /// The format is not a choice. Every font Space Engineers ships is BC7_UNORM_SRGB with a full mip
    /// chain and premultiplied alpha - checked against FontDataPA-0.dds rather than assumed - and a page
    /// in any other format either fails to load or draws with dark haloes round every glyph.
    /// <para>
    /// Premultiplying is the step that is easy to miss and hard to diagnose. Straight alpha looks correct
    /// in every image viewer and only goes wrong once the game blends it, at which point the symptom is
    /// "the artwork looks slightly dirty" rather than anything that points at a pipeline.
    /// </para>
    /// </remarks>
    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: FontTool <page.png> <page.dds>");
                Console.Error.WriteLine("       FontTool --straight <image.png> <image.dds>");
                Console.Error.WriteLine("       FontTool --check <page.dds> <back.png>");
                return 2;
            }

            // An LCD's own image is the one texture the game keeps with straight alpha: the TexturePath files
            // in LCDTextures.sbc are straight, and their SpritePath twins premultiplied like every font page.
            var straight = args[0] == "--straight";
            if (straight)
            {
                if (args.Length < 3)
                {
                    Console.Error.WriteLine("usage: FontTool --straight <image.png> <image.dds>");
                    return 2;
                }

                args = new[] { args[1], args[2] };
            }

            // Reading it back is worth a command of its own. A page with the premultiply the wrong way
            // round loads perfectly and looks perfectly fine in any viewer; the only symptom is a dark
            // fringe once the game blends it, which is a long way from here.
            if (args[0] == "--check")
            {
                if (args.Length < 3)
                {
                    Console.Error.WriteLine("usage: FontTool --check <page.dds> <back.png>");
                    return 2;
                }

                try
                {
                    Check(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
                    return 0;
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine("failed: " + error.Message);
                    return 1;
                }
            }

            string source = Path.GetFullPath(args[0]);
            string target = Path.GetFullPath(args[1]);

            if (!File.Exists(source))
            {
                Console.Error.WriteLine("no such file: " + source);
                return 2;
            }

            try
            {
                Convert(source, target, straight);
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("failed: " + error.Message);
                return 1;
            }
        }

        /// <summary>
        /// Decodes a page back to PNG the same way the simulator decodes the game's own fonts.
        /// </summary>
        /// <remarks>
        /// The same three steps in the same order - load, decompress, reverse the premultiply - so what
        /// comes out is what the simulator would draw with. Comparing it to what went in is the whole
        /// test: if the two agree, the pipeline is right in both directions.
        /// </remarks>
        static void Check(string source, string target)
        {
            ScratchImage image = TexHelper.Instance.LoadFromDDSFile(source, DDS_FLAGS.NONE);

            try
            {
                TexMetadata meta = image.GetMetadata();
                Console.WriteLine("read  {0}x{1}  {2}  {3} mips",
                    meta.Width, meta.Height, meta.Format, meta.MipLevels);

                ScratchImage flat = image.Decompress(DXGI_FORMAT.B8G8R8A8_UNORM);
                image.Dispose();
                image = flat;

                // Guarded exactly the way the simulator guards it, and the guard matters: the game's own
                // pages are premultiplied but declare their alpha mode as unknown, so this never fires for
                // them either. Reversing regardless throws rather than being quietly wrong, which is at
                // least an honest failure - but matching the simulator is the point, since what this
                // writes has to come back through that path.
                if (image.GetMetadata().GetAlphaMode() == TEX_ALPHA_MODE.PREMULTIPLIED)
                {
                    ScratchImage straight = image.PremultiplyAlpha(TEX_PMALPHA_FLAGS.REVERSE);
                    image.Dispose();
                    image = straight;
                    Console.WriteLine("  reversed the premultiply (the file declared it)");
                }
                else
                {
                    Console.WriteLine("  left as stored (alpha mode not declared, same as the game's own)");
                }

                image.SaveToWICFile(0, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(WICCodecs.PNG), target);
                Console.WriteLine("wrote {0}", target);
            }
            finally
            {
                image.Dispose();
            }
        }

        static void Convert(string source, string target, bool straight)
        {
            ScratchImage image = TexHelper.Instance.LoadFromWICFile(source, WIC_FLAGS.IGNORE_SRGB);

            try
            {
                // Premultiplied unless --straight. The page is authored with straight alpha, and this is the one
                // place that has to know the game wants it the other way.
                if (!straight)
                {
                    ScratchImage premultiplied = image.PremultiplyAlpha(TEX_PMALPHA_FLAGS.DEFAULT);
                    image.Dispose();
                    image = premultiplied;
                }

                // A full chain, because the game draws these small. Without mips a symbol shrunk to a
                // third of its cell samples whichever texels it lands on, which is how a ring becomes a
                // dotted circle - the same defect the simulator had before it asked for proper sampling.
                ScratchImage mipped = image.GenerateMipMaps(TEX_FILTER_FLAGS.FANT, 0);
                image.Dispose();
                image = mipped;

                ScratchImage compressed = image.Compress(
                    DXGI_FORMAT.BC7_UNORM_SRGB, TEX_COMPRESS_FLAGS.PARALLEL, 1f);
                image.Dispose();
                image = compressed;

                Directory.CreateDirectory(Path.GetDirectoryName(target));

                // Beside itself and moved, so a run that dies halfway cannot leave a half-written page
                // where the next build will trust it.
                string partial = target + ".partial";
                image.SaveToDDSFile(DDS_FLAGS.NONE, partial);

                if (File.Exists(target))
                    File.Delete(target);

                File.Move(partial, target);

                TexMetadata written = image.GetMetadata();
                Console.WriteLine("wrote {0}", target);
                Console.WriteLine("  {0}x{1}  {2}  {3} mips  {4} bytes",
                    written.Width, written.Height, written.Format, written.MipLevels,
                    new FileInfo(target).Length);
            }
            finally
            {
                image.Dispose();
            }
        }
    }
}
