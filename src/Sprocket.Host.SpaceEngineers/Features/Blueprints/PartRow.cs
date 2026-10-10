using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Writing;
using VRageMath;

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
        readonly List<Mention> _signalPieces = new List<Mention>();

        float _wide = PartList.Widest;

        /// <summary>A part saying nothing yet.</summary>
        public PartRow()
        {
            Blocks = new List<long>();
            Signals = new List<EntityEvent>();
            CreateView();
        }

        /// <summary>What the part is called.</summary>
        public string Name
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>The mark of what kind of slot it is: the block, or the signal.</summary>
        public string Mark
        {
            get { return _mark.Symbol; }
            set { _mark.Symbol = value; }
        }

        /// <summary>What the author wrote about the part beyond its name, as markup read into rich text, or empty.</summary>
        public string Described
        {
            get { return _about.Markdown ?? ""; }
            set
            {
                var said = value ?? "";
                _about.Markdown = said;
                _about.IsVisible = said.Length != 0 && !Editing;
            }
        }

        /// <summary>The word after the name of a slot that may be left without a block, or null for one that needs a block.</summary>
        public string Optional
        {
            get { return _optional.Text.Length != 0 ? _optional.Text : null; }
            set
            {
                _optional.Text = value ?? "";
            }
        }

        /// <summary>Whether the slot is being written rather than filled: its name and what it is for in boxes.</summary>
        public bool Editing
        {
            get { return _nameBox.IsVisible; }
            set
            {
                _name.IsVisible = !value;
                _about.IsVisible = !value && Described.Length != 0;
                _blocks.IsVisible = !value;
                _signals.IsVisible = !value && Signals.Count != 0;
                _problem.IsVisible = !value && _problem.Text.Length != 0;
                _nameBox.IsVisible = value;
                _refusal.IsVisible = value && _refusal.Text.Length != 0;
                _aboutComposer.IsVisible = value;
            }
        }

        /// <summary>Why the name in the box is not the slot's, said under it and marked on it, or null where nothing is refused.</summary>
        public string Refusal
        {
            get { return _refusal.Text.Length != 0 ? _refusal.Text : null; }
            set
            {
                _refusal.Text = value ?? "";
                _refusal.IsVisible = value != null;
                _nameBox.Style = value == null ? Styles.Field : Styles.FieldRefused;
            }
        }

        /// <summary>The box the slot's name is written in.</summary>
        public TextBox NameBox => _nameBox;

        /// <summary>Where what the slot is for is written.</summary>
        public Composer About => _aboutComposer;

        /// <summary>The switch that says whether the slot may be left without a block, on for optional.</summary>
        public Switch Required => _required;

        /// <summary>What is wrong with the slot's block, said beside it, or null where nothing is.</summary>
        public string Problem
        {
            get { return _problem.Text.Length != 0 ? _problem.Text : null; }
            set
            {
                _problem.Text = value ?? "";
                _problem.IsVisible = value != null && !Editing;
            }
        }

        /// <summary>How wide the part's name and description stand, which the list it is in decides.</summary>
        public float Column
        {
            get { return _wide; }
            set
            {
                if (_wide == value)
                    return;

                _wide = value;
                _column.Placement = Fixed.Wide(value);
            }
        }

        /// <summary>The blocks the chosen logic system fills the part with, by entity id; none where it is empty.</summary>
        public List<long> Blocks { get; }

        /// <summary>For a signal slot, the signals its blocks send that answer it, such as "button 2 is pressed"; none for any other slot.</summary>
        public List<EntityEvent> Signals { get; }

        /// <summary>What pressing the blocks does, which is choosing others, or null where they cannot be changed.</summary>
        public ICommand Fills
        {
            get { return _blocks.Command; }
            set
            {
                _blocks.Command = value;
                _signals.Command = value;
            }
        }

        /// <summary>How wide the part's name and description would stand in <paramref name="available" />.</summary>
        public float Asks(IGraphics g, Vector2 available)
        {
            _column.Measure(g, available);
            return _column.DesiredSize.X;
        }

        /// <summary>Says <paramref name="words" /> in place of blocks, marked as a slot still to be filled where <paramref name="unfilled" /> is true.</summary>
        public void Say(string words, bool unfilled)
        {
            _pieces.Clear();
            _pieces.Add(new Mention(words, null));
            _blocks.Say(_pieces);
            _blocks.IsEmpty = unfilled;
            _signals.IsVisible = false;
        }

        /// <summary>Says the blocks, naming them as <paramref name="world" /> calls them, and for a signal slot the signals beside them.</summary>
        /// <remarks>
        /// The blocks and their signals are two parts, as they are in a trigger's row, so a slot and the trigger its signal
        /// starts read alike. Walks the blocks for their names, so it is for whoever holds it to call only when they change.
        /// </remarks>
        public void Show(GridWorld world)
        {
            _pieces.Clear();
            SentenceText.Entities(world, Blocks, Signals.Count == 0, _pieces);
            _blocks.Say(_pieces);
            _blocks.IsEmpty = Blocks.Count == 0;

            _signalPieces.Clear();
            for (var i = 0; i < Signals.Count; i++)
            {
                if (i > 0)
                    _signalPieces.Add(new Mention(", ", null));

                SentenceText.InTrigger(world, new EventSlot(Signals[i], null), _signalPieces);
            }

            _signals.Say(_signalPieces);
            _signals.IsVisible = Signals.Count != 0 && !Editing;
        }
    }
}
