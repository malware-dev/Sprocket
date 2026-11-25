using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>What a pick has chosen so far: the blocks, the member done to them and how it is reached, its facet, the toolbar slot and how the blocks are read.</summary>
    public class PickChoices
    {
        /// <summary>The blocks picked, in the order they were picked.</summary>
        public List<long> Blocks { get; } = new List<long>();

        /// <summary>The member chosen, in its namespace, or empty where none is.</summary>
        public MemberKey Member { get; set; }

        /// <summary>How the chosen member is reached.</summary>
        public ReachKind Form { get; set; }

        /// <summary>The name of the facet the chosen member acts on, or empty.</summary>
        public string Facet { get; set; } = "";

        /// <summary>The slot of a toolbar a person presses that is chosen, or null where no such toolbar is.</summary>
        public ToolbarSlot Slot { get; set; }

        /// <summary>How several blocks are read.</summary>
        public Quantifier Reading { get; set; }

        /// <summary>Whether a reading of several blocks may check a comparison on any or all of them, as a condition an expression builds may.</summary>
        public bool Checks { get; set; }

        /// <summary>Whether several blocks are read by checking a comparison on any or all of them, as chosen in how they are read.</summary>
        public bool Checking { get; set; }

        /// <summary>Whether the blocks are read by a total of a number on them, a sum or an average, which lists only numbers.</summary>
        public bool Totals => Quantifiers.Totals(Reading);

        /// <summary>A copy of every choice as it stands, which changes to these leave as it is.</summary>
        public PickChoices Copy()
        {
            var copy = new PickChoices { Member = Member, Form = Form, Facet = Facet, Slot = Slot, Reading = Reading, Checks = Checks, Checking = Checking };
            copy.Blocks.AddRange(Blocks);
            return copy;
        }
    }
}
