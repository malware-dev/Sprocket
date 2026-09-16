using Reactor.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Reactor.Sim.Machine;

/// <summary>
/// The programmable block Reactor itself runs in.
/// </summary>
/// <remarks>
/// On the rig like any other block, because it is: it shows up in its own block list, and its own
/// mod storage is where the program lives.
/// </remarks>
sealed class Programmable : Fitting
{
    public Programmable(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyProgrammableBlock>()
            .Gives("ProgramData", () => "")
            .Gives("IsRunning", () => false));
    }
}
