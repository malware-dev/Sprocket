using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;
using Reactor.Host.SpaceEngineers.Terminal;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>Holds whatever a block told us since the last tick.</summary>
    interface IBlockListener
    {
        bool TryRead(string propertyId, out EntityValue value);

        /// <summary>Forgets a moment, once a tick has had the chance to see it.</summary>
        void Clear();

        void Dispose();
    }
}
