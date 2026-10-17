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
            var item = new Item();
            if (arguments != null && arguments.Count > 0)
                item.Known = ItemIds.Read(arguments[0].Text, out item.Id);

            return item;
        }
    }
}
