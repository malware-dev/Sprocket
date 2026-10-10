using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyShipConnector is declared in both, and the
// mod-side one is the one with the events on it.
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A connector: whether it holds a ship and which, and the members of its terminal said in Sprocket's words.</summary>
    /// <remarks>
    /// Lock and unlock are the game's own and take effect at once, so neither is waited for. The connector says its
    /// properties changed every hundred ticks whether anything did or not, so that is no cue for anything here.
    /// </remarks>
    public sealed class Connector : Capability
    {
        static readonly string[] Statuses = { "unlocked", "ready-to-lock", "locked" };

        static readonly Being State = Being.OneOf<IMyShipConnector>(Statuses, connector => Position(connector.Status));

        static readonly Being Docked = Being.Text<IMyShipConnector>(Ship);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipConnector;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            // A ship renamed while docked is not told, and is seen at the next look.
            kind.WithValue("status", "status", State).Announced().In(Category.Locking)
                .WithChange("becomes-ready-to-lock", "becomes ready to lock", "status", Literal.Name("ready-to-lock"))
                .Announced().Reading("is ready to lock", "is not ready to lock")
                .WithChange("locks", "locks", "status", Literal.Name("locked")).Announced()
                .Reading("is locked", "is not locked")
                .WithValue("docked-ship", "docked ship", Docked).Announced().In(Category.Locking)
                .Renaming("Lock", false, "lock")
                .Renaming("Unlock", false, "unlock")
                .Renaming("SwitchLock", false, "toggle lock")
                .Renaming("ThrowOut", true, "throwing out")
                .Renaming("ThrowOut", false, "toggle throwing out")
                .Renaming("CollectAll", true, "collecting all")
                .Renaming("CollectAll", false, "toggle collecting all")
                .Renaming("AutoUnlockTime", true, "unlocks automatically after")
                .Renaming("Strength", true, "pull strength")
                .Renaming("Trading", true, "trading mode")
                .Renaming("Trading", false, "toggle trading mode")
                .Renaming("PowerTransferOverride", true, "power transfer override")
                .Renaming("PowerTransferOverride", false, "toggle power transfer override")
                .Renaming("EnableParking", true, "used for parking")
                .Renaming("EnableParking", false, "toggle used for parking");
        }

        /// <summary>
        /// Told as the connector locks or unlocks, as another comes into or goes out of reach, and as it starts or stops
        /// working, since one that is not working is never ready.
        /// </summary>
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
            var grid = other == null ? null : other.CubeGrid;
            return grid == null ? "" : grid.CustomName;
        }

        /// <summary>A connector being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyShipConnector _connector;
            readonly Action _told;

            public Listening(IMyShipConnector connector, Action told)
            {
                _connector = connector;
                _told = told;
                _connector.IsConnectedChanged += Changed;
                _connector.AttachFinished += Changed;
                _connector.DetachFinished += Changed;
                _connector.IsWorkingChanged += Working;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _connector.IsConnectedChanged -= Changed;
                _connector.AttachFinished -= Changed;
                _connector.DetachFinished -= Changed;
                _connector.IsWorkingChanged -= Working;
            }

            void Changed(IMyShipConnector connector)
            {
                _told();
            }

            void Working(IMyCubeBlock block)
            {
                _told();
            }
        }
    }
}
