using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>An emotion controller on the rig, offering one action for each face as the game does.</summary>
/// <remarks>The game names each face's action by the face's texture path, under the emotes folder.</remarks>
public sealed class Emoter : Fitting
{
    /// <summary>An emotion controller on the rig with a happy and a sad face, and one action that is no face.</summary>
    public Emoter(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyEmotionControllerBlock>());
        Offering(@"Textures\Models\Emotes\Happy.dds", "Happy");
        Offering(@"Textures\Models\Emotes\Sad.dds", "Sad");
        Offering("Blink", "Blink");
    }
}
