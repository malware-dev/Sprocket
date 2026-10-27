using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>One answer to a choice about the picking itself, such as running a sequence or stopping it, headed by a ring.</summary>
    public class OptionRow : HStack
    {
        readonly ChoiceMark _mark;
        readonly Label _words;

        /// <summary>An answer saying nothing yet, given by pressing it once it has a command.</summary>
        public OptionRow()
        {
            Spacing = 8f;
            Style = Styles.Option;

            Children.Add(new ChoiceMark { Placement = FixedSize.Size(20f, 20f) }.As(out _mark));
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
                _words.Set(LookStates.Chosen, value);
            }
        }
    }
}
