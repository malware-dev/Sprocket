using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Writing;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>The box what a slot or a setting is for is written in, in the panel it is made or opened in.</summary>
    public static class PartAbout
    {
        /// <summary>How tall the box is: three lines of writing, as the panel has room for.</summary>
        const float High = 96f;

        /// <summary>A box holding <paramref name="text" />, showing <paramref name="hint" /> while empty, with the way to the guide to its marks.</summary>
        public static Composer Composer(SprocketEditor editor, string hint, string text)
        {
            var composer = new Composer(High, Styles.SlotDescription);
            composer.Show(editor.Texts);
            composer.Guides = editor.Guides;
            composer.Hint = hint;
            composer.Text = text ?? "";
            return composer;
        }
    }
}
