using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>A word with an arrowhead after it, saying where pressing it goes.</summary>
    public class Signpost : Stack
    {
        readonly Label _word;

        /// <summary>
        ///     Makes a signpost reading <paramref name="word" />, with the symbol <paramref name="arrow" /> after it.
        /// </summary>
        public Signpost(string word, string arrow)
        {
            Horizontal = true;
            Spacing = Space.Row;

            // No highlight on the word: the whole signpost lights as one control.
            _word = Widgets.Quiet(Widgets.Caps(word), Lettering.Small);

            Children.Add(_word);
            Children.Add(Widgets.Symbol(arrow, Palette.Muted));
        }

        /// <summary>What it says.</summary>
        public string Text
        {
            set { _word.Text = value; }
        }
    }
}