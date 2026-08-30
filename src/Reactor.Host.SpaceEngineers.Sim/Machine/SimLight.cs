using Reactor.World;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

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
