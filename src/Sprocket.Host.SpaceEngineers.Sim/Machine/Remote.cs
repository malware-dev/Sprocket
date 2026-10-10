using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A remote control on the rig, which can fly the ship.</summary>
/// <remarks>Nothing flies: the rig models no flight.</remarks>
public sealed class Remote : Fitting
{
    /// <summary>A remote control, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Remote(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyRemoteControl>()
            .Gives("CanControlShip", () => true));
    }
}
