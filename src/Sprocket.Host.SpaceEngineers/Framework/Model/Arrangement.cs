using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>How a list of a system's things stands on its screen: their order, the category each is under, and the categories with none.</summary>
    public sealed class Arrangement
    {
        /// <summary>Every one of the list, in its order.</summary>
        public List<ICategorized> Order { get; } = new List<ICategorized>();

        /// <summary>The category each of <see cref="Order" /> is under, at the same place, empty for none.</summary>
        public List<string> Placed { get; } = new List<string>();

        /// <summary>The id each of <see cref="Order" /> runs one at a time with, at the same place, empty for none or where it cannot.</summary>
        public List<string> Held { get; } = new List<string>();

        /// <summary>The categories with nothing under them.</summary>
        public List<EmptyCategory> Empty { get; } = new List<EmptyCategory>();

        /// <summary>How <paramref name="list" /> stands now.</summary>
        public static Arrangement Of(ICategorizedList list)
        {
            var arrangement = new Arrangement();
            var items = list.Items;
            for (var i = 0; i < items.Count; i++)
            {
                arrangement.Order.Add(items[i]);
                arrangement.Placed.Add(items[i].Category);
                arrangement.Held.Add(HeldOf(items[i]));
            }

            arrangement.Empty.AddRange(list.Empty);
            return arrangement;
        }

        /// <summary>The id <paramref name="item" /> runs one at a time with, or empty for none or where it cannot.</summary>
        public static string HeldOf(ICategorized item)
        {
            var exclusive = item as IExclusive;
            return exclusive != null ? exclusive.Exclusive : "";
        }

        /// <summary>Stands <paramref name="list" /> as this says.</summary>
        public void Put(ICategorizedList list)
        {
            list.Arrange(Order);
            for (var i = 0; i < Order.Count; i++)
            {
                Order[i].Category = Placed[i];
                var exclusive = Order[i] as IExclusive;
                if (exclusive != null)
                    exclusive.Exclusive = Held[i];
            }

            var empty = list.Empty;
            empty.Clear();
            for (var i = 0; i < Empty.Count; i++)
                empty.Add(Empty[i]);
        }

        /// <summary>Whether <paramref name="list" /> stands as this says.</summary>
        public bool Matches(ICategorizedList list)
        {
            return Same(Of(list));
        }

        /// <summary>Whether <paramref name="other" /> stands the same things the same way.</summary>
        public bool Same(Arrangement other)
        {
            if (other.Order.Count != Order.Count || other.Empty.Count != Empty.Count)
                return false;

            for (var i = 0; i < Order.Count; i++)
            {
                if (other.Order[i] != Order[i] || other.Placed[i] != Placed[i] || other.Held[i] != Held[i])
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
