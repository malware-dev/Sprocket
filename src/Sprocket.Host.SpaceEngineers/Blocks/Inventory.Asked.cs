using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Inventory
    {
        /// <summary>The inventory and the item a reading of an amount was given, read once when it is compiled.</summary>
        sealed class Asked
        {
            /// <summary>The inventory's number, counted from 1; 0 where none was given.</summary>
            public int Number;

            /// <summary>The item.</summary>
            public Item Item;

            /// <summary>What <paramref name="arguments" /> ask for.</summary>
            public static Asked Of(IList<Literal> arguments)
            {
                return new Asked
                {
                    Number = arguments != null && arguments.Count > 0 ? (int)arguments[0].Number : 0,
                    Item = Item.At(arguments, 1)
                };
            }
        }
    }
}
