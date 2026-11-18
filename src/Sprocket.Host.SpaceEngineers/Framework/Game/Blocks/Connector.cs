using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using Sandbox.ModAPI;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyShipConnector is declared in both, and the
// mod-side one is the one with the events on it.
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A connector, read for whether it holds a ship and which.</summary>
    /// <remarks>The connector raises its properties-changed event every hundred ticks whether anything changed or not.</remarks>
    public sealed partial class Connector : Capability
    {
        static readonly OwnValue State = OwnValue.OneOf<IMyShipConnector>(ChoiceTypes.LockStatus, connector => Position(connector.Status));

        static readonly OwnValue Docked = OwnValue.Text<IMyShipConnector>(Ship);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipConnector;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            // A ship renamed while docked is not told, and is seen at the next look.
            kind.WithValue("status", "status", State).Status().Announced().In(Category.Locking)
                .WithChange("becomes_ready_to_lock", "becomes ready to lock", "status", Literal.Name("ready-to-lock"))
                .Announced().Reading("is ready to lock", "is not ready to lock")
                .WithChange("locks", "locks", "status", Literal.Name("locked")).Announced()
                .Reading("is locked", "is not locked")
                .WithValue("docked_ship", "docked ship", Docked).Announced().In(Category.Locking)
                .Renaming("Lock", false, "lock")
                .Renaming("Unlock", false, "unlock")
                .Renaming("SwitchLock", false, "toggle lock")
                .Renaming("ThrowOut", true, "throwing out").Stating("ThrowOut")
                .Renaming("ThrowOut", false, "toggle throwing out")
                .Renaming("CollectAll", true, "collecting all").Stating("CollectAll")
                .Renaming("CollectAll", false, "toggle collecting all")
                .Renaming("AutoUnlockTime", true, "unlocks automatically after")
                .Measuring("AutoUnlockTime", " s")
                .Renaming("Strength", true, "pull strength")
                .Measuring("Strength", " %")
                .Renaming("Trading", true, "trading mode")
                .Renaming("Trading", false, "toggle trading mode")
                .Renaming("PowerTransferOverride", true, "override power transfer")
                .Renaming("PowerTransferOverride", false, "toggle override power transfer")
                .Renaming("EnableParking", true, "used for parking")
                .Renaming("EnableParking", false, "toggle used for parking");
        }

        /// <summary>Told as the connector locks or unlocks, as another comes into or goes out of reach, and as it starts or stops working.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var connector = block as IMyShipConnector;
            return connector == null ? null : new Listening(connector, told);
        }

        /// <summary>Where <paramref name="status" /> stands among the connector's statuses.</summary>
        static int Position(MyShipConnectorStatus status)
        {
            switch (status)
            {
                case MyShipConnectorStatus.Connectable:
                    return 1;
                case MyShipConnectorStatus.Connected:
                    return 2;
            }

            return 0;
        }

        /// <summary>The name of the ship locked to the connector, or empty where none is.</summary>
        static string Ship(IMyShipConnector connector)
        {
            if (connector.Status != MyShipConnectorStatus.Connected)
                return "";

            var other = connector.OtherConnector;
            var grid = other?.CubeGrid;
            return grid == null ? "" : grid.CustomName;
        }
    }
}
