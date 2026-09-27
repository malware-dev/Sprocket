using System.Globalization;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>One kind of block in the list that narrows a column to it, with how many of that kind there are.</summary>
    /// <remarks>
    /// The count is a fact rather than a decoration: fourteen doors is a useful narrowing, one antenna is not. As wide as
    /// what is written in it, because the list is a menu and a menu is as wide as its longest word.
    /// </remarks>
    public class KindRow : HStack
    {
        readonly Label _name;
        readonly Label _count;

        int _counted = -1;

        /// <summary>A kind saying nothing yet, narrowed to by pressing it once it has a command.</summary>
        public KindRow()
        {
            Spacing = 24f;
            Style = Styles.MenuRow;

            Children.Add(new Label { Style = Styles.OptionWord, Placement = Flex.Grow }.As(out _name));
            Children.Add(new Label { Style = Styles.Count }.As(out _count));
        }

        /// <summary>What the kind is called.</summary>
        public string Text
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>How many blocks of the kind there are.</summary>
        public int Count
        {
            get { return _counted; }
            set
            {
                if (_counted == value)
                    return;

                _counted = value;
                _count.Text = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Whether it is the kind the column is narrowed to.</summary>
        public bool IsCurrent
        {
            get { return _name.Style == Styles.OptionChosen; }
            set { _name.Style = value ? Styles.OptionChosen : Styles.OptionWord; }
        }

        /// <summary>As wide as its words and its count need, with the count pushed to the far end when it is given more.</summary>
        /// <remarks>Measured twice: once for what it needs, and again held to that, so it shares any room it is given later.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var needs = base.MeasureContent(g, new Vector2(float.PositiveInfinity, available.Y));
            return base.MeasureContent(g, new Vector2(needs.X, available.Y));
        }
    }
}
