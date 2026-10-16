using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A text panel: a screen a sequence writes a status, a warning or an image on.</summary>
    /// <remarks>
    /// A panel is a block with one screen, and that screen is reached as every block's screen is, through
    /// <see cref="ScreenType" />: showing text, setting it up, reading its settings. What is the panel's own is here.
    /// </remarks>
    public sealed class TextPanel : Capability
    {
        /// <summary>Showing a chosen image, which is Sprocket's own.</summary>
        public const string ShowImage = "show-image";

        /// <summary>The text the screen shows, which is Sprocket's own.</summary>
        public const string Shown = "text-shown";

        /// <summary>The app the screen runs, which is Sprocket's own.</summary>
        public const string App = "app";

        const string Unheard = "A screen raises no event when what it shows changes.";

        static readonly Doing Shows = Doing.Of<IMyTextPanel, Chosen>(Chosen.Read, Showed);

        // Each look reads the text afresh, as the terminal's own text values do: the screen hands it out only as a new string.
        static readonly Being Written = Being.Text<IMyTextPanel>(panel => panel.GetText());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTextPanel;
        }

        /// <inheritdoc />
        /// <remarks>The title is a label shown only in the terminal, like the block's name.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(ShowImage, "show image", Shows, AskedImage()).In(Category.Screen)
                .WithValue(Shown, "text", Written).Looked(Unheard).In(Category.Screen)
                .WithValue(App, "app", Running(Apps(block))).Looked(Unheard).In(Category.Screen)
                .Hiding("Title")
                .Renaming("Rotate", true, "rotation")
                .Measuring("Rotate", "°", 1, 90, "270");
        }

        /// <summary>
        /// The image to show, from the screen images the terminal lists and by the names it shows them by.
        /// </summary>
        /// <remarks>
        /// The game's LCD texture definitions that are public and selectable, each named by its localized text, or its id
        /// where it has none. Where none are loaded - the test harness - the list is empty.
        /// </remarks>
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
            image.Entry = Entry.Text;
            image.Needed = true;
            return image;
        }

        /// <summary>Every app the screen can run, by id.</summary>
        /// <remarks>The game's own list, which holds the apps mods add as well.</remarks>
        static string[] Apps(IMyTerminalBlock block)
        {
            var apps = new List<string>();
            var panel = block as IMyTextPanel;
            if (panel != null)
                panel.GetScripts(apps);

            return apps.ToArray();
        }

        /// <summary>The app the screen runs, as one of <paramref name="apps" />, which a step can choose.</summary>
        /// <remarks>
        /// A look finds the app among the ids, which is a pass over the list; it is made only where an <c>if</c> reads the
        /// value or a <c>when</c> watches it, on its cadence. No app, or one the list does not hold, is none of the names.
        /// Choosing one leaves the screen's content as it was: the screen runs the app while its content is an app, which is
        /// when the terminal shows its list of them.
        /// </remarks>
        static Being Running(string[] apps)
        {
            return Being.OneOf<IMyTextPanel>(apps, panel => Array.IndexOf(apps, panel.Script))
                .Writing<IMyTextPanel>((panel, value) => panel.Script = apps[(int)value.Whole]);
        }

        /// <summary>Replaces the images the screen shows with the one a step chose, and has it show text and images.</summary>
        static void Showed(IMyTextPanel panel, Chosen chosen, Recall seen)
        {
            panel.ContentType = ContentType.TEXT_AND_IMAGE;
            panel.ClearImagesFromSelection();
            panel.AddImageToSelection(chosen.Image);
        }

        /// <summary>The image a step chose.</summary>
        sealed class Chosen
        {
            /// <summary>The image's id.</summary>
            public string Image = "";

            /// <summary>The image a step showing one was given.</summary>
            public static Chosen Read(IList<Literal> arguments)
            {
                return new Chosen { Image = arguments != null && arguments.Count > 0 ? arguments[0].Text ?? "" : "" };
            }
        }
    }
}
