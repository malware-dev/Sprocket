using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A remote control's autopilot: flying its route, whether it flies, where it is heading, and how.</summary>
    /// <remarks>
    /// What a remote control shares with a cockpit, its flight switches, its seat and its hotbar, is the ship controller's.
    /// The game's own on and off for the autopilot is the order a sequence gives, and whether it flies is the game's answer,
    /// which goes off by itself at the end of a one-way route; so the order is offered as its two actions and its toggle,
    /// its value is left out, and whether it flies is Sprocket's own.
    /// </remarks>
    public sealed class RemoteControl : Capability
    {
        // Read from the game's code: the autopilot switching is a synced value of its component, and only some of the ways
        // it switches raise the block's properties-changed event.
        const string Unswitched = "The game does not announce every switch of the autopilot.";

        static readonly Being Flying = Being.Flag(Reading.Of<IMyRemoteControl>(remote => remote.IsAutoPilotEnabled));

        static readonly Being Waypoint = Being.Text<IMyRemoteControl>(remote => remote.CurrentWaypoint.Name);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyRemoteControl;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("autopilot", "autopilot", Flying).Looked(Unswitched)
                .WithValue("current-waypoint", "current waypoint", Waypoint).Announced()
                .Renaming("AutoPilot_On", false, "fly the route")
                .Renaming("AutoPilot_Off", false, "stop the autopilot")
                .Renaming("AutoPilot", false, "toggle the autopilot")
                .Renaming("Reset", false, "restart the route")
                .Renaming("FlightMode", true, "flight mode")
                .Renaming("Direction", true, "direction")
                .Renaming("MainRemoteControl", true, "main remote control")
                .Renaming("MainRemoteControl", false, "toggle main remote control")
                .Hiding("SetFlightMode_BlockPropertyTitle_FlightMode_Patrol", "SetFlightMode_BlockPropertyTitle_FlightMode_Circle",
                    "SetFlightMode_BlockPropertyTitle_FlightMode_OneWay", "Forward", "Backward", "Left", "Right", "Up", "Down");
            FlightMovement.Flying(kind);
        }

        /// <summary>Told as the autopilot's current waypoint moves on, which its component says on every route mode.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var autopilot = block.Components.Get<MyAutopilotComponent>();
            return autopilot == null ? null : new Listening(autopilot, told);
        }

        /// <summary>An autopilot being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly MyAutopilotComponent _autopilot;
            readonly Action _told;

            public Listening(MyAutopilotComponent autopilot, Action told)
            {
                _autopilot = autopilot;
                _told = told;
                _autopilot.OnCurrentWaypointIndexChanged += told;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _autopilot.OnCurrentWaypointIndexChanged -= _told;
            }
        }
    }
}
