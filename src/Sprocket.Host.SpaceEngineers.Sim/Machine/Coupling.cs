using System;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Sprocket.Sim.Machine;

/// <summary>A connector on the rig, which a test locks and unlocks as the game would, with nothing on the other side.</summary>
/// <remarks>Announces each change of its status through <c>IsConnectedChanged</c>, as the game does.</remarks>
public sealed class Coupling : Fitting
{
    MyShipConnectorStatus _status = MyShipConnectorStatus.Unconnected;
    Action<IMyShipConnector>? _changed;

    /// <summary>An unlocked connector, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    public Coupling(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        var connector = Pretend.To<IMyShipConnector>()
            .Gives("Status", () => _status)
            .Gives("IsConnected", () => _status == MyShipConnectorStatus.Connected)
            .Gives("OtherConnector", () => null)
            .Does("add_IsConnectedChanged", handler => { _changed += (Action<IMyShipConnector>)handler[0]; })
            .Does("remove_IsConnectedChanged", handler => { _changed -= (Action<IMyShipConnector>)handler[0]; })
            .Does("add_AttachFinished", () => null)
            .Does("remove_AttachFinished", () => null)
            .Does("add_DetachFinished", () => null)
            .Does("remove_DetachFinished", () => null);

        Fit(connector);
    }

    /// <summary>Puts the connector in <paramref name="status" />, announcing the change as the game does.</summary>
    public void Lock(MyShipConnectorStatus status)
    {
        if (_status == status)
            return;

        _status = status;
        _changed?.Invoke((IMyShipConnector)Block);
    }
}
