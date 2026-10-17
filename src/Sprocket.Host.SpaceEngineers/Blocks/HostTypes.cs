using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Every type this host provides, beside the language's own.</summary>
    public static class HostTypes
    {
        static readonly HostType[] Provided = { new ScreenType() };

        /// <summary>Every type, in the order a kind of block is described by them.</summary>
        public static IList<HostType> All => Provided;

        /// <summary>The type a slot expecting <paramref name="id" /> holds, or null where this host provides none.</summary>
        public static HostType Named(string id)
        {
            foreach (var type in Provided)
            {
                if (type.Id == id)
                    return type;
            }

            return null;
        }

        /// <summary>The type the function <paramref name="function" /> of this host returns, or null where none does.</summary>
        public static HostType Returned(string function)
        {
            foreach (var type in Provided)
            {
                if (type.Function == function)
                    return type;
            }

            return null;
        }
    }
}
