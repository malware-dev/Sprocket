using System;
using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.GUI.TextPanel;

// The mod-side provider adds nothing to the one in the Ingame namespace, and some blocks provide only that one.
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block's screen, the type this host provides as <c>ext.screen</c>: show text on it, set it up, read its settings.</summary>
    /// <remarks>Each member receives its screen first, counted from 1, ahead of the step's own values.</remarks>
    public sealed partial class ScreenType : HostType
    {
        /// <summary>The action that writes text on a screen.</summary>
        public const string ShowText = "show-text";

        /// <summary>Setting up how a screen shows what it shows, which is Sprocket's own.</summary>
        public const string SetUp = "set-up-screen";

        /// <summary>How long the text on a screen may be: the game's own panels hold no more.</summary>
        public const int TextLongest = 100000;

        // The game's settings of a screen; on a block with several they act on the one last picked.
        static readonly string[] Settings =
        {
            "Content", "FontColor", "BackgroundColor", "Font", "FontSize", "alignment", "TextPaddingSlider",
            "ScriptForegroundColor", "ScriptBackgroundColor", "ChangeIntervalSlider", "PreserveAspectRatio"
        };

        const string Unheard = "A screen raises no event when its settings change.";

        // What a screen shows and how its text lines up, each word beside the game's value for it.
        static readonly string[] Contents = { "nothing", "text and images", "app" };
        static readonly ContentType[] ContentTypes = { ContentType.NONE, ContentType.TEXT_AND_IMAGE, ContentType.SCRIPT };
        static readonly string[] Alignments = { "left", "center", "right" };
        static readonly TextAlignment[] TextAlignments = { TextAlignment.LEFT, TextAlignment.CENTER, TextAlignment.RIGHT };

        /// <inheritdoc />
        public override string Id => $"{BlockKinds.Own}.{Function}";

        /// <inheritdoc />
        public override string Function => "screen";

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            var screens = block as IMyTextSurfaceProvider;
            return screens != null && (screens.UseGenericLcd || block is IMyTextPanel) && screens.SurfaceCount > 0;
        }

        /// <inheritdoc />
        public override void Offered(IMyTerminalBlock block, List<HostTypeFacet> into)
        {
            var names = Names((IMyTextSurfaceProvider)block);
            for (var i = 0; i < names.Count; i++)
                into.Add(new HostTypeFacet(i + 1, names[i]));
        }

        /// <inheritdoc />
        public override string Showing(IMyTerminalBlock block, int number)
        {
            var screens = block as IMyTextSurfaceProvider;
            if (screens == null || number < 1 || number > screens.SurfaceCount)
                return "";

            var screen = screens.GetSurface(number - 1);
            var content = Contents[Math.Max(0, Array.IndexOf(ContentTypes, screen.ContentType))];
            return screen.ContentType == ContentType.SCRIPT && !string.IsNullOrEmpty(screen.Script) ? $"{content} {AppName(screen.Script)}" : content;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var screens = (IMyTextSurfaceProvider)block;
            Func<IList<Literal>, Picked> picked = Picked.Read;

            kind.Hiding(Settings)
                .WithAction(ShowText, "show text", OwnAction.Of<IMyTextSurfaceProvider, Shown>(Shown.Read, Show), Lines())
                .In(Category.Screen).Of(this)
                .WithAction(SetUp, "set up screen", OwnAction.Of<IMyTextSurfaceProvider, Look>(Look.Read, Apply), Asked(Fonts(screens), Apps(screens)))
                .In(Category.Screen).Of(this);

            Value(kind, "content", OwnValue.OneOf<IMyTextSurfaceProvider, Picked>(Contents, picked,
                (provider, which, seen) => Position(ContentTypes, Surface(provider, which).ContentType)));
            Value(kind, "text color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).FontColor));
            Value(kind, "background color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).BackgroundColor));
            Value(kind, "font", OwnValue.Text<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).Font));
            Value(kind, "font size", OwnValue.Number<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).FontSize));
            Value(kind, "alignment", OwnValue.OneOf<IMyTextSurfaceProvider, Picked>(Alignments, picked,
                (provider, which, seen) => Position(TextAlignments, Surface(provider, which).Alignment)));
            Value(kind, "padding", OwnValue.Number<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).TextPadding));
            Value(kind, "app color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).ScriptForegroundColor));
            Value(kind, "app background color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).ScriptBackgroundColor));
            Value(kind, "image interval", OwnValue.Number<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).ChangeInterval));
            Value(kind, "keep aspect ratio", OwnValue.Flag<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which, seen) => Surface(provider, which).PreserveAspectRatio));
        }

        /// <summary>Adds a reading of one screen's setting, called <paramref name="said" />.</summary>
        void Value(EntityKind kind, string said, OwnValue being)
        {
            kind.WithValue(said.Replace(' ', '-'), said, being).Looked(Unheard).In(Category.Screen).Of(this);
        }

        /// <summary>The names of the block's screens, as the game names them, or numbered where it names none.</summary>
        public static IList<string> Names(IMyTextSurfaceProvider screens)
        {
            var names = new string[screens.SurfaceCount];
            for (var i = 0; i < names.Length; i++)
            {
                var surface = screens.GetSurface(i);
                var name = surface?.DisplayName;
                names[i] = string.IsNullOrEmpty(name) ? $"screen {(i + 1)}" : name;
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

        /// <summary>The apps a screen of <paramref name="screens" /> can run, by the game's ids, the ones mods add among them.</summary>
        static IList<string> Apps(IMyTextSurfaceProvider screens)
        {
            var apps = new List<string>();
            var surface = screens.GetSurface(0);
            if (surface != null)
                surface.GetScripts(apps);

            return apps;
        }

        /// <summary>What the game calls the app <paramref name="id" /> in its own lists, or the id where it names none.</summary>
        public static string AppName(string id)
        {
            var apps = MyTextSurfaceScriptFactory.Instance;
            MyTextSurfaceScriptFactory.ScriptInfo app;
            return apps != null && id != null && apps.Scripts.TryGetValue(id, out app) && MyTexts.Exists(app.DisplayName)
                ? MyTexts.GetString(app.DisplayName)
                : id;
        }

        /// <summary>The text to show.</summary>
        static Field Lines()
        {
            var text = Field.Called("text");
            text.Entry = FieldInput.Text;
            text.Edits = FieldEditor.Lines;
            text.Longest = TextLongest;
            text.Needed = true;
            return text;
        }

        /// <summary>Each setting of the screen to set up, every one left as it is where it is left empty.</summary>
        static Field[] Asked(IList<string> fonts, IList<string> apps)
        {
            var font = fonts.Count > 0 ? Field.Among("font", fonts, fonts) : Field.Called("font");
            font.Entry = FieldInput.Text;
            var named = new List<string>();
            foreach (var one in apps)
                named.Add(AppName(one));

            var app = apps.Count > 0 ? Field.Among("app", named, apps) : Field.Called("app");
            app.Entry = FieldInput.Text;

            // Content first, and each setting asked for while the content it applies to is chosen, as the terminal shows it.
            return new[]
            {
                Field.Among("content", Contents, Contents), Showing(app, Contents[2]),
                Showing(Field.Holding("text color", FieldEditor.Colour, 1), Contents[1]),
                Showing(Field.Holding("background color", FieldEditor.Colour, 1), Contents[1]), Showing(font, Contents[1]),
                Showing(Decimal("font size", "0.1", "10"), Contents[1]),
                Showing(Field.Among("alignment", Alignments, Alignments), Contents[1]), Showing(Decimal("padding", "0", "50"), Contents[1]),
                Showing(Field.Holding("app color", FieldEditor.Colour, 1), Contents[2]),
                Showing(Field.Holding("app background color", FieldEditor.Colour, 1), Contents[2]),
                Showing(Decimal("image interval", "0", "30"), Contents[1]), Showing(Switch("keep aspect ratio"), Contents[1])
            };
        }

        /// <summary><paramref name="field" />, asked for while the screen's content is set to <paramref name="content" />.</summary>
        static Field Showing(Field field, string content)
        {
            field.AskedWith = "content";
            field.AskedFor = new[] { content };
            return field;
        }

        /// <summary>A number with a fraction, called <paramref name="name" />, between <paramref name="least" /> and <paramref name="most" />.</summary>
        static Field Decimal(string name, string least, string most)
        {
            var field = Field.Between(name, least, most);
            field.Entry = FieldInput.Decimal;
            return field;
        }

        /// <summary>An on or off, called <paramref name="name" />.</summary>
        static Field Switch(string name)
        {
            var field = Field.OneOf(name, Literal.Of(true).Said, Literal.Of(false).Said);
            field.Edits = FieldEditor.Chosen;
            return field;
        }

        /// <summary>The screen <paramref name="which" /> names, or the first where it names none.</summary>
        static IMyTextSurface Surface(IMyTextSurfaceProvider provider, Picked which)
        {
            return provider.GetSurface(which.Screen < provider.SurfaceCount ? which.Screen : 0);
        }

        /// <summary>Where <paramref name="value" /> is among <paramref name="values" />, or the second where it is not there.</summary>
        static int Position<T>(T[] values, T value)
        {
            var at = Array.IndexOf(values, value);
            return at < 0 ? 1 : at;
        }

        /// <summary>Writes <paramref name="shown" />'s text on its screen, whatever the screen is set to show.</summary>
        /// <remarks>A screen set to show nothing or an app keeps that.</remarks>
        static void Show(IMyTextSurfaceProvider provider, Shown shown, ReaderMemory seen)
        {
            Surface(provider, shown.Which).WriteText(shown.Text);
        }

        /// <summary>Sets each setting <paramref name="look" /> holds on its screen, and leaves the rest.</summary>
        static void Apply(IMyTextSurfaceProvider provider, Look look, ReaderMemory seen)
        {
            var surface = Surface(provider, look.Which);
            if (look.Content >= 0)
                surface.ContentType = ContentTypes[look.Content];
            if (look.TextColor.HasValue)
                surface.FontColor = look.TextColor.Value;
            if (look.App != null)
                surface.Script = look.App;
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

        /// <summary>The screen a member acts on.</summary>
        sealed class Picked
        {
            /// <summary>Where the screen is among the block's, counted from 0.</summary>
            public int Screen;

            /// <summary>The screen its slot holds, first among <paramref name="arguments" />, counted from 1; the first where none is.</summary>
            public static Picked Read(IList<Literal> arguments)
            {
                var given = arguments != null && arguments.Count > 0 && arguments[0].Kind == ValueKind.Integer ? arguments[0].Whole : 1;
                return new Picked { Screen = given < 1 || given > int.MaxValue ? 0 : (int)given - 1 };
            }
        }

        /// <summary>The screen a step shows text on, and the text.</summary>
        sealed class Shown
        {
            /// <summary>The screen.</summary>
            public Picked Which;

            /// <summary>What is shown.</summary>
            public string Text = "";

            /// <summary>The values of a step showing text, after its screen.</summary>
            public static Shown Read(IList<Literal> arguments)
            {
                return new Shown
                {
                    Which = Picked.Read(arguments),
                    Text = arguments != null && arguments.Count > 1 ? arguments[1].Text ?? "" : ""
                };
            }
        }
    }
}
