using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What a pick has chosen so far: the blocks, the member done to them and how it is reached, its facet, the toolbar slot and how the blocks are read.</summary>
    public class PickChoices
    {
        /// <summary>The blocks picked, in the order they were picked.</summary>
        public List<long> Blocks { get; } = new List<long>();

        /// <summary>The id of the member chosen, or empty where none is.</summary>
        public string Member { get; set; } = "";

        /// <summary>How the chosen member is reached.</summary>
        public ReachKind Form { get; set; }

        /// <summary>The name of the facet the chosen member acts on, or empty.</summary>
        public string Facet { get; set; } = "";

        /// <summary>The slot of a toolbar a person presses that is chosen, or null where no such toolbar is.</summary>
        public ToolbarSlot Slot { get; set; }

        /// <summary>How several blocks are read.</summary>
        public Quantifier Reading { get; set; }

        /// <summary>Whether the blocks are read by a total of a number on them, a sum or an average, which lists only numbers.</summary>
        public bool Totals => Reading == Quantifier.Sum || Reading == Quantifier.Average;

        /// <summary>A copy of every choice as it stands, which changes to these leave as it is.</summary>
        public PickChoices Copy()
        {
            var copy = new PickChoices { Member = Member, Form = Form, Facet = Facet, Slot = Slot, Reading = Reading };
            copy.Blocks.AddRange(Blocks);
            return copy;
        }
    }
}
