using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A block with nothing but a switch: a light, a sensor, a tank.
/// </summary>
/// <remarks>
/// What the mod gives every block before it knows what the block is - whether it is on, whether it
/// is working, and the two actions that change the first. A light is exactly that and nothing more,
/// which is worth having on the rig: most of a ship is.
/// </remarks>
public sealed class Plain : Fitting
{
    /// <summary>A plain block known by <paramref name="id" /> and called <paramref name="name" />, of the game's <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Plain(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyFunctionalBlock>());
    }
}
