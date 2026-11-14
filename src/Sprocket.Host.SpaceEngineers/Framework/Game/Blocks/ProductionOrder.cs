using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>What a step ordering an assembler to make something was given.</summary>
    public sealed class ProductionOrder
    {
        /// <summary>The blueprint that makes the item, where it is one the assembler can make.</summary>
        public MyDefinitionId Blueprint;

        /// <summary>How many.</summary>
        public long Amount;

        /// <summary>Whether the item is one the assembler can make.</summary>
        public bool Known;

        /// <summary>The order a step's arguments make, against the blueprints the assembler can use.</summary>
        public static ProductionOrder Read(IList<Literal> arguments, IDictionary<string, MyDefinitionId> blueprints)
        {
            var order = new ProductionOrder();
            if (arguments == null || arguments.Count < 2)
                return order;

            order.Known = blueprints.TryGetValue(arguments[0].Text ?? "", out order.Blueprint);
            order.Amount = arguments[1].Whole;
            return order;
        }
    }
}
