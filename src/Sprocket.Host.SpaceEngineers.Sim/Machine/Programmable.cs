using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A programmable block on the rig: the one Sprocket runs in, or one whose script a step runs, which always runs.</summary>
/// <remarks>
/// Sprocket's own is on the rig like any other block, because it is: it shows up in its own block list, and its own mod
/// storage is where the program lives.
/// </remarks>
public sealed class Programmable : Fitting
{
    /// <summary>A programmable block known by <paramref name="id" /> and called <paramref name="name" />, of the game's <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Programmable(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyProgrammableBlock>()
            .Gives("ProgramData", () => "")
            .Gives("IsRunning", () => false)
            .Does("TryRun", arguments =>
            {
                Told("run " + arguments[0]);
                return true;
            }));
    }
}
