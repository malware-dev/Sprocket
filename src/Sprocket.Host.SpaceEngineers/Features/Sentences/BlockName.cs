using Lumel;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One block as it is shown wherever it is named: its icon, then its name.</summary>
    public class BlockName : HStack, ITextual
    {
        readonly Image _icon;
        readonly Label _name;

        string _art;

        /// <summary>A block with no name and no icon yet.</summary>
        public BlockName()
        {
            Base.Spacing = Gaps.Parts;
            Children.Add(new Image { IsVisible = false }.As(out _icon));
            Children.Add(new Label().As(out _name));
            Resize(_name.FontSize);
        }

        /// <summary>What the block is called.</summary>
        public string Name
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>The block's icon, as its kind names it, or empty for a block with none.</summary>
        public string Icon
        {
            get { return _art; }
            set
            {
                if (_art == value)
                    return;

                _art = value;
                _icon.IsVisible = !string.IsNullOrEmpty(value);
                if (_icon.IsVisible)
                    _icon.Source = Icons.PictureFor(value);
            }
        }

        /// <inheritdoc />
        public string Font => _name.Font;

        /// <inheritdoc />
        public float FontSize => _name.FontSize;

        /// <inheritdoc />
        public Color Color => _name.Color;

        /// <summary>Draws the name in <paramref name="font" />, <paramref name="fontSize" /> and <paramref name="color" />, the icon as tall as a line of it.</summary>
        public void Dress(string font, float fontSize, Color color)
        {
            _name.Base.Wear(font, fontSize, color);
            Resize(fontSize);
        }

        void Resize(float size)
        {
            _icon.Placement = FixedSize.Size(size, size);
        }
    }
}
