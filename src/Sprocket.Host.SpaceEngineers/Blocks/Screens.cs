using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.GUI.TextPanel;
using VRageMath;

// The mod-side provider adds nothing to the one in the Ingame namespace, and some blocks are given only that one.
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with several screens a player can set up: what each shows and how, every member naming its screen.</summary>
    /// <remarks>
    /// The game's own settings act on whichever screen was picked last in the terminal, so they are left to the terminal
    /// view. Each is read here as a function whose screen option says which screen, and set through set up screen, since a
    /// function is not set.
    /// </remarks>
    public sealed class Screens : Capability
    {
        /// <summary>Setting up how one of the screens shows what it shows, which is Sprocket's own.</summary>
        public const string SetUp = "set-up-screen";

        const string Unheard = "A screen raises no event when its settings change.";

        // What a screen shows and how its text lines up, each word beside the game's value for it.
        static readonly string[] Contents = { "nothing", "text and images", "app" };
        static readonly ContentType[] ContentTypes = { ContentType.NONE, ContentType.TEXT_AND_IMAGE, ContentType.SCRIPT };
        static readonly string[] Alignments = { "left", "center", "right" };
        static readonly TextAlignment[] TextAlignments = { TextAlignment.LEFT, TextAlignment.CENTER, TextAlignment.RIGHT };

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            var screens = block as IMyTextSurfaceProvider;
            return screens != null && screens.UseGenericLcd && !(block is IMyTextPanel) && screens.SurfaceCount > 1;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var screens = (IMyTextSurfaceProvider)block;
            var names = Names(screens);
            Func<IList<Literal>, Picked> picked = arguments => Picked.Read(arguments, names);

            kind.Hiding(Screen.Settings)
                .WithAction(Screen.ShowText, "show text", Doing.Of<IMyTextSurfaceProvider, Shown>(
                        arguments => Shown.Read(arguments, names), Show), Faces(names), Lines())
                .In(Category.Screen)
                .WithAction(SetUp, "set up screen", Doing.Of<IMyTextSurfaceProvider, Look>(
                        arguments => Look.Read(arguments, names), Apply), Asked(names, Fonts(screens)))
                .In(Category.Screen);

            Value(kind, "content", Being.OneOf<IMyTextSurfaceProvider, Picked>(Contents, picked,
                (provider, which, seen) => Position(ContentTypes, Surface(provider, which).ContentType)), names);
            Value(kind, "text color", Being.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).FontColor), names);
            Value(kind, "background color", Being.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).BackgroundColor), names);
            Value(kind, "font", Being.Text<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).Font), names);
            Value(kind, "font size", Being.Number<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).FontSize), names);
            Value(kind, "alignment", Being.OneOf<IMyTextSurfaceProvider, Picked>(Alignments, picked,
                (provider, which, seen) => Position(TextAlignments, Surface(provider, which).Alignment)), names);
            Value(kind, "padding", Being.Number<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).TextPadding), names);
            Value(kind, "app color", Being.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).ScriptForegroundColor), names);
            Value(kind, "app background color", Being.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).ScriptBackgroundColor), names);
            Value(kind, "image interval", Being.Number<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).ChangeInterval), names);
            Value(kind, "keep aspect ratio", Being.Flag<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).PreserveAspectRatio), names);
        }

        /// <summary>Adds a reading of one screen's setting, called <paramref name="said" />, taking which screen.</summary>
        static void Value(EntityKind kind, string said, Being being, IList<string> names)
        {
            kind.WithValue(said.Replace(' ', '-'), said, being.Taking(Faces(names))).Looked(Unheard).In(Category.Screen);
        }

        /// <summary>The names of the block's screens, as the game gives them, or numbered where it gives none.</summary>
        static IList<string> Names(IMyTextSurfaceProvider screens)
        {
            var names = new string[screens.SurfaceCount];
            for (var i = 0; i < names.Length; i++)
            {
                var surface = screens.GetSurface(i);
                var name = surface == null ? null : surface.DisplayName;
                names[i] = string.IsNullOrEmpty(name) ? "screen " + (i + 1) : name;
            }

            return names;
        }

        /// <summary>The fonts a screen offers, which are the same on every screen.</summary>
        static IList<string> Fonts(IMyTextSurfaceProvider screens)
        {
            var fonts = new List<string>();
            var surface = screens.GetSurface(0);
            if (surface != null)
                surface.GetFonts(fonts);

            return fonts;
        }

        /// <summary>The screen a member means, among <paramref name="names" />.</summary>
        static Field Faces(IList<string> names)
        {
            var screen = Field.Among("screen", names, names);
            screen.Entry = Entry.Text;
            screen.Needed = true;
            return screen;
        }

        /// <summary>The text to show.</summary>
        static Field Lines()
        {
            var text = Field.Called("text");
            text.Entry = Entry.Text;
            text.Edits = Editor.Lines;
            text.Longest = Screen.TextLongest;
            text.Needed = true;
            return text;
        }

        /// <summary>The screen to set up, then each setting, every one left as it is where it is left empty.</summary>
        static Field[] Asked(IList<string> names, IList<string> fonts)
        {
            var font = fonts.Count > 0 ? Field.Among("font", fonts, fonts) : Field.Called("font");
            font.Entry = Entry.Text;

            return new[]
            {
                Faces(names), Field.Among("content", Contents, Contents), Field.Holding("text color", Editor.Colour, 1),
                Field.Holding("background color", Editor.Colour, 1), font, Decimal("font size", "0.1", "10"),
                Field.Among("alignment", Alignments, Alignments), Decimal("padding", "0", "50"),
                Field.Holding("app color", Editor.Colour, 1), Field.Holding("app background color", Editor.Colour, 1),
                Decimal("image interval", "0", "30"), Switch("keep aspect ratio")
            };
        }

        /// <summary>A number with a fraction, called <paramref name="name" />, between <paramref name="least" /> and <paramref name="most" />.</summary>
        static Field Decimal(string name, string least, string most)
        {
            var field = Field.Between(name, least, most);
            field.Entry = Entry.Decimal;
            return field;
        }

        /// <summary>An on or off, called <paramref name="name" />.</summary>
        static Field Switch(string name)
        {
            var field = Field.OneOf(name, Literal.Of(true).Said, Literal.Of(false).Said);
            field.Edits = Editor.Chosen;
            return field;
        }

        /// <summary>The screen <paramref name="which" /> names, or the first where it names none.</summary>
        static IMyTextSurface Surface(IMyTextSurfaceProvider provider, Picked which)
        {
            return provider.GetSurface(which.Screen < provider.SurfaceCount ? which.Screen : 0);
        }

        /// <summary>Where <paramref name="value" /> is among <paramref name="values" />, or the second where it is not there.</summary>
        /// <remarks>A content type with no word here, such as the image content the game marks obsolete, reads as text and images, the second.</remarks>
        static int Position<T>(T[] values, T value)
        {
            var at = Array.IndexOf(values, value);
            return at < 0 ? 1 : at;
        }

        /// <summary>Writes <paramref name="shown" />'s text on its screen, whatever the screen is set to show.</summary>
        /// <remarks>
        /// The text alone: a screen set to show nothing or an app keeps that, so a mod drawing the text itself, rich text from
        /// a screen's own text, is left to do so. Setting it to show text is "set up screen"'s to do.
        /// </remarks>
        static void Show(IMyTextSurfaceProvider provider, Shown shown, Recall seen)
        {
            Surface(provider, shown.Which).WriteText(shown.Text);
        }

        /// <summary>Sets each setting <paramref name="look" /> was given on its screen, and leaves the rest.</summary>
        static void Apply(IMyTextSurfaceProvider provider, Look look, Recall seen)
        {
            var surface = Surface(provider, look.Which);
            if (look.Content >= 0)
                surface.ContentType = ContentTypes[look.Content];
            if (look.TextColor.HasValue)
                surface.FontColor = look.TextColor.Value;
            if (look.BackgroundColor.HasValue)
                surface.BackgroundColor = look.BackgroundColor.Value;
            if (look.Font != null)
                surface.Font = look.Font;
            if (look.FontSize.HasValue)
                surface.FontSize = look.FontSize.Value;
            if (look.Alignment >= 0)
                surface.Alignment = TextAlignments[look.Alignment];
            if (look.Padding.HasValue)
                surface.TextPadding = look.Padding.Value;
            if (look.AppColor.HasValue)
                surface.ScriptForegroundColor = look.AppColor.Value;
            if (look.AppBackgroundColor.HasValue)
                surface.ScriptBackgroundColor = look.AppBackgroundColor.Value;
            if (look.Interval.HasValue)
                surface.ChangeInterval = look.Interval.Value;
            if (look.KeepsAspectRatio.HasValue)
                surface.PreserveAspectRatio = look.KeepsAspectRatio.Value;
        }

        /// <summary>The screen a member was given.</summary>
        sealed class Picked
        {
            /// <summary>Where the screen is among the block's, counted from 0.</summary>
            public int Screen;

            /// <summary>The screen named first among <paramref name="arguments" />, among <paramref name="names" />.</summary>
            public static Picked Read(IList<Literal> arguments, IList<string> names)
            {
                var name = arguments != null && arguments.Count > 0 ? arguments[0].Text : null;
                var at = name == null ? -1 : names.IndexOf(name);
                return new Picked { Screen = at < 0 ? 0 : at };
            }
        }

        /// <summary>The screen a step shows text on, and the text.</summary>
        sealed class Shown
        {
            /// <summary>The screen.</summary>
            public Picked Which;

            /// <summary>What is shown.</summary>
            public string Text = "";

            /// <summary>What a step showing text was given.</summary>
            public static Shown Read(IList<Literal> arguments, IList<string> names)
            {
                return new Shown
                {
                    Which = Picked.Read(arguments, names),
                    Text = arguments != null && arguments.Count > 1 ? arguments[1].Text ?? "" : ""
                };
            }
        }

        /// <summary>The screen a step sets up, and each setting it was given.</summary>
        sealed class Look
        {
            /// <summary>The screen.</summary>
            public Picked Which;

            /// <summary>Where what it shows is among <see cref="Contents" />, or -1 to leave it.</summary>
            public int Content = -1;

            /// <summary>The color of its text, or null to leave it.</summary>
            public Color? TextColor;

            /// <summary>The color behind its text, or null to leave it.</summary>
            public Color? BackgroundColor;

            /// <summary>The font its text is in, or null to leave it.</summary>
            public string Font;

            /// <summary>How large its text is, or null to leave it.</summary>
            public float? FontSize;

            /// <summary>Where its alignment is among <see cref="Alignments" />, or -1 to leave it.</summary>
            public int Alignment = -1;

            /// <summary>The space around its text, in percent, or null to leave it.</summary>
            public float? Padding;

            /// <summary>The color an app draws in, or null to leave it.</summary>
            public Color? AppColor;

            /// <summary>The color behind an app, or null to leave it.</summary>
            public Color? AppBackgroundColor;

            /// <summary>How long each image shows, in seconds, or null to leave it.</summary>
            public float? Interval;

            /// <summary>Whether an image keeps its shape, or null to leave it.</summary>
            public bool? KeepsAspectRatio;

            /// <summary>What a step setting up a screen was given, in the order <see cref="Screens.Asked" /> asks for it.</summary>
            public static Look Read(IList<Literal> arguments, IList<string> names)
            {
                var look = new Look { Which = Picked.Read(arguments, names) };
                look.Content = Chosen(arguments, 1, Contents);
                look.TextColor = Colored(arguments, 2);
                look.BackgroundColor = Colored(arguments, 3);
                look.Font = Given(arguments, 4) ? arguments[4].Text : null;
                look.FontSize = Number(arguments, 5);
                look.Alignment = Chosen(arguments, 6, Alignments);
                look.Padding = Number(arguments, 7);
                look.AppColor = Colored(arguments, 8);
                look.AppBackgroundColor = Colored(arguments, 9);
                look.Interval = Number(arguments, 10);
                look.KeepsAspectRatio = Given(arguments, 11) && arguments[11].Kind == ValueKind.Flag
                    ? arguments[11].Flag
                    : (bool?)null;
                return look;
            }

            /// <summary>Whether the setting at <paramref name="at" /> was given, which an empty one was not.</summary>
            static bool Given(IList<Literal> arguments, int at)
            {
                return arguments != null && at < arguments.Count
                    && !(arguments[at].Kind == ValueKind.Text && string.IsNullOrEmpty(arguments[at].Text));
            }

            static int Chosen(IList<Literal> arguments, int at, string[] words)
            {
                return Given(arguments, at) ? Array.IndexOf(words, arguments[at].Text) : -1;
            }

            static Color? Colored(IList<Literal> arguments, int at)
            {
                if (!Given(arguments, at) || arguments[at].Kind != ValueKind.Color)
                    return null;

                var given = arguments[at];
                return new Color(given.Red, given.Green, given.Blue, given.Alpha);
            }

            static float? Number(IList<Literal> arguments, int at)
            {
                if (!Given(arguments, at) || (arguments[at].Kind != ValueKind.Real && arguments[at].Kind != ValueKind.Integer))
                    return null;

                return (float)arguments[at].Real;
            }
        }
    }
}
