using System;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;

namespace Sprocket.Sim.Machine;

/// <summary>A railgun's capacitor on the rig, charged by a test, and found among its block's components as the game's is.</summary>
/// <remarks>With no power on the grid the game gives the time remaining as infinite, measured on the dedicated server.</remarks>
public sealed class Capacitor : MyComponentBase, IMyEntityCapacitorComponent
{
    bool _charged;

    /// <inheritdoc />
    public float Capacity { get; set; } = 0.5f;

    /// <inheritdoc />
    public float RechargeDraw { get; set; }

    /// <inheritdoc />
    public float StoredPower { get; set; }

    /// <inheritdoc />
    public float TimeRemaining { get; set; } = float.PositiveInfinity;

    /// <inheritdoc />
    public bool IsCharged => _charged;

    /// <inheritdoc />
    public event Action<IMyEntityCapacitorComponent>? OnCharged;

    /// <inheritdoc />
    public event Action<IMyEntityCapacitorComponent>? OnNotCharged;

    /// <summary>Holds <paramref name="stored" /> of its capacity with <paramref name="remaining" /> seconds to go, as a capacitor charging on power does.</summary>
    public void Charging(float stored, float remaining)
    {
        StoredPower = stored;
        TimeRemaining = remaining;
        Charged(false);
    }

    /// <summary>Stops charging where it stands, as a capacitor on a grid with no power does.</summary>
    public void Unpowered()
    {
        TimeRemaining = float.PositiveInfinity;
        Charged(false);
    }

    /// <summary>Stops charging with no time to go, as the capacitor of a railgun switched off does, measured on the dedicated server.</summary>
    public void SwitchedOff()
    {
        TimeRemaining = 0f;
        Charged(false);
    }

    /// <summary>Fills up, as a capacitor does once its time runs out.</summary>
    public void Full()
    {
        StoredPower = Capacity;
        TimeRemaining = 0f;
        Charged(true);
    }

    void Charged(bool charged)
    {
        if (_charged == charged)
            return;

        _charged = charged;
        if (charged)
            OnCharged?.Invoke(this);
        else
            OnNotCharged?.Invoke(this);
    }
}
