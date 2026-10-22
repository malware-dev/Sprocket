using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Lumel.Text;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Measuring;
using Sprocket.Sim.Saves;
using Sprocket.Sim.Scenarios;
using Sprocket.Sim.Shell;
using Sprocket.Sim.Simulation;

namespace Sprocket.Sim
{
    /// <summary>The harness's entry point: a window to drive the editor in, or a picture, a dump, a profile or a suite from the command line.</summary>
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

            // What a saved world remembers of the runtime, which the save keeps in binary.
            if (args.Length > 1 && args[0] == "--state")
            {
                Console.Write(SavedState.Render(args[1]));
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

            // What the runtime costs a world per tick, apart from the rest of the profile because it takes minutes.
            if (args.Length > 0 && args[0] == "--runtime")
            {
                Console.Write(RuntimeReport.Render());
                return;
            }

            if (args.Length > 0 && args[0] == "--content")
            {
                Console.Write(ContentReport.Render());
                return;
            }

            // A scenario written out for the game: --blueprint <name> [folder], into the folder or the one SPROCKET_BLUEPRINTS names;
            // --checks <name> [first], its checks as rows for the Testing artifact, numbered from first.
            if (args.Length > 1 && args[0] == "--blueprint")
            {
                Console.WriteLine(Export.Blueprint(args[1], args.Length > 2 ? args[2] : null));
                return;
            }

            if (args.Length > 1 && args[0] == "--checks")
            {
                Console.Write(Export.Checks(args[1], args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1));
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

            // The automation panel's troubled and calm screens in each of a set of app colors, on the two large programmable
            // blocks and the corner LCD: --colors <folder> [zoom], into <colors>/<scene>-<screen>.png.
            if (args.Length > 1 && args[0] == "--colors")
            {
                var zoom = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1;
                foreach (var colors in PanelColors.All)
                {
                    Directory.CreateDirectory(Path.Combine(args[1], colors.Key));
                    foreach (var scene in new[] { "trouble", "all-normal" })
                    {
                        var status = StatusScenes.Make(scene, out var texts);
                        foreach (var screen in new[] { "programmable-block", "programmable-block-automatons", "corner-lcd" })
                        {
                            var shape = ScreenShape.Reference.First(one => one.Name == screen);
                            var face = new Face(shape.Name) { Shape = shape, Foreground = colors.Value[0], Background = colors.Value[1] };
                            var path = Path.Combine(args[1], colors.Key, scene + "-" + screen + ".png");
                            FaceShot.Save(path, face, zoom, () =>
                            {
                                var graphics = new SpriteGraphics(face.Surface, face.Sprites, null);
                                graphics.Begin();
                                new StatusPicture().Draw(graphics, status, texts, face.Foreground, face.Background, face.Padding, shape.Metres, 0, 0);
                            });
                            Console.WriteLine(path);
                        }
                    }
                }

                return;
            }

            // The automation panel in each of its scenes on every screen of the reference set, a picture of each page it takes
            // turns between, its pixels zoomed: --faces <folder> [zoom], into <scene>/<screen>.png, then <screen>-2.png and on.
            if (args.Length > 1 && args[0] == "--faces")
            {
                var zoom = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1;
                foreach (var scene in StatusScenes.Names)
                {
                    var status = StatusScenes.Make(scene, out var texts);
                    Directory.CreateDirectory(Path.Combine(args[1], scene));
                    foreach (ScreenShape shape in ScreenShape.Reference)
                    {
                        var picture = new StatusPicture();
                        for (var turn = 0; turn < picture.Pages; turn++)
                        {
                            var face = new Face(shape.Name) { Shape = shape };
                            var name = turn == 0 ? shape.Name : shape.Name + "-" + (turn + 1).ToString(CultureInfo.InvariantCulture);
                            var path = Path.Combine(args[1], scene, name + ".png");
                            var shown = turn;
                            FaceShot.Save(path, face, zoom, () =>
                            {
                                var graphics = new SpriteGraphics(face.Surface, face.Sprites, null);
                                graphics.Begin();
                                picture.Draw(graphics, status, texts, face.Foreground, face.Background, face.Padding, shape.Metres, shown, shown);
                            });
                            Console.WriteLine(path);
                        }
                    }
                }

                return;
            }

            // The automation panel turning from its first page to its second, a quarter, half and three quarters of the way, on
            // every screen of the reference set that has a second page: --slides <folder> [zoom], into <scene>/<screen>-25.png and on.
            if (args.Length > 1 && args[0] == "--slides")
            {
                var zoom = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1;
                foreach (var scene in StatusScenes.Names)
                {
                    var status = StatusScenes.Make(scene, out var texts);
                    Directory.CreateDirectory(Path.Combine(args[1], scene));
                    foreach (ScreenShape shape in ScreenShape.Reference)
                    {
                        // How many pages a screen has is known once it is laid out, so a screen with one loses its first picture.
                        var picture = new StatusPicture();
                        foreach (var slid in new[] { 25, 50, 75 })
                        {
                            var face = new Face(shape.Name) { Shape = shape };
                            var path = Path.Combine(args[1], scene, shape.Name + "-" + slid.ToString(CultureInfo.InvariantCulture) + ".png");
                            FaceShot.Save(path, face, zoom, () =>
                            {
                                var graphics = new SpriteGraphics(face.Surface, face.Sprites, null);
                                graphics.Begin();
                                picture.Look.Slid = slid / 100f;
                                picture.Draw(graphics, status, texts, face.Foreground, face.Background, face.Padding, shape.Metres, 1, 0);
                            });
                            if (picture.Pages < 2)
                            {
                                File.Delete(path);
                                break;
                            }

                            Console.WriteLine(path);
                        }
                    }
                }

                return;
            }

            // A markdown file drawn by the rich text element, in the game's font, for looking at what it can do:
            // --rich <file.md> <picture.png> <width> [tip|description|codex] [strong=RRGGBB] [emphasis=RRGGBB] [link=RRGGBB] [align=left|center|right] [fit=<height>]
            // A text panel's screen as the rich text app shows an empty panel, the full text or the brief one:
            // --panel <full.md> <brief.md> <picture.png> <width> <height> [ink=RRGGBB] [screen=RRGGBB] [fit] [code=<font>]
            if (args.Length > 5 && args[0] == "--panel")
            {
                Console.WriteLine(args[3] + ": " + PanelShot.Save(args[3], File.ReadAllText(args[1]), File.ReadAllText(args[2]),
                    float.Parse(args[4], CultureInfo.InvariantCulture), float.Parse(args[5], CultureInfo.InvariantCulture),
                    Hex(args, "ink="), Hex(args, "screen="), Array.IndexOf(args, "fit") > 0,
                    Text(args, "code=")));
                return;
            }

            if (args.Length > 3 && args[0] == "--rich")
            {
                var wide = float.Parse(args[3], CultureInfo.InvariantCulture);
                var style = Array.IndexOf(args, "tip") > 3 ? Styles.Tip
                    : Array.IndexOf(args, "codex") > 3 ? Styles.Codex
                    : Array.IndexOf(args, "description") > 3 ? Styles.Description
                    : null;
                var size = RichShot.Save(args[2], File.ReadAllText(args[1]), wide, style, Hex(args, "strong="), Hex(args, "emphasis="),
                    Hex(args, "link="), Aligned(args), Number(args, "fit="));
                Console.WriteLine("wrote " + args[2] + "   " + size.X.ToString(CultureInfo.InvariantCulture) + "x"
                                  + size.Y.ToString(CultureInfo.InvariantCulture));
                return;
            }

            // The notice shown over the terminal, as a player not let in sees it: --notice <file.png>.
            if (args.Length > 1 && args[0] == "--notice")
            {
                var texts = new Texts();
                var arguments = new Arguments();
                arguments.Set("construct", "Hailstone Station");
                Shot.Save(args[1], 1920, 1080, TerminalNotice.Staged(texts.TerminalForeign, arguments, texts.TerminalOk));
                Console.WriteLine("wrote " + args[1]);
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

                // The player an admin with creative tools on, who sees the world's settings: "admin".
                var admin = args.Length > from && args[from] == "admin";
                if (admin)
                    from += 1;

                // Blocks welded onto the demo ship before the script starts, a kind and a name each, on a rotor's head where
                // one is named: with rotor "Arm Rotor" with light "Arm Light" on "Arm Rotor".
                Rig with = null;
                for (var id = 900L; args.Length > from + 2 && args[from] == "with"; id++)
                {
                    with = with ?? Rig.Demo();
                    var on = args.Length > from + 4 && args[from + 3] == "on" ? args[from + 4] : null;
                    Weld(with, id, args[from + 1], args[from + 2], on);
                    from += on == null ? 3 : 5;
                }

                if (with != null && holding != null)
                    throw new ArgumentException("--shot takes 'holding' or 'with', not both: a held program names the demo ship's blocks.");

                string ended = with != null
                    ? Shot.Save(args[1], width, height, Script.Parse(args, from), EditorScene.On(with), culture, developer, admin)
                    : Shot.Save(args[1], width, height, Script.Parse(args, from), holding, culture, developer, admin);
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
            foreach (var arg in args)
            {
                if (arg == "--quiet" || arg == "--background")
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

        /// <summary>What follows <paramref name="name" /> in the argument starting with it, or null where none does.</summary>
        static string Text(string[] args, string name)
        {
            var given = Array.Find(args, a => a.StartsWith(name, StringComparison.Ordinal));
            return given?.Substring(name.Length);
        }

        /// <summary>The number an argument starting <paramref name="name" /> gives, or zero where none does.</summary>
        static float Number(string[] args, string name)
        {
            var given = Array.Find(args, a => a.StartsWith(name, StringComparison.Ordinal));
            return given == null ? 0f : float.Parse(given.Substring(name.Length), CultureInfo.InvariantCulture);
        }

        /// <summary>The alignment an <c>align=</c> argument names, or left where none does.</summary>
        static Alignment Aligned(string[] args)
        {
            var given = Array.Find(args, a => a.StartsWith("align=", StringComparison.Ordinal));
            return given == null
                ? Alignment.Left
                : (Alignment)Enum.Parse(typeof(Alignment), given.Substring(6), true);
        }

        /// <summary>
        /// Welds a block of <paramref name="kind" />, a panel, a light, a door, a billboard, a cockpit or a rotor, onto <paramref name="rig" /> as
        /// <paramref name="id" />, named <paramref name="name" />, on the head of the rotor named <paramref name="on" />, or on
        /// the ship itself for null.
        /// </summary>
        /// <exception cref="ArgumentException">The kind is none of the four, or no rotor is named <paramref name="on" />.</exception>
        static void Weld(Rig rig, long id, string kind, string name, string on)
        {
            if (on != null)
            {
                var rotor = rig.Fitted.OfType<Rotor>().FirstOrDefault(fitting => fitting.Name == on);
                if (rotor == null)
                    throw new ArgumentException($"--shot has no rotor called '{on}' to weld '{name}' on.");

                rig.OnHeadOf(rotor);
                Weld(rig, id, kind, name, null);
                rig.OnBase();
                return;
            }

            if (kind == "rotor")
                rig.Rotor(id, name);
            else if (kind == "panel")
                rig.Panel(id, name);
            else if (kind == "light")
                rig.Light(id, name);
            else if (kind == "door")
                rig.Door(id, name);
            else if (kind == "billboard")
                rig.Weld(new Billboard(id, name));
            else if (kind == "cockpit")
                rig.ScreenedCockpit(id, name);
            else
                throw new ArgumentException($"--shot cannot weld '{kind}'; it welds a panel, a light, a door, a billboard, a cockpit or a rotor.");
        }

        /// <summary>Whether a word begins a script step rather than naming a file.</summary>
        static bool IsStep(string word)
        {
            string bare = word.TrimStart('-');

            if (bare == "at" || bare == "tick" || bare == "drag" || bare == "carry" || bare == "move" || bare == "scroll"
                || bare == "type" || bare == "enter" || bare == "escape" || bare == "clear"
                || bare == "double" || bare == "tab" || bare == "left" || bare == "right" || bare == "up"
                || bare == "down")
                return true;

            return float.TryParse(bare, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
