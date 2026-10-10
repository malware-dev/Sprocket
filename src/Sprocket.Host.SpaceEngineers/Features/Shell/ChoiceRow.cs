using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>One answer in a list of them: its words, what giving it does that somebody should know first, and whether it is the one already given.</summary>
    /// <remarks>The elements it is drawn with are declared in <c>ChoiceRow.View.cs</c>, and nothing else is.</remarks>
    public partial class ChoiceRow : HStack
    {
        /// <summary>An answer saying nothing yet, given by pressing it once it has a command.</summary>
        public ChoiceRow()
        {
            CreateView();
            _given.Tint = Palette.Live;
        }

        /// <summary>The mark of what kind of thing the answer names, one of the symbols, or empty for none.</summary>
        public string Mark
        {
            get { return _mark.IsVisible ? _mark.Symbol : ""; }
            set
            {
                _mark.IsVisible = !string.IsNullOrEmpty(value);
                if (_mark.IsVisible)
                    _mark.Symbol = value;
            }
        }

        /// <summary>What the answer says.</summary>
        public string Text
        {
            get { return _words.Text; }
            set { _words.Text = value; }
        }

        /// <summary>What giving it does that somebody should know first, or empty.</summary>
        public string Caution
        {
            get { return _caution.Text; }
            set
            {
                _caution.Text = value;
                _caution.IsVisible = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>Whether it is the answer already given.</summary>
        public bool IsGiven
        {
            get { return _given.IsVisible; }
            set
            {
                _given.IsVisible = value;
                Style = value ? Styles.Given : Styles.Answer;
            }
        }
    }
}
