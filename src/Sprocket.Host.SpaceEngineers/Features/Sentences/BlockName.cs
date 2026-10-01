using Lumel;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One block as it is shown wherever it is named: its icon, then its name.</summary>
    /// <remarks>The icon is as tall as a line of the name, so it sizes with whatever style the name is given.</remarks>
    public class BlockName : HStack, ITextual
    {
        readonly Image _icon;
        readonly Label _name;

        string _art;

        /// <summary>A block with no name and no icon yet.</summary>
        public BlockName()
        {
            Spacing = 8f;
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
                    _icon.Material = Icons.MaterialFor(value);
            }
        }

        /// <inheritdoc />
        public string Font
        {
            get { return _name.Font; }
            set { _name.Font = value; }
        }

        /// <inheritdoc />
        public float FontSize
        {
            get { return _name.FontSize; }
            set
            {
                if (_name.FontSize == value)
                    return;

                _name.FontSize = value;
                Resize(value);
            }
        }

        /// <inheritdoc />
        public Color Color
        {
            get { return _name.Color; }
            set { _name.Color = value; }
        }

        void Resize(float size)
        {
            _icon.Placement = Fixed.Size(size, size);
        }
    }
}
