using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The item a step or a test was given, read once when it is compiled.</summary>
    public sealed class Item
    {
        /// <summary>The item, where <see cref="Known" />.</summary>
        public MyDefinitionId Id;

        /// <summary>Whether it names an item the game has.</summary>
        public bool Known;

        /// <summary>The item the first of <paramref name="arguments" /> names.</summary>
        public static Item First(IList<Literal> arguments)
        {
            return At(arguments, 0);
        }

        /// <summary>The item the argument at <paramref name="at" /> of <paramref name="arguments" /> names.</summary>
        public static Item At(IList<Literal> arguments, int at)
        {
            var item = new Item();
            if (arguments != null && arguments.Count > at)
                item.Known = ItemIds.Read(arguments[at].Text, out item.Id);

            return item;
        }
    }
}
