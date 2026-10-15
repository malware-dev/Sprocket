using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

// The mod-side provider adds nothing to the one in the Ingame namespace, and some blocks are given only that one.
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with one screen a player can set up: what it shows, and how it shows it.</summary>
    /// <remarks>A block with several is <see cref="Screens" />'.</remarks>
    public sealed class Screen : Capability
    {
        /// <summary>The action that writes text on the block's screen.</summary>
        public const string ShowText = "show-text";

        /// <summary>How long the text on a screen may be: the game's own panels hold no more.</summary>
        public const int TextLongest = 100000;

        /// <summary>The game's settings of a screen, which on a block with several act on the one last picked.</summary>
        public static readonly string[] Settings =
        {
            "Content", "FontColor", "BackgroundColor", "Font", "FontSize", "alignment", "TextPaddingSlider",
            "ScriptForegroundColor", "ScriptBackgroundColor", "ChangeIntervalSlider", "PreserveAspectRatio"
        };

        static readonly Doing Shows = Doing.Of<IMyTextSurfaceProvider, string>(Written, Show);

        /// <inheritdoc />
        /// <remarks>A text panel sets its screen up with controls of its own, so the game does not count it among the generic ones.</remarks>
        public override bool Claims(IMyTerminalBlock block)
        {
            var screens = block as IMyTextSurfaceProvider;
            return screens != null && (screens.UseGenericLcd || block is IMyTextPanel) && screens.SurfaceCount == 1;
        }

        /// <inheritdoc />
        /// <remarks>A text panel's own words for its settings are the text panel's.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(ShowText, "show text", Shows, Asked()).In(Category.Screen);

            if (block is IMyTextPanel)
                return;

            kind.Renaming("Content", true, "content")
                .Renaming("FontColor", true, "text color")
                .Renaming("BackgroundColor", true, "background color")
                .Renaming("Font", true, "font")
                .Renaming("FontSize", true, "font size")
                .Renaming("alignment", true, "alignment")
                .Renaming("TextPaddingSlider", true, "text padding")
                .Measuring("TextPaddingSlider", " %")
                .Renaming("ScriptForegroundColor", true, "app color")
                .Renaming("ScriptBackgroundColor", true, "app background color")
                .Renaming("ChangeIntervalSlider", true, "image change interval")
                .Measuring("ChangeIntervalSlider", " s")
                .Renaming("PreserveAspectRatio", true, "preserve aspect ratio");
        }

        /// <summary>The text to show.</summary>
        static Field Asked()
        {
            var text = Field.Called("text");
            text.Entry = Entry.Text;
            text.Edits = Editor.Lines;
            text.Longest = Screen.TextLongest;
            text.Needed = true;
            return text;
        }

        /// <summary>The text a step showing some was given.</summary>
        static string Written(IList<Literal> arguments)
        {
            return arguments != null && arguments.Count > 0 ? arguments[0].Text ?? "" : "";
        }

        /// <summary>Writes <paramref name="text" /> on the screen, whatever it is set to show.</summary>
        /// <remarks>
        /// The text alone: a screen set to show nothing or an app keeps that, so a mod drawing the text itself, rich text from
        /// a screen's own text, is left to do so. Setting it to show text is "set up screen"'s to do.
        /// </remarks>
        static void Show(IMyTextSurfaceProvider screens, string text, Recall seen)
        {
            screens.GetSurface(0).WriteText(text);
        }
    }
}
