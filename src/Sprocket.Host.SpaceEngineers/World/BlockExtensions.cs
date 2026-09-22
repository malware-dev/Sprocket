using Sandbox.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.

namespace Sprocket.Host.SpaceEngineers.World
{
    static class BlockExtensions
    {
        static readonly IBlockExtension[] All =
        {
            new DoorExtension(),
            new AirVentExtension(),
            new ButtonPanelExtension(),
            new RotorExtension()
        };

        public static IBlockExtension For(IMyTerminalBlock block)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Claims(block))
                    return All[i];
            }

            return null;
        }
    }
}
