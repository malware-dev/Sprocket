using System.Globalization;
using System.Text;
using Avalonia;
using Reactor.Sim.Content;
using Reactor.Sim.Measuring;
using Reactor.Sim.Shell;
using Reactor.Sim.Simulation;

namespace Reactor.Sim
{
    static class Program
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

            // Answered without Avalonia, because the question is about this machine rather than about the
            // screen: whether the game is here decides whether real letterforms and real block artwork
            // are on the table at all.
            if (args.Length > 0 && args[0] == "--profile")
            {
                Console.Write(StorageReport.Render());
                return;
            }

            if (args.Length > 0 && args[0] == "--content")
            {
                Console.Write(ContentReport.Render());
                return;
            }

            // Decoding needs Avalonia only to write a PNG, which is a small reason to start it and a
            // smaller one than adding a second image encoder to this project.
            if (args.Length > 0 && args[0] == "--extract")
            {
                BuildAvaloniaApp().SetupWithoutStarting();
                Console.Write(Extract.Run(args.Length > 1 ? args[1] : null));
                return;
            }

            if (args.Length > 2 && args[0] == "--suite")
            {
                BuildAvaloniaApp().SetupWithoutStarting();
                Console.Write(Suite.Run(args[1], args[2]));
                return;
            }

            if (args.Length > 1 && args[0] == "--shot")
            {
                BuildAvaloniaApp().SetupWithoutStarting();

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

                string ended = Shot.Save(args[1], width, height, Script.Parse(args, from));
                Console.WriteLine("wrote " + args[1] + "   " + width + "x" + height);
                Console.WriteLine("ended on: " + ended);
                return;
            }

            if (args.Length > 0 && args[0] == "--dump")
            {
                // Avalonia is still set up, because text has to be measured and only it has a font. It is
                // simply never started, so nothing is shown and nothing waits for a person.
                BuildAvaloniaApp().SetupWithoutStarting();

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

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>The carriage return that a dump written for comparison does without.</summary>
        static readonly string Cr = ((char)13).ToString();

        /// <summary>Whether a word begins a script step rather than naming a file.</summary>
        static bool IsStep(string word)
        {
            string bare = word.TrimStart('-');

            if (bare == "at" || bare == "tick" || bare == "drag" || bare == "move" || bare == "scroll"
                || bare == "type" || bare == "enter" || bare == "escape" || bare == "clear")
                return true;

            float ignored;
            return float.TryParse(bare, NumberStyles.Float, CultureInfo.InvariantCulture, out ignored);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
