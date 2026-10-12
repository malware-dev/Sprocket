using System.Collections.Generic;
using Lumel;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>One slot of a blueprint: its name, what it says about itself, the blocks one installation fills it with, and what is still to do about it.</summary>
    /// <remarks>
    /// The blocks are a slot, the same chip a sentence fills: a slot is what a sentence names, and pressing it chooses what
    /// fills it, which is what pressing a sentence's slot does too. An empty slot says so in the color an unfilled slot
    /// does, because it is the same news. Whoever holds it sets what it shows and calls <see cref="Show" />; the elements
    /// it is drawn with are declared in <c>PartRow.View.cs</c>.
    /// </remarks>
    public partial class PartRow : HStack
    {
        readonly List<Mention> _pieces = new List<Mention>();
        readonly List<Mention> _signalPieces = new List<Mention>();

        float _wide = PartList.Widest;
        string _opensAbout;
        NoticeLevel _level = NoticeLevel.Problem;

        /// <summary>A slot saying nothing yet.</summary>
        public PartRow()
        {
            Blocks = new List<long>();
            Signals = new List<EntityEvent>();
            CreateView();
        }

        /// <summary>What the slot is called.</summary>
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

        /// <summary>What the author wrote about the slot beyond its name, as markup read into rich text, or empty.</summary>
        public string Described
        {
            get { return _about.Markdown ?? ""; }
            set
            {
                var said = value ?? "";
                _about.Markdown = said;
                _about.IsVisible = said.Length != 0;
            }
        }

        /// <summary>What pressing the slot's name does, which is opening it to be named and described, or null where it cannot be.</summary>
        public ICommand Opens
        {
            get { return _column.Command; }
            set { _column.Command = value; }
        }

        /// <summary>What pressing the slot's name does, said where the pointer rests on it; null for nothing.</summary>
        public string OpensAbout
        {
            get { return _opensAbout; }
            set
            {
                _opensAbout = value;
                _column.Tip = Tip.For(_column.Tip, value);
            }
        }

        /// <summary>What is wrong with the slot's block, or worth a word, said beside it, or null where nothing is.</summary>
        public string Problem
        {
            get { return _problem.Text.Length != 0 ? _problem.Text : null; }
            set
            {
                _problem.Text = value ?? "";
                _problem.IsVisible = value != null;
            }
        }

        /// <summary>How much <see cref="Problem" /> matters: a problem, a warning or a note.</summary>
        public NoticeLevel Level
        {
            get { return _level; }
            set
            {
                _level = value;
                _problem.Color = Inks.Of(value, Palette.Faint);
            }
        }

        /// <summary>What other installations still have to do about the slot, said under its blocks, or null where nothing.</summary>
        public string Marks
        {
            get { return _marks.Text.Length != 0 ? _marks.Text : null; }
            set
            {
                _marks.Text = value ?? "";
                _marks.IsVisible = value != null;
            }
        }

        /// <summary>The blocks set aside for the slot, which its picker offers first, said under its blocks, or null where none are.</summary>
        public string SetAside
        {
            get { return _aside.Text.Length != 0 ? _aside.Text : null; }
            set
            {
                _aside.Text = value ?? "";
                _aside.IsVisible = value != null;
            }
        }

        /// <summary>How wide the slot's name and description stand, which the list it is in decides.</summary>
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

        /// <summary>The blocks the installation fills the slot with, by entity id; none where it is empty.</summary>
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

        /// <summary>How wide the slot's name and description would stand in <paramref name="available" />.</summary>
        public float Asks(IGraphics g, Vector2 available)
        {
            _column.Measure(g, available);
            return _column.DesiredSize.X;
        }

        /// <summary>
        /// Takes as its blocks the ones <paramref name="filled" /> names that <paramref name="bindings" /> finds, and for a signal
        /// slot the signals <paramref name="at" /> says each sends; answers the first name whose block is gone, or null.
        /// </summary>
        public string Take(Bindings bindings, IList<string> filled, IList<ToolbarSlot> at)
        {
            Blocks.Clear();
            Signals.Clear();

            string gone = null;
            for (var i = 0; filled != null && i < filled.Count; i++)
            {
                var block = bindings.Entity(filled[i]);
                if (block == null)
                {
                    if (gone == null)
                        gone = filled[i];

                    continue;
                }

                Blocks.Add(block.Id);
                var answers = at != null && i < at.Count ? block.Kind.Signal(at[i]) : null;
                if (answers != null)
                    Signals.Add(answers);
            }

            return gone;
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
            _signals.IsVisible = Signals.Count != 0;
        }
    }
}
