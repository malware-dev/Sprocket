using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Sim.Machine;

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

    Action<IMyDoor>? _opened;
    Action<IMyDoor>? _closed;
    Action<bool>? _changed;

    public Door(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyDoor>()
            .Gives("Status", () => Status)
            .Gives("OpenRatio", () => (float)_progress / TravelTicks)
            .Does("OpenDoor", () => { Command(true, "open"); return null; })
            .Does("CloseDoor", () => { Command(false, "close"); return null; })
            .Does("ToggleDoor", () => { Command(!_opening, "toggle"); return null; })
            .Does("add_OnDoorOpened", handler => { _opened += (Action<IMyDoor>)handler[0]; })
            .Does("remove_OnDoorOpened", handler => { _opened -= (Action<IMyDoor>)handler[0]; })
            .Does("add_OnDoorClosed", handler => { _closed += (Action<IMyDoor>)handler[0]; })
            .Does("remove_OnDoorClosed", handler => { _closed -= (Action<IMyDoor>)handler[0]; })
            .Does("add_DoorStateChanged", handler => { _changed += (Action<bool>)handler[0]; })
            .Does("remove_DoorStateChanged", handler => { _changed -= (Action<bool>)handler[0]; }));
    }

    /// <summary>Whether the door raises the game's own events on finishing, as every door in the game does.</summary>
    /// <remarks>Off for a test about a door whose announcement never comes, which is what the backstop look is for.</remarks>
    public bool Announces { get; set; } = true;

    /// <summary>The game's own pair for a door's switch, which is called Open.</summary>
    public override bool Offers(string actionId)
    {
        return actionId == "Open" || actionId == "Open_On" || actionId == "Open_Off"
            || base.Offers(actionId);
    }

    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("Open", "Open/Closed"));
        into.Add(new Offer("Open_On", "Open"));
        into.Add(new Offer("Open_Off", "Closed"));
        base.Offered(into);
    }

    public override void Perform(string actionId, IList<Literal> values)
    {
        if (actionId == "Open_On")
        {
            Command(true, "open");
            return;
        }

        if (actionId == "Open_Off")
        {
            Command(false, "close");
            return;
        }

        base.Perform(actionId, values);
    }

    /// <summary>
    /// Already open, without having been told to open.
    /// </summary>
    /// <remarks>
    /// For setting a scene: a test about closing a door wants one that is open to begin with, and
    /// forty-five ticks of it travelling is not what the test is about. It announces having opened,
    /// as a door that got there by travelling does.
    /// </remarks>
    public void ForceOpen()
    {
        var was = _progress;
        Command(true, null);
        _progress = TravelTicks;

        if (was != TravelTicks && Announces)
            _opened?.Invoke((IMyDoor)Block);
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

    /// <summary>How many are listening for the door to finish opening or closing.</summary>
    public int Listeners => (_opened == null ? 0 : _opened.GetInvocationList().Length)
        + (_closed == null ? 0 : _closed.GetInvocationList().Length);

    public override void Tick()
    {
        if (!Enabled)
            return;

        if (_opening && _progress < TravelTicks)
        {
            _progress++;
            if (_progress == TravelTicks && Announces)
                _opened?.Invoke((IMyDoor)Block);
        }
        else if (!_opening && _progress > 0)
        {
            _progress--;
            if (_progress == 0 && Announces)
            {
                _changed?.Invoke(false);
                _closed?.Invoke((IMyDoor)Block);
            }
        }
    }

    /// <summary>Throws the door's switch, telling the journal <paramref name="told" /> where there is one.</summary>
    /// <remarks>
    /// As every door in the game does, it raises <c>DoorStateChanged</c> as it is told to open, and again only once it has
    /// finished closing.
    /// </remarks>
    void Command(bool opening, string? told)
    {
        var was = _opening;
        _opening = opening;
        if (told != null)
            Told(told);

        if (opening && !was && Announces)
            _changed?.Invoke(true);
    }
}
