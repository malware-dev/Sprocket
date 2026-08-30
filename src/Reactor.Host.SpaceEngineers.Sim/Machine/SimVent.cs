using Reactor.World;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Machine;
sealed class SimVent(long id, string name) : SimEntity(id, name, Kinds.Vent)
{
    const int CycleTicks = 90;

    int _progress = CycleTicks;
    bool _pressurizing = true;

    public override string Status => Cycling
        ? _pressurizing ? "filling…" : "emptying…"
        : _progress >= CycleTicks ? "has air" : "empty";

    public override bool IsActive => Cycling;

    bool Cycling => _progress > 0 && _progress < CycleTicks;

    public override void Invoke(string actionId)
    {
        if (actionId == "pressurize")
            _pressurizing = true;
        else if (actionId == "depressurize")
            _pressurizing = false;
    }

    public override void Tick()
    {
        if (_pressurizing && _progress < CycleTicks)
            _progress++;
        else if (!_pressurizing && _progress > 0)
            _progress--;

        Set("air", _progress >= CycleTicks);
        Set("empty", _progress <= 0);
    }
}
