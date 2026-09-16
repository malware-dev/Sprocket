using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Look;

namespace Reactor.Host.SpaceEngineers.Presentation.Controls
{
    /// <summary>
    /// A word with an arrowhead beside it, saying where pressing it goes.
    /// </summary>
    /// <remarks>
    /// Two things rather than the word alone. Back is the only way out of a screen and a list of kinds is
    /// the only thing on a column that opens downward, and an audience who have never used one of these
    /// finds an arrow before it reads anything.
    /// <para>
    /// The arrow leads where it points away from the word - Back, going left - and trails where it points
    /// at what the word is about to open. Both are the same picture turned.
    /// </para>
    /// </remarks>
    public class Signpost : Stack
    {
        readonly Glyph _arrow;
        readonly Label _word;

        public Signpost(string word, string arrow, bool leads)
        {
            Horizontal = true;
            Spacing = Space.Row;

            _arrow = Widgets.Symbol(arrow, Palette.Muted);

            _word = Widgets.Ghost(Widgets.Caps(word));
            _word.Wraps = false;

            if (leads)
            {
                Children.Add(_arrow);
                Children.Add(_word);
            }
            else
            {
                Children.Add(_word);
                Children.Add(_arrow);
            }
        }

        /// <summary>What it says. The arrow beside it does not change.</summary>
        public string Text
        {
            set { _word.Text = value; }
        }

        /// <summary>Colours the arrow and the word together, so the two cannot disagree.</summary>
        public Color Tint
        {
            set
            {
                _arrow.Tint = value;
                _word.Color = value;
            }
        }
    }
}
