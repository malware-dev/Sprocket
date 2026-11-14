using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>How a system's slots stand on the installation panel: their order, the category each is under, and the categories with none.</summary>
    public sealed class SlotArrangement
    {
        /// <summary>Every slot of the system, in its order.</summary>
        public List<Part> Order { get; } = new List<Part>();

        /// <summary>The category each slot of <see cref="Order" /> is under, at the same place, empty for none.</summary>
        public List<string> Placed { get; } = new List<string>();

        /// <summary>The categories with no slot under them.</summary>
        public List<EmptyCategory> Empty { get; } = new List<EmptyCategory>();

        /// <summary>How the slots of <paramref name="system" /> stand now.</summary>
        public static SlotArrangement Of(AutomationSystem system)
        {
            var arrangement = new SlotArrangement();
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                arrangement.Order.Add(parts[i]);
                arrangement.Placed.Add(parts[i].Category);
            }

            arrangement.Empty.AddRange(system.EmptyCategories);
            return arrangement;
        }

        /// <summary>Stands the slots of <paramref name="system" /> as this says.</summary>
        public void Put(AutomationSystem system)
        {
            system.Arrange(Order);
            for (var i = 0; i < Order.Count; i++)
                Order[i].Category = Placed[i];

            var empty = system.EmptyCategories;
            empty.Clear();
            for (var i = 0; i < Empty.Count; i++)
                empty.Add(Empty[i]);
        }

        /// <summary>Whether the slots of <paramref name="system" /> stand as this says.</summary>
        public bool Matches(AutomationSystem system)
        {
            return Same(Of(system));
        }

        /// <summary>Whether <paramref name="other" /> stands the same slots the same way.</summary>
        public bool Same(SlotArrangement other)
        {
            if (other.Order.Count != Order.Count || other.Empty.Count != Empty.Count)
                return false;

            for (var i = 0; i < Order.Count; i++)
            {
                if (other.Order[i] != Order[i] || other.Placed[i] != Placed[i])
                    return false;
            }

            for (var i = 0; i < Empty.Count; i++)
            {
                if (other.Empty[i] != Empty[i])
                    return false;
            }

            return true;
        }
    }
}
