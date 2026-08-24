using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Reactor.Sim.Content;
using Reactor.Sim.Shell;
using Reactor.Sim.Simulation;
using VRageMath;

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
                int sent = Shell.Attention.StandBack();
                Console.WriteLine(sent == 0
                    ? "no simulator window is open"
                    : "sent " + sent + " behind");
                return;
            }

            if (args.Length > 0 && args[0] == "--raise")
            {
                int raised = Shell.Attention.ComeForward();
                Console.WriteLine(raised == 0
                    ? "no simulator window is open"
                    : "brought " + raised + " forward");
                return;
            }

            // Answered without Avalonia, because the question is about this machine rather than about the
            // screen: whether the game is here decides whether real letterforms and real block artwork
            // are on the table at all.
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

            if (args.Length > 1 && args[0] == "--shot")
            {
                BuildAvaloniaApp().SetupWithoutStarting();

                // The size is worth being able to say, and worth defaulting to one somebody actually
                // has. The screen is a fixed number of UI units that the game stretches to the monitor,
                // so how big a letter lands is decided by this number and by nothing in the layout. The
                // default used to be the window's own 1280x690, which draws the whole screen at
                // two-thirds - small enough that a hairline diagonal in the font falls between the
                // samples and a capital N comes out broken. That is the harness inventing a defect, and
                // a judgement about legibility made at a size no player has is a judgement about
                // nothing.
                int width = 1920;
                int height = 1080;
                int from = 2;

                if (args.Length > 4 && args[2] == "at")
                {
                    width = int.Parse(args[3], CultureInfo.InvariantCulture);
                    height = int.Parse(args[4], CultureInfo.InvariantCulture);
                    from = 5;
                }

                string ended = Shot.Save(args[1], width, height, Script(args, from));
                Console.WriteLine("wrote " + args[1] + "   " + width + "x" + height);
                Console.WriteLine("ended on: " + ended);
                return;
            }

            if (args.Length > 0 && args[0] == "--dump")
            {
                // Avalonia is still set up, because text has to be measured and only it has a font. It is
                // simply never started, so nothing is shown and nothing waits for a person.
                BuildAvaloniaApp().SetupWithoutStarting();
                Console.WriteLine(FrameDump.Render(1920f, 1080f));
                return;
            }

            // Opened to be looked at unless told otherwise. Most runs are a check somebody will read as
            // a screenshot instead, and those have no business taking the keyboard off whoever is
            // working - so they say so, and can be called forward later with --raise.
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--quiet" || args[i] == "--background")
                    Shell.MainWindow.OpensQuietly = true;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        /// Reads the rest of the command line as things to do to the screen, in order.
        /// </summary>
        /// <remarks>
        /// A bare pair of numbers is a press, because that is nearly every line of nearly every script
        /// and making the common case wordless is worth the small asymmetry. <c>tick n</c> advances the
        /// ship's clock, which is the only way to reach anything drawn because a system is actually
        /// running. <c>drag x y x y</c> picks something up and puts it down somewhere else,
        /// <c>scroll x y n</c> turns the wheel over a point, and <c>type "words"</c>, <c>enter</c> and
        /// <c>escape</c> work whatever box currently has the keyboard.
        /// </remarks>
        static List<Shot.Step> Script(string[] args, int from)
        {
            var script = new List<Shot.Step>();

            for (int i = from; i < args.Length; i++)
            {
                string word = args[i].TrimStart('-');

                if (word == "tick" && i + 1 < args.Length)
                {
                    var tick = new Shot.Step();
                    tick.Kind = Shot.Step.Tick;
                    tick.Count = int.Parse(args[i + 1], CultureInfo.InvariantCulture);

                    script.Add(tick);
                    i += 1;
                    continue;
                }

                if (word == "enter" || word == "escape")
                {
                    var key = new Shot.Step();
                    key.Kind = word == "enter" ? Shot.Step.Enter : Shot.Step.Escape;

                    script.Add(key);
                    continue;
                }

                if (word == "type" && i + 1 < args.Length)
                {
                    var typed = new Shot.Step();
                    typed.Kind = Shot.Step.Type;
                    typed.Text = args[i + 1];

                    script.Add(typed);
                    i += 1;
                    continue;
                }

                if (word == "scroll" && i + 3 < args.Length)
                {
                    var scroll = new Shot.Step();
                    scroll.Kind = Shot.Step.Scroll;
                    scroll.At = Point(args, i + 1);
                    scroll.Amount = float.Parse(args[i + 3], CultureInfo.InvariantCulture);

                    script.Add(scroll);
                    i += 3;
                    continue;
                }

                if (word == "drag" && i + 4 < args.Length)
                {
                    var drag = new Shot.Step();
                    drag.Kind = Shot.Step.Drag;
                    drag.At = Point(args, i + 1);
                    drag.To = Point(args, i + 3);

                    script.Add(drag);
                    i += 4;
                    continue;
                }

                if (i + 1 >= args.Length)
                    break;

                var press = new Shot.Step();
                press.Kind = Shot.Step.Press;
                press.At = Point(args, i);

                script.Add(press);
                i += 1;
            }

            return script;
        }

        static Vector2 Point(string[] args, int at)
        {
            return new Vector2(
                float.Parse(args[at], CultureInfo.InvariantCulture),
                float.Parse(args[at + 1], CultureInfo.InvariantCulture));
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
