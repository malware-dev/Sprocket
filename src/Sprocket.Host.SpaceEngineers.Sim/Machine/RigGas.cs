using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Sim.Machine;

/// <summary>The gas the rig's vents move, as the mod reads it.</summary>
/// <remarks>
/// The game says it through its own concrete resource components, which a made-up vent cannot carry. Read as nothing,
/// every vent would look as if it had given up, so this answers over the rig's own vents instead.
/// </remarks>
public sealed class RigGas : IGasMeter
{
    readonly Rig _rig;

    /// <summary>The gas of <paramref name="rig" />'s vents.</summary>
    public RigGas(Rig rig)
    {
        _rig = rig;
    }

    /// <inheritdoc />
    public float In(IMyAirVent vent)
    {
        var found = Find(vent);
        return found == null ? -1f : found.In;
    }

    /// <inheritdoc />
    public float Out(IMyAirVent vent)
    {
        var found = Find(vent);
        return found == null ? -1f : found.Out;
    }

    Vent? Find(IMyAirVent vent)
    {
        foreach (var fitting in _rig.Fitted)
        {
            if (fitting.Id == vent.EntityId)
                return fitting as Vent;
        }

        return null;
    }
}
