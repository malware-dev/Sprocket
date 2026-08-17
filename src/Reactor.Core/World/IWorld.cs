using System.Collections.Generic;

namespace Reactor.World
{
    /// <summary>
    /// The machine Reactor is attached to, as seen from the Reactor unit.
    /// </summary>
    /// <remarks>
    /// Scope is the host's responsibility, and it is not negotiable at this level:
    /// <see cref="Devices"/> contains this construct only - the grid the Reactor unit
    /// sits on plus anything mechanically connected to it by rotors, pistons and
    /// hinges. Connectors, landing gear and magplates are docking, not construction,
    /// so whatever is on the other side of them is somebody else's machine and never
    /// appears here.
    ///
    /// That rule is what stops a rule's meaning from changing underneath the user
    /// when they dock, which in Space Engineers is a routine event rather than an
    /// edge case.
    /// </remarks>
    public interface IWorld
    {
        /// <summary>
        /// Everything on this construct, right now. Re-read rather than cached -
        /// blocks get welded on and shot off while the program is running.
        /// </summary>
        IReadOnlyList<IDevice> Devices { get; }

        /// <summary>
        /// Looks up a device by id, or returns null if it is no longer part of this
        /// construct.
        /// </summary>
        /// <remarks>
        /// Null here is a state the UI must surface loudly rather than skip over. A
        /// rule that quietly refers to a door that no longer exists is
        /// indistinguishable from a rule that is working, which is exactly the kind
        /// of silent failure that makes automation impossible to trust.
        /// </remarks>
        IDevice FindDevice(long deviceId);

        /// <summary>Tells a device to do something.</summary>
        void Invoke(IDevice device, string actionId);
    }
}
