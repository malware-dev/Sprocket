using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>How a system's slots stand on the installation panel: their order, the category each is under, and the order of the categories.</summary>
    public sealed class SlotArrangement
    {
        /// <summary>Every slot of the system, in its order.</summary>
        public List<Part> Order { get; } = new List<Part>();

        /// <summary>The category each slot of <see cref="Order" /> is under, at the same place, empty for none.</summary>
        public List<string> Placed { get; } = new List<string>();

        /// <summary>The system's categories, in the order they are shown.</summary>
        public List<string> Categories { get; } = new List<string>();

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

            arrangement.Categories.AddRange(system.Categories);
            return arrangement;
        }

        /// <summary>Stands the slots of <paramref name="system" /> as this says.</summary>
        public void Put(AutomationSystem system)
        {
            system.Arrange(Order);
            for (var i = 0; i < Order.Count; i++)
                Order[i].Category = Placed[i];

            var categories = system.Categories;
            categories.Clear();
            for (var i = 0; i < Categories.Count; i++)
                categories.Add(Categories[i]);
        }

        /// <summary>Whether the slots of <paramref name="system" /> stand as this says.</summary>
        public bool Matches(AutomationSystem system)
        {
            var parts = system.Parts;
            if (parts.Count != Order.Count || system.Categories.Count != Categories.Count)
                return false;

            for (var i = 0; i < Order.Count; i++)
            {
                if (parts[i] != Order[i] || parts[i].Category != Placed[i])
                    return false;
            }

            for (var i = 0; i < Categories.Count; i++)
            {
                if (system.Categories[i] != Categories[i])
                    return false;
            }

            return true;
        }

        /// <summary>Whether <paramref name="other" /> stands the same slots the same way.</summary>
        public bool Same(SlotArrangement other)
        {
            if (other.Order.Count != Order.Count || other.Categories.Count != Categories.Count)
                return false;

            for (var i = 0; i < Order.Count; i++)
            {
                if (other.Order[i] != Order[i] || other.Placed[i] != Placed[i])
                    return false;
            }

            for (var i = 0; i < Categories.Count; i++)
            {
                if (other.Categories[i] != Categories[i])
                    return false;
            }

            return true;
        }
    }
}
