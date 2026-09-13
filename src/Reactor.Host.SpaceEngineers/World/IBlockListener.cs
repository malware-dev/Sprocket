using Reactor.World;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.

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
