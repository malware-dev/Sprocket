using Reactor.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Reactor.Sim.Machine;

/// <summary>An air vent on the rig, which takes time to fill and to empty.</summary>
sealed class Vent : Fitting
{
    const int CycleTicks = 90;

    int _progress = CycleTicks;
    bool _pressurizing = true;

    public Vent(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyAirVent>()
            .Gives("Status", () => Status)
            .Gives("CanPressurize", () => true)
            .Gives("Depressurize", () => !_pressurizing)
            .Takes("Depressurize", value => _pressurizing = !(bool)value)
            .Does("GetOxygenLevel", () => (float)_progress / CycleTicks));
    }

    VentStatus Status
    {
        get
        {
            if (_progress >= CycleTicks)
                return VentStatus.Pressurized;

            if (_progress <= 0)
                return VentStatus.Depressurized;

            return _pressurizing ? VentStatus.Pressurizing : VentStatus.Depressurizing;
        }
    }

    public override void Tick()
    {
        if (!Enabled)
            return;

        if (_pressurizing && _progress < CycleTicks)
            _progress++;
        else if (!_pressurizing && _progress > 0)
            _progress--;
    }
}
