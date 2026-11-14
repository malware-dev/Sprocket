using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>One answer in a list of answers.</summary>
    public partial class ChoiceRow : HStack
    {
        /// <summary>An answer saying nothing yet, given by pressing it once it has a command.</summary>
        public ChoiceRow()
        {
            CreateView();
            _given.Base.Tint = Palette.Live;
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
                Set(LookStates.Chosen, value);
            }
        }
    }
}
