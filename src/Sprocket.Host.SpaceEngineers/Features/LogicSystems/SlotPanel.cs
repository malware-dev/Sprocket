using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>What a logic system's installation panel tab shows: its description under a heading, and its slots with the ways to group and tidy them.</summary>
    public partial class SlotPanel : VStack
    {
        /// <summary>An empty panel.</summary>
        public SlotPanel()
        {
            CreateView();
        }

        /// <summary>What the panel says while there is nothing on it; hidden where it says nothing.</summary>
        public string Empty
        {
            set
            {
                _empty.Text = value ?? "";
                _emptyLine.IsVisible = _empty.Text.Length != 0;
            }
        }

        /// <summary>The description's heading, its box and its words as they read, shown together or not at all.</summary>
        public Section Describing => _describing;

        /// <summary>The heading over the description.</summary>
        public Label DescriptionHeading => _descriptionHeading;

        /// <summary>The box the description is written in while the logic system may be changed.</summary>
        public Composer Composer => _composer;

        /// <summary>The description as it reads while the logic system may not be changed.</summary>
        public RichText Description => _description;

        /// <summary>The slots under their heading, with the ways to tidy them and to add a category; shown while there are slots.</summary>
        public Section Slotting => _slotting;

        /// <summary>The heading over the slots, which links what a slot is.</summary>
        public RichText SlotsHeading => _slotsHeading;

        /// <summary>The slots and categories, one row each, carried to be put in order.</summary>
        public PartList Slots => _slots;

        /// <summary>The button that adds a category.</summary>
        public Button AddCategory => _addCategory;

        /// <summary>The button that tidies the slots.</summary>
        public Button Optimize => _optimize;
    }
}
