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
            .Does("OpenDoor", () => { _opening = true; Told("open"); return null; })
            .Does("CloseDoor", () => { _opening = false; Told("close"); return null; })
            .Does("ToggleDoor", () => { _opening = !_opening; Told("toggle"); return null; }));
    }

    /// <summary>
    /// Already open, without having been told to open.
    /// </summary>
    /// <remarks>
    /// For setting a scene: a test about closing a door wants one that is open to begin with, and
    /// forty-five ticks of it travelling is not what the test is about.
    /// </remarks>
    public void ForceOpen()
    {
        _progress = TravelTicks;
        _opening = true;
    }

    /// <summary>What the game would report of this door, which is what the mod reads.</summary>
    public DoorStatus Status
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
