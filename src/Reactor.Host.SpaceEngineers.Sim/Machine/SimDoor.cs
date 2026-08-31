using Reactor.World;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Sim.Machine;
sealed class SimDoor(long id, string name) : SimEntity(id, name, Kinds.Door)
{
    const int TravelTicks = 45;

    int _progress;
    bool _opening;
    bool _locked;

    public override string Status => _locked
        ? _progress >= TravelTicks ? "locked open" : "locked"
        : Moving
            ? _opening ? "opening…" : "closing…"
            : _progress >= TravelTicks ? "open" : "shut";

    public override bool IsActive => Moving;

    bool Moving => _progress > 0 && _progress < TravelTicks;

    public override void Invoke(string actionId)
    {
        switch (actionId)
        {
            // A locked door ignores being told to open, which is what makes locking
            // it a real safeguard rather than a note to self.
            case "open" when !_locked:
                _opening = true;
                break;
            case "close":
                _opening = false;
                break;
            case "lock":
                _locked = true;
                Set("locked", true);
                break;
            case "unlock":
                _locked = false;
                Set("locked", false);
                break;
        }
    }

    public override void Tick()
    {
        // Locking is disabling, and a disabled door stops where it is. Lock one
        // mid-travel and it stays there, which is why closing has to actually finish
        // before locking.
        if (!_locked)
        {
            if (_opening && _progress < TravelTicks)
                _progress++;
            else if (!_opening && _progress > 0)
                _progress--;
        }

        Set("open", _progress >= TravelTicks);
        Set("shut", _progress <= 0);
        Set("moving", Moving);
    }
}
