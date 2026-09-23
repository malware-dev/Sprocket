using Sprocket.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// An interior light, answering the game's own light interface.
/// </summary>
/// <remarks>
/// A switch and what it carries, like any plain block - and a light to the mod, so what the mod knows
/// about lights applies to it as it does in the game: the colour and radius setters written beside the
/// light's own values are left to the terminal view.
/// </remarks>
sealed class Lamp : Fitting
{
    public Lamp(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyInteriorLight>());
    }
}
