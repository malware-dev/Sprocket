using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Utils;
using VRage.Game.GUI.TextPanel;

// The mod-side provider adds nothing to the one in the Ingame namespace, and some blocks provide only that one.
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// A block's screen, the type this host provides as <c>sprocket.screen</c>: show text or an image on it, set it up, read what it
    /// shows and its settings.
    /// </summary>
    /// <remarks>Each member receives its screen first, counted from 1, ahead of the step's own values.</remarks>
    public sealed partial class ScreenType : HostType
    {
        /// <summary>The action that writes text on a screen.</summary>
        public const string ShowText = "show_text";

        /// <summary>Setting up how a screen shows what it shows, which is Sprocket's own.</summary>
        public const string SetUp = "set_up_screen";

        /// <summary>Showing a chosen image on a screen, which is Sprocket's own.</summary>
        public const string ShowImage = "show_image";

        /// <summary>The text a screen shows, which is Sprocket's own.</summary>
        public const string TextShown = "text_shown";

        /// <summary>The type's id, as a slot expecting it names it: <c>sprocket.screen</c>.</summary>
        public const string TypeId = BlockKinds.Own + ".screen";

        /// <summary>The app a screen runs, which is Sprocket's own.</summary>
        public const string App = "app";

        /// <summary>How long the text on a screen may be: the game's own panels hold no more.</summary>
        public const int TextLongest = 100000;

        // The game's settings of a screen; on a block with several they act on the one last picked.
        static readonly string[] Settings =
        {
            "Content", "FontColor", "BackgroundColor", "Font", "FontSize", "alignment", "TextPaddingSlider",
            "ScriptForegroundColor", "ScriptBackgroundColor", "ChangeIntervalSlider", "PreserveAspectRatio"
        };

        const string Unheard = "A screen raises no event when its settings change.";
        const string Unwritten = "A screen raises no event when what it shows changes.";

        // Where showing an app is among the contents.
        const int AppContent = 2;

        // The app a screen runs where it runs none.
        const string NoApp = "none";

        // What a screen shows and how its text lines up, each word beside the game's value for it.
        static readonly string[] Contents = ChoiceTypes.ScreenContent.Choices;
        static readonly ContentType[] ContentTypes = { ContentType.NONE, ContentType.TEXT_AND_IMAGE, ContentType.SCRIPT };
        static readonly string[] Alignments = ChoiceTypes.Alignment.Choices;
        static readonly TextAlignment[] TextAlignments = { TextAlignment.LEFT, TextAlignment.CENTER, TextAlignment.RIGHT };

        /// <inheritdoc />
        public override string Id => TypeId;

        /// <inheritdoc />
        public override string Function => "get_screen";

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
                .In(Category.Screen).Of(this)
                .WithAction(ShowImage, "show image", OwnAction.Of<IMyTextSurfaceProvider, Imaged>(Imaged.Read, Showed), AskedImage())
                .In(Category.Screen).Of(this)
                .WithValue(TextShown, "text", OwnValue.Text<IMyTextSurfaceProvider, Picked>(picked,
                    (provider, which) => Surface(provider, which).GetText())).Looked(Unwritten).In(Category.Screen).Of(this);

            Value(kind, "content", OwnValue.OneOf<IMyTextSurfaceProvider, Picked>(ChoiceTypes.ScreenContent, picked,
                (provider, which) => Position(ContentTypes, Surface(provider, which).ContentType)));
            kind.WithValue(App, "app", Running(Apps(screens), picked)).Looked(Unwritten).In(Category.Screen).Of(this);
            Value(kind, "text color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).FontColor));
            Value(kind, "background color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).BackgroundColor));
            Value(kind, "font", OwnValue.Text<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).Font));
            Value(kind, "font size", OwnValue.Real<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).FontSize));
            Value(kind, "alignment", OwnValue.OneOf<IMyTextSurfaceProvider, Picked>(ChoiceTypes.Alignment, picked,
                (provider, which) => Position(TextAlignments, Surface(provider, which).Alignment)));
            Value(kind, "padding", OwnValue.Real<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).TextPadding));
            Value(kind, "app color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).ScriptForegroundColor));
            Value(kind, "app background color", OwnValue.Color<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).ScriptBackgroundColor));
            Value(kind, "image interval", OwnValue.Real<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).ChangeInterval));
            Value(kind, "keep aspect ratio", OwnValue.Flag<IMyTextSurfaceProvider, Picked>(picked,
                (provider, which) => Surface(provider, which).PreserveAspectRatio));
        }

        /// <summary>Adds a reading of one screen's setting, called <paramref name="said" />.</summary>
        void Value(EntityKind kind, string said, OwnValue own)
        {
            kind.WithValue(Setting(said), said, own).Looked(Unheard).In(Category.Screen).Of(this);
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

        /// <summary>
        /// The app a screen runs, by its id, which an editor offers among <paramref name="apps" />, each by the name the game shows
        /// for it; empty where it runs none.
        /// </summary>
        /// <remarks>A look is a pass over the list, made only where an <c>if</c> reads the value or a <c>when</c> watches it.</remarks>
        static OwnValue Running(IList<string> apps, Func<IList<Literal>, Picked> picked)
        {
            var ids = new string[apps.Count];
            var named = new string[apps.Count];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = apps[i];
                named[i] = AppName(apps[i]);
            }

            return OwnValue.TextAmong<IMyTextSurfaceProvider, Picked>(ids, picked, (provider, which) => Surface(provider, which).Script ?? "").Saying(named);
        }

        /// <summary>The image to show, from the screen images the terminal lists and by the names it shows them by.</summary>
        static Field AskedImage()
        {
            var words = new List<string>();
            var means = new List<string>();

            var definitions = MyDefinitionManager.Static;
            if (definitions != null)
            {
                foreach (var texture in definitions.GetLCDTexturesDefinitions())
                {
                    if (!texture.Public || !texture.Selectable)
                        continue;

                    var id = texture.Id.SubtypeName;
                    words.Add(string.IsNullOrEmpty(texture.LocalizationId)
                        ? id
                        : MyTexts.GetString(MyStringId.GetOrCompute(texture.LocalizationId)));
                    means.Add(id);
                }
            }

            var image = Field.Among("image", words, means);
            image.Searched = true;
            image.Entry = FieldInput.Text;
            image.Needed = true;
            return image;
        }

        /// <summary>What the game calls the app <paramref name="id" /> in its own lists, or the id where it names none.</summary>
        /// <remarks>
        /// The game's own apps are named by its texts; a mod's app usually names itself in plain words, which are its name as
        /// they stand.
        /// </remarks>
        public static string AppName(string id)
        {
            var apps = MyTextSurfaceScriptFactory.Instance;
            MyTextSurfaceScriptFactory.ScriptInfo app;
            if (apps == null || id == null || !apps.Scripts.TryGetValue(id, out app))
                return id;

            if (MyTexts.Exists(app.DisplayName))
                return MyTexts.GetString(app.DisplayName);

            var named = app.DisplayName.String;
            return string.IsNullOrEmpty(named) ? id : named;
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

        /// <summary>
        /// Each setting of the screen to set up: what it shows, which may be left empty to set nothing, and the settings of
        /// what it is set to show, each opening on what the screen holds now.
        /// </summary>
        /// <remarks>A setting not asked for under the content chosen is left as it is.</remarks>
        static Field[] Asked(IList<string> fonts, IList<string> apps)
        {
            var font = fonts.Count > 0 ? Field.Among("font", fonts, fonts) : Field.Called("font");
            font.Searched = true;
            font.Entry = FieldInput.Text;

            // No app is a choice of its own: a screen set to show an app and running none is drawn on by a script.
            var named = new List<string> { NoApp };
            var ids = new List<string> { "" };
            foreach (var one in apps)
            {
                named.Add(AppName(one));
                ids.Add(one);
            }

            var app = Field.Among("app", named, ids);
            app.Searched = true;
            app.Entry = FieldInput.Text;

            // Content first, and each setting asked for while the content it applies to is chosen, as the terminal shows it.
            var content = Field.Among("content", Contents, Contents);
            content.Default = "";
            return new[]
            {
                content, Showing(app, Contents[AppContent], NoApp), Showing(ColorCalled("text color"), Contents[1]),
                Showing(ColorCalled("background color"), Contents[1]), Showing(font, Contents[1]),
                Showing(Decimal("font size", "0.1", "10"), Contents[1]),
                Showing(Field.Among("alignment", Alignments, Alignments), Contents[1]), Showing(Decimal("padding", "0", "50"), Contents[1]),
                Showing(ColorCalled("app color"), Contents[AppContent]), Showing(ColorCalled("app background color"), Contents[AppContent]),
                Showing(Decimal("image interval", "0", "30"), Contents[1]), Showing(Switch("keep aspect ratio"), Contents[1])
            };
        }

        /// <summary>
        /// <paramref name="field" />, asked for while the screen's content is set to <paramref name="content" />, opening on the
        /// screen's own value of the same name, and given as empty while it is not asked for.
        /// </summary>
        static Field Showing(Field field, string content, string empty = "")
        {
            field.AskedWith = "content";
            field.AskedFor = new[] { content };
            field.OpensOn = Setting(field.Name);
            field.Default = empty;
            return field;
        }

        /// <summary>The id of the screen's setting called <paramref name="said" />.</summary>
        static string Setting(string said)
        {
            return said.Replace(' ', '_');
        }

        /// <summary>A color called <paramref name="name" />.</summary>
        static Field ColorCalled(string name)
        {
            return Field.Holding(name, FieldEditor.Color, 1);
        }

        /// <summary>A number with a fraction, called <paramref name="name" />, between <paramref name="least" /> and <paramref name="most" />.</summary>
        static Field Decimal(string name, string least, string most)
        {
            var field = Field.Between(name, least, most);
            field.Entry = FieldInput.Real;
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

        /// <summary>Replaces the images a screen shows with the one a step chose, and has it show text and images.</summary>
        static void Showed(IMyTextSurfaceProvider provider, Imaged imaged, ReaderMemory seen)
        {
            var surface = Surface(provider, imaged.Which);
            surface.ContentType = ContentType.TEXT_AND_IMAGE;
            surface.ClearImagesFromSelection();
            surface.AddImageToSelection(imaged.Image);
        }

        /// <summary>Sets each setting <paramref name="look" /> holds on its screen, and leaves the rest.</summary>
        static void Apply(IMyTextSurfaceProvider provider, Look look, ReaderMemory seen)
        {
            var surface = Surface(provider, look.Which);
            if (look.Content >= 0)
                surface.ContentType = ContentTypes[look.Content];
            if (look.TextColor.HasValue)
                surface.FontColor = look.TextColor.Value;
            if (look.Runs != null)
                surface.Script = look.Runs;
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
                var given = arguments != null && arguments.Count > 0 && arguments[0].Kind == ValueKind.Integer ? arguments[0].Integer : 1;
                return new Picked { Screen = given < 1 || given > int.MaxValue ? 0 : (int)given - 1 };
            }
        }

        /// <summary>The screen a step shows an image on, and the image.</summary>
        sealed class Imaged
        {
            /// <summary>The screen.</summary>
            public Picked Which;

            /// <summary>The image's id.</summary>
            public string Image = "";

            /// <summary>The values of a step showing an image, after its screen.</summary>
            public static Imaged Read(IList<Literal> arguments)
            {
                return new Imaged
                {
                    Which = Picked.Read(arguments),
                    Image = arguments != null && arguments.Count > 1 ? arguments[1].Text ?? "" : ""
                };
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
