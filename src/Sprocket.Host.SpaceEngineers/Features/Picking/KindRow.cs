using System.Globalization;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Drawing;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>One kind of block in the list that narrows a column to it, with how many of that kind there are.</summary>
    public class KindRow : HStack
    {
        readonly Label _name;
        readonly Label _count;

        int _counted = -1;

        /// <summary>A kind saying nothing yet, narrowed to by pressing it once it has a command.</summary>
        public KindRow()
        {
            Spacing = Gaps.Sections;
            Style = Styles.MenuRow;

            Children.Add(new Label { Style = Styles.OptionWord, Placement = Flex.Grow }.As(out _name));
            Children.Add(new Label { Style = Styles.Count, IsVisible = false }.As(out _count));
        }

        /// <summary>What the kind is called.</summary>
        public string Text
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>How many blocks of the kind there are, or -1 where nothing is counted, which shows no count.</summary>
        public int Count
        {
            get { return _counted; }
            set
            {
                if (_counted == value)
                    return;

                _counted = value;
                _count.IsVisible = value >= 0;
                _count.Text = value < 0 ? "" : value.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Whether it is a kind listed under its family, and set in from it.</summary>
        public bool IsNested
        {
            get { return Is(LookStates.Nested); }
            set { Set(LookStates.Nested, value); }
        }

        /// <summary>Whether it is the kind the column is narrowed to.</summary>
        public bool IsCurrent
        {
            get { return _name.Is(LookStates.Chosen); }
            set { _name.Set(LookStates.Chosen, value); }
        }

        /// <summary>As wide as its words and its count need, with the count pushed to the far end when it is given more.</summary>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var needs = base.MeasureContent(g, new Vector2(float.PositiveInfinity, available.Y));
            return base.MeasureContent(g, new Vector2(needs.X, available.Y));
        }
    }
}
