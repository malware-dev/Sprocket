using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>One answer to a choice about the picking itself, such as running a sequence or stopping it, headed by a ring.</summary>
    /// <remarks>Round, never square: exactly one of these holds at a time, and that is what the shape says before anybody reads the words.</remarks>
    public class OptionRow : HStack
    {
        readonly Mark _mark;
        readonly Label _words;

        /// <summary>An answer saying nothing yet, given by pressing it once it has a command.</summary>
        public OptionRow()
        {
            Spacing = 8f;
            Style = Styles.Option;

            Children.Add(new Mark { Placement = Fixed.Size(20f, 20f) }.As(out _mark));
            Children.Add(new Label { Style = Styles.OptionWord }.As(out _words));
        }

        /// <summary>What the answer says.</summary>
        public string Text
        {
            get { return _words.Text; }
            set { _words.Text = value; }
        }

        /// <summary>Whether it is the answer that holds.</summary>
        public bool IsCurrent
        {
            get { return _mark.IsChecked; }
            set
            {
                _mark.IsChecked = value;
                _words.Style = value ? Styles.OptionChosen : Styles.OptionWord;
            }
        }
    }
}
