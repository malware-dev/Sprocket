using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;
using Reactor.Host.SpaceEngineers.Ui;

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
