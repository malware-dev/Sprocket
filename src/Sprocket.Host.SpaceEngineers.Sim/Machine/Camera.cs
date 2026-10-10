using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A camera on the rig, which somebody may be looking through.</summary>
public sealed class Camera : Fitting
{
    /// <summary>A camera on the rig, nobody looking.</summary>
    public Camera(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyCameraBlock>().Gives("IsActive", () => Watched));
    }

    /// <summary>Whether anybody is looking through it.</summary>
    public bool Watched { get; set; }
}
