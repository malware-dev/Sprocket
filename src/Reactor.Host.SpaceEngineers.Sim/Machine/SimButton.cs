using Reactor.World;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Sim.Machine;
/// <summary>
/// A button. Its signal is true for a single tick, which is what makes "is pressed"
/// a moment rather than a state.
/// </summary>
sealed class SimButton(long id, string name) : SimEntity(id, name, Kinds.Button)
{
    bool _queued;

    public override string Status => "ready";

    public void Press() => _queued = true;

    public override void Tick()
    {
        Set("pressed", _queued);
        _queued = false;
    }
}
