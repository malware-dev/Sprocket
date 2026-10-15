using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Sim.Machine;

/// <summary>A block with two screens, front and back, as a billboard has.</summary>
public sealed class Billboard : Fitting
{
    /// <summary>A billboard on the rig, its two screens blank.</summary>
    public Billboard(long id, string name)
        : base(id, name, typeof(MyObjectBuilder_LCDPanelsBlock), "LargeBlockBillboard", "Billboard")
    {
        Fit(Pretend.To<IMyFunctionalBlock>()
            .Also<IMyTextSurfaceProvider>()
            .Gives("UseGenericLcd", () => true)
            .Gives("SurfaceCount", () => 2)
            .Does("GetSurface", arguments => Faces[(int)arguments[0]].Surface));
    }

    /// <summary>Its two screens.</summary>
    public Face[] Faces { get; } = [new Face("Front"), new Face("Back")];
}
