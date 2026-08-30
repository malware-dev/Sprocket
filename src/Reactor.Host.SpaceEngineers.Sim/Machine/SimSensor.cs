using Reactor.World;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Machine;
/// <summary>A sensor the tester trips by hand, standing in for a ship flying up.</summary>
sealed class SimSensor(long id, string name) : SimEntity(id, name, Kinds.Sensor)
{
    public override string Status => Flag("detecting") ? "detecting" : "clear";

    public override bool IsActive => Flag("detecting");

    public void SetDetecting(bool detecting) => Set("detecting", detecting);
}
