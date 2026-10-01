using System;
using System.Collections.Generic;
using Lumel.Input;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Writing;

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

        string _said = "";

        /// <summary>A part saying nothing yet, whose description's guide opens in <paramref name="windows" />.</summary>
        public PartRow(Func<WindowLayer> windows)
        {
            Blocks = new List<long>();
            CreateView(windows);
        }

        /// <summary>What the part is called.</summary>
        public string Name
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>What the author wrote about the part beyond its name, as markup read into rich text, or empty.</summary>
        public string Described
        {
            get { return _said; }
            set
            {
                var said = value ?? "";
                if (said != _said)
                {
                    _said = said;
                    _about.Document = Markdown.Parse(said);
                }

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
                _optional.IsVisible = value != null && !Editing;
            }
        }

        /// <summary>Whether the slot is being written rather than filled: its name and what it is for in boxes, and a switch for whether it needs a block.</summary>
        public bool Editing
        {
            get { return _nameBox.IsVisible; }
            set
            {
                _name.IsVisible = !value;
                _about.IsVisible = !value && _said.Length != 0;
                _optional.IsVisible = !value && _optional.Text.Length != 0;
                _blocks.IsVisible = !value;
                _problem.IsVisible = !value && _problem.Text.Length != 0;
                _nameBox.IsVisible = value;
                _aboutComposer.IsVisible = value;
                _required.IsVisible = value;
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

        /// <summary>The blocks the chosen logic system fills the part with, by entity id; none where it is empty.</summary>
        public List<long> Blocks { get; }

        /// <summary>What pressing the blocks does, which is choosing others, or null where they cannot be changed.</summary>
        public ICommand Fills
        {
            get { return _blocks.Command; }
            set { _blocks.Command = value; }
        }

        /// <summary>Says <paramref name="words" /> in place of blocks, marked as a slot still to be filled where <paramref name="unfilled" /> is true.</summary>
        public void Say(string words, bool unfilled)
        {
            _pieces.Clear();
            _pieces.Add(new Mention(words, null));
            _blocks.Say(_pieces);
            _blocks.IsEmpty = unfilled;
        }

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
