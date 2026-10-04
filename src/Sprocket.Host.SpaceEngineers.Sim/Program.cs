using System.IO;
using System;
using System.Globalization;
using System.Text;
using Sprocket.Sim.Content;
using Sprocket.Sim.Measuring;
using Sprocket.Sim.Shell;
using Sprocket.Sim.Simulation;

namespace Sprocket.Sim
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Nothing is started for these: they are about a window some other run already put up.
            if (args.Length > 0 && args[0] == "--dismiss")
            {
                int sent = Attention.StandBack();
                Console.WriteLine(sent == 0
                    ? "no simulator window is open"
                    : "sent " + sent + " behind");
                return;
            }

            if (args.Length > 0 && args[0] == "--raise")
            {
                int raised = Attention.ComeForward();
                Console.WriteLine(raised == 0
                    ? "no simulator window is open"
                    : "brought " + raised + " forward");
                return;
            }

            // Answered without a window, because the question is about this machine rather than about the
            // screen: whether the game is here decides whether real letterforms and real block artwork
            // are on the table at all.
            if (args.Length > 0 && args[0] == "--profile")
            {
                Console.Write(StorageReport.Render());
                Console.WriteLine();
                Console.Write(RichTextReport.Render());
                Console.WriteLine();
                Console.Write(BindingReport.Render());
                return;
            }

            if (args.Length > 0 && args[0] == "--content")
            {
                Console.Write(ContentReport.Render());
                return;
            }

            // Decoding needs a framework only to write a PNG, which is a small reason to reach for one and a
            // smaller one than adding a second image encoder to this project.
            if (args.Length > 0 && args[0] == "--extract")
            {
                Console.Write(Extract.Run(args.Length > 1 ? args[1] : null));
                return;
            }

            if (args.Length > 2 && args[0] == "--suite")
            {
                Console.Write(Suite.Run(args[1], args[2]));
                return;
            }

            // A markdown file drawn by the rich text element, in the game's font, for looking at what it can do:
            // --rich <file.md> <picture.png> <width> [tip] [strong=RRGGBB] [emphasis=RRGGBB] [align=left|center|right] [fit=<height>]
            if (args.Length > 3 && args[0] == "--rich")
            {
                var wide = float.Parse(args[3], CultureInfo.InvariantCulture);
                var tip = Array.IndexOf(args, "tip") > 3;
                var size = RichShot.Save(args[2], File.ReadAllText(args[1]), wide, tip, Hex(args, "strong="),
                    Hex(args, "emphasis="), Aligned(args), Number(args, "fit="));
                Console.WriteLine("wrote " + args[2] + "   " + size.X.ToString(CultureInfo.InvariantCulture) + "x"
                                  + size.Y.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (args.Length > 1 && args[0] == "--shot")
            {

                // Defaulted to a size somebody actually has. The screen is a fixed number of UI units
                // that the game stretches to the monitor, so how big a letter lands is decided by this
                // number and by nothing in the layout. At the window's own 1280x690 the whole screen
                // draws at two-thirds, small enough that a hairline diagonal falls between the samples
                // and a capital N comes out broken - the harness inventing a defect, and a judgement
                // about legibility made at a size no player has.
                int width = 1920;
                int height = 1080;
                int from = 2;

                if (args.Length > 4 && args[2] == "at")
                {
                    width = int.Parse(args[3], CultureInfo.InvariantCulture);
                    height = int.Parse(args[4], CultureInfo.InvariantCulture);
                    from = 5;
                }

                // A program for the block to hold before the script starts, read from a file: "holding <file>".
                string holding = null;
                if (args.Length > from + 1 && args[from] == "holding")
                {
                    holding = File.ReadAllText(args[from + 1]);
                    from += 2;
                }

                // The language the screen speaks, as the mod's translation files name it: "in no".
                string culture = null;
                if (args.Length > from + 1 && args[from] == "in")
                {
                    culture = args[from + 1];
                    from += 2;
                }

                // The developer tools in the footer, as a Sprocket with developerMode on offers them: "developer".
                var developer = args.Length > from && args[from] == "developer";
                if (developer)
                    from += 1;

                string ended = Shot.Save(args[1], width, height, Script.Parse(args, from), holding, culture, developer);
                Console.WriteLine("wrote " + args[1] + "   " + width + "x" + height);
                Console.WriteLine("ended on: " + ended);
                return;
            }

            if (args.Length > 0 && args[0] == "--dump")
            {

                float width = 1920f;
                float height = 1080f;
                int from = 1;
                string into = null;

                // A path first, or "-" to say out loud. Without one the whole line is a script, which
                // keeps the bare --dump somebody types by hand working as it always has.
                if (args.Length > 1 && !IsStep(args[1]))
                {
                    into = args[1] == "-" ? null : args[1];
                    from = 2;
                }

                if (args.Length > from + 2 && args[from] == "at")
                {
                    width = float.Parse(args[from + 1], CultureInfo.InvariantCulture);
                    height = float.Parse(args[from + 2], CultureInfo.InvariantCulture);
                    from += 3;
                }

                string frame = FrameDump.Render(width, height, Script.Parse(args, from));

                if (into == null)
                {
                    Console.Write(frame);
                    return;
                }

                // Written without a byte order mark and with one kind of line ending, because these are
                // compared against each other and a difference has to mean the screen changed.
                File.WriteAllText(into, frame.Replace(Cr, string.Empty), new UTF8Encoding(false));
                Console.WriteLine("wrote " + into + "   " + width + "x" + height);
                return;
            }

            // Opened to be looked at unless told otherwise. Most runs are a check somebody will read as
            // a screenshot instead, and those have no business taking the keyboard off whoever is
            // working - so they say so, and can be called forward later with --raise.
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--quiet" || args[i] == "--background")
                    MainWindow.OpensQuietly = true;
            }

            new App().Run();
        }

        static readonly string Cr = ((char)13).ToString();

        /// <summary>The color an argument starting <paramref name="name" /> gives as six hex digits, or null where none does.</summary>
        static VRageMath.Color? Hex(string[] args, string name)
        {
            var given = Array.Find(args, a => a.StartsWith(name, StringComparison.Ordinal));
            if (given == null)
                return null;

            var rgb = int.Parse(given.Substring(name.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new VRageMath.Color((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        }

        /// <summary>The number an argument starting <paramref name="name" /> gives, or zero where none does.</summary>
        static float Number(string[] args, string name)
        {
            var given = Array.Find(args, a => a.StartsWith(name, StringComparison.Ordinal));
            return given == null ? 0f : float.Parse(given.Substring(name.Length), CultureInfo.InvariantCulture);
        }

        /// <summary>The alignment an <c>align=</c> argument names, or left where none does.</summary>
        static Lumel.Text.Alignment Aligned(string[] args)
        {
            var given = Array.Find(args, a => a.StartsWith("align=", StringComparison.Ordinal));
            return given == null
                ? Lumel.Text.Alignment.Left
                : (Lumel.Text.Alignment)Enum.Parse(typeof(Lumel.Text.Alignment), given.Substring(6), true);
        }

        /// <summary>Whether a word begins a script step rather than naming a file.</summary>
        static bool IsStep(string word)
        {
            string bare = word.TrimStart('-');

            if (bare == "at" || bare == "tick" || bare == "drag" || bare == "move" || bare == "scroll"
                || bare == "type" || bare == "enter" || bare == "escape" || bare == "clear"
                || bare == "double" || bare == "tab" || bare == "left" || bare == "right" || bare == "up"
                || bare == "down")
                return true;

            return float.TryParse(bare, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
