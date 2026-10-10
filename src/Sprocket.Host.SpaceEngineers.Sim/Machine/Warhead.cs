using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A warhead on the rig: whether it is counting down, as a test sets it.</summary>
/// <remarks>Nothing counts down and nothing explodes: what is told to it is written down, and what it reads is what a test said.</remarks>
public sealed class Warhead : Fitting
{
    /// <summary>A warhead, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Warhead(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyWarhead>()
            .Gives("IsCountingDown", () => CountingDown));
    }

    /// <summary>Whether the warhead reads as counting down.</summary>
    public bool CountingDown { get; set; }
}
