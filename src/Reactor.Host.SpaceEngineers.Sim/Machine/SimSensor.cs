
namespace Reactor.Sim.Machine;
/// <summary>A sensor the tester trips by hand, standing in for a ship flying up.</summary>
sealed class SimSensor(long id, string name) : SimEntity(id, name, Kinds.Sensor)
{
    public override string Status => Flag("detecting") ? "detecting" : "clear";

    public override bool IsActive => Flag("detecting");

    public void SetDetecting(bool detecting) => Set("detecting", detecting);
}
