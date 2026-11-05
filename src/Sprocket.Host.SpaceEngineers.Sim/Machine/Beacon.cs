using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A beacon on the rig, which is here for the word it shares with a light.
/// </summary>
/// <remarks>
/// A beacon's radius is how far it is heard and a light's is how far it shines, and the game calls
/// both of them <c>Radius</c>. A rig that only had lights on it could not tell whether the one
/// category the id lands in was chosen or merely inherited.
/// </remarks>
public sealed class Beacon : Fitting
{
    /// <summary>A beacon known by <paramref name="id" /> and called <paramref name="name" />, of the game's <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Beacon(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyBeacon>());
    }
}
