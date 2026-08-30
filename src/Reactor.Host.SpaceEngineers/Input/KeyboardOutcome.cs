using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Input;

// Two interfaces share this name. VRage.Input's is the engine's own and is not whitelisted; the mod-facing
// one is what MyAPIGateway.Input hands back. Saying which removes an ambiguity that otherwise resolves to
// the wrong one and fails only at the whitelist gate.
using IMyInput = VRage.ModAPI.IMyInput;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;
using Reactor.Host.SpaceEngineers.Ui;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Input
{
    /// <summary>What a pass over the keyboard did.</summary>
    public struct KeyboardOutcome
    {
        /// <summary>The text or the caret moved, so anything watching should look again.</summary>
        public bool Changed;

        /// <summary>Enter was pressed. What that means is the caller's business.</summary>
        public bool Commit;
    }
}
