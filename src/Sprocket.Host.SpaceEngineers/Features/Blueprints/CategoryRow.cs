using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>A category of slots on the installation panel: its name over the slots under it, with a grip that carries them all, and a bin.</summary>
    public partial class CategoryRow : HStack, ICarried
    {
        string _category = "";

        /// <summary>A category with no name yet.</summary>
        public CategoryRow()
        {
            CreateView();
        }

        /// <summary>What the category is called.</summary>
        public string Name
        {
            get { return _category; }
            set
            {
                _category = value ?? "";
                _name.Text = _category.ToUpperInvariant();
            }
        }

        /// <summary>What pressing the name does, which is letting it be typed, or null where it cannot be changed.</summary>
        /// <remarks>Shown with a pencil while it renames, and as the name alone where it does not.</remarks>
        public ICommand Renames
        {
            get { return _name.Command; }
            set
            {
                _name.Command = value;
                _name.CanRename = value != null;
            }
        }

        /// <summary>What pressing the name does, said where the pointer rests on it; null for nothing.</summary>
        public string RenamesAbout
        {
            set { _name.About = value; }
        }

        /// <summary>The name, which is given in place.</summary>
        public NamePlate Plate => _name;

        /// <summary>What pressing the bin does, which is removing the category, or null where it cannot be.</summary>
        public ICommand Removes
        {
            get { return _remove.Command; }
            set
            {
                _remove.Command = value;
                _remove.IsEnabled = value != null;
            }
        }

        /// <summary>What pressing the bin does, or why it cannot be pressed, said where the pointer rests on it.</summary>
        public string RemovesAbout
        {
            set { _remove.Tip = Tip.For(_remove.Tip, value); }
        }

        /// <summary>
        /// Whether it can be changed at all: where it cannot, it shows its name alone, with no grip and no bin, its words
        /// standing where a slot's mark does.
        /// </summary>
        public bool Arranged
        {
            set
            {
                _grip.IsVisible = value;
                _remove.IsVisible = value;
                Padding = new Thickness(20f, 16f, 20f, 0f);
            }
        }

        /// <summary>Lets the grip be taken where <paramref name="can" />, dimmed where not, saying <paramref name="about" /> where the pointer rests on it.</summary>
        public void Grips(bool can, string about)
        {
            _grip.CanCarry = can;
            _grip.Color = can ? Palette.Faint : Palette.Dim;
            _grip.Tip = Tip.For(_grip.Tip, about);
        }

        /// <inheritdoc />
        public Grip Grip => _grip;

        /// <inheritdoc />
        public bool IsCarried { get; set; }

        /// <summary>How many slots stand under it, which go with it when it is carried.</summary>
        public int Carries { get; set; }

    }
}
