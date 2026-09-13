
// Two interfaces share this name. VRage.Input's is the engine's own and is not whitelisted; the mod-facing
// one is what MyAPIGateway.Input hands back. Saying which removes an ambiguity that otherwise resolves to
// the wrong one and fails only at the whitelist gate.

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
