using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>One part of a blueprint: its name, what it says about itself, and the blocks the chosen logic system fills it with.</summary>
    /// <remarks>
    /// The blocks are a slot, the same chip a sentence fills: a part is what a sentence names, and pressing it chooses what
    /// fills it, which is what pressing a sentence's slot does too. An empty part says so in the color an unfilled slot
    /// does, because it is the same news. Whoever holds it sets what it shows and calls <see cref="Show" />; the elements
    /// it is drawn with are declared in <c>PartRow.View.cs</c>.
    /// </remarks>
    public partial class PartRow : HStack
    {
        readonly List<Mention> _pieces = new List<Mention>();

        /// <summary>A part saying nothing yet.</summary>
        public PartRow()
        {
            Blocks = new List<long>();
            CreateView();
        }

        /// <summary>What the part is called.</summary>
        public string Name
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>What the author wrote about the part beyond its name, or empty.</summary>
        public string About
        {
            get { return _about.Text; }
            set
            {
                _about.Text = value ?? "";
                _about.IsVisible = _about.Text.Length != 0;
            }
        }

        /// <summary>The blocks the chosen logic system fills the part with, by entity id; none where it is empty.</summary>
        public List<long> Blocks { get; }

        /// <summary>What pressing the blocks does, which is choosing others, or null where they cannot be changed.</summary>
        public ICommand Fills
        {
            get { return _blocks.Command; }
            set { _blocks.Command = value; }
        }

        /// <summary>What pressing the pencil does, which is asking the part's new name, or null where it cannot be renamed.</summary>
        public ICommand Renames
        {
            get { return _pencil.Command; }
            set
            {
                _pencil.Command = value;
                _pencil.IsVisible = value != null;
            }
        }

        /// <summary>The pencil, which a question about the part's name hangs under.</summary>
        public Element Pencil => _pencil;

        /// <summary>Says the blocks, naming them as <paramref name="world" /> calls them.</summary>
        /// <remarks>Walks the blocks for their names, so it is for whoever holds it to call only when they change.</remarks>
        public void Show(GridWorld world)
        {
            _pieces.Clear();
            SentenceText.Entities(world, Blocks, true, _pieces);
            _blocks.Say(_pieces);
            _blocks.IsEmpty = Blocks.Count == 0;
        }
    }
}
