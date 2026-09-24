using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>
    ///     A word with an arrowhead beside it, saying where pressing it goes.
    /// </summary>
    /// <remarks>
    ///     Two things rather than the word alone. A list of kinds is the only thing on a column that opens
    ///     downward, and an audience who have never used one finds the arrow before it reads anything.
    ///     <para>
    ///         The arrow trails the word, pointing at what pressing it is about to open.
    ///     </para>
    /// </remarks>
    public class Signpost : Stack
    {
        readonly Label _word;

        public Signpost(string word, string arrow)
        {
            Horizontal = true;
            Spacing = Space.Row;

            // The words alone. This whole thing is the control, so the word carries no highlight of
            // its own - the line lights, arrow and all.
            _word = Widgets.Quiet(Widgets.Caps(word), Lettering.Small);

            Children.Add(_word);
            Children.Add(Widgets.Symbol(arrow, Palette.Muted));
        }

        /// <summary>What it says. The arrow beside it does not change.</summary>
        public string Text
        {
            set { _word.Text = value; }
        }
    }
}