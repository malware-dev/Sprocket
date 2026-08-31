using Reactor.World;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Sim.Machine;
sealed class SimLight(long id, string name) : SimEntity(id, name, Kinds.Light)
{
    public override string Status => Flag("on") ? "on" : "off";

    public override bool IsActive => Flag("on");

    public override void Invoke(string actionId)
    {
        if (actionId == "on")
            Set("on", true);
        else if (actionId == "off")
            Set("on", false);
    }
}
