using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>A category of slots on the installation panel: its name over the slots under it, with a grip that carries them all, and a bin.</summary>
    public partial class CategoryRow : HStack, ICarried
    {
        /// <summary>How long a category's name may be: the most the game's own text boxes in the terminal take.</summary>
        public const int NameLongest = 512;

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
                _name.Text = _category;
            }
        }

        /// <summary>What pressing the name does, which is letting it be typed, or null where it cannot be changed.</summary>
        /// <remarks>Drawn as a button while it renames, and as the name alone where it does not.</remarks>
        public ICommand Renames
        {
            get { return _name.Command; }
            set
            {
                _name.Command = value;
                _name.Style = value != null ? Styles.SlotName : Styles.SlotNameStill;
            }
        }

        /// <summary>What pressing the name does, said where the pointer rests on it; null for nothing.</summary>
        public string RenamesAbout
        {
            set { _name.Tip = Tip.For(_name.Tip, value); }
        }

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
                Padding = new Thickness(value ? 20f : 4f, 24f, 20f, 8f);
            }
        }

        /// <summary>Lets the grip be taken where <paramref name="can" />, dimmed where not, saying <paramref name="about" /> where the pointer rests on it.</summary>
        public void Grips(bool can, string about)
        {
            _grip.CanCarry = can;
            _grip.Color = can ? Palette.Faint : Palette.Dim;
            _grip.Tip = Tip.For(_grip.Tip, about);
        }

        /// <summary>The box the name is typed in, while it is being renamed.</summary>
        public TextBox Box => _box;

        /// <summary>Whether the name is being typed.</summary>
        public bool IsRenaming => _box.IsVisible;

        /// <inheritdoc />
        public Grip Grip => _grip;

        /// <inheritdoc />
        public bool IsCarried { get; set; }

        /// <summary>How many slots stand under it, which go with it when it is carried.</summary>
        public int Carries { get; set; }

        /// <summary>Shows the box in place of the name, holding the name, for it to be typed over.</summary>
        public void Rename()
        {
            _box.Text = _category;
            _box.SelectAll();
            _box.IsVisible = true;
            _name.IsVisible = false;
        }

        /// <summary>Shows the name again in place of the box.</summary>
        public void Renamed()
        {
            _box.IsVisible = false;
            _name.IsVisible = true;
        }
    }
}
