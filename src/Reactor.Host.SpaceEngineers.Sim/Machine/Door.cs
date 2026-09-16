using Reactor.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Reactor.Sim.Machine;

/// <summary>
/// A door on the rig, which takes time to move.
/// </summary>
/// <remarks>
/// The reason the mod reads a door's status rather than its switch: "close the inner door" is
/// finished when the door is shut, and a door that shut instantly would never let that be wrong.
/// </remarks>
sealed class Door : Fitting
{
    const int TravelTicks = 45;

    int _progress;
    bool _opening;

    public Door(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyDoor>()
            .Gives("Status", () => Status)
            .Gives("OpenRatio", () => (float)_progress / TravelTicks)
            .Does("OpenDoor", () => { _opening = true; return null; })
            .Does("CloseDoor", () => { _opening = false; return null; })
            .Does("ToggleDoor", () => { _opening = !_opening; return null; }));
    }

    DoorStatus Status
    {
        get
        {
            if (_progress >= TravelTicks)
                return DoorStatus.Open;

            if (_progress <= 0)
                return DoorStatus.Closed;

            return _opening ? DoorStatus.Opening : DoorStatus.Closing;
        }
    }

    public override void Tick()
    {
        if (!Enabled)
            return;

        if (_opening && _progress < TravelTicks)
            _progress++;
        else if (!_opening && _progress > 0)
            _progress--;
    }
}
