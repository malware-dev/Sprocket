using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A spotlight, answering the game's own reflector light interface.</summary>
/// <remarks>A switch and what it carries, like a lamp; what tells it apart is the interface and its subtype.</remarks>
public sealed class Spotlight : Fitting
{
    /// <summary>A spotlight on the rig.</summary>
    public Spotlight(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyReflectorLight>());
    }
}
