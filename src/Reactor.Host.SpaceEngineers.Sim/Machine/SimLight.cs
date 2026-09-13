
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
