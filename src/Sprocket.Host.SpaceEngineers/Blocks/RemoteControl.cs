using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A remote control's autopilot: flying its route, whether it flies, where it is heading, and how.</summary>
    public sealed class RemoteControl : Capability
    {
        // Read from the game's code: the autopilot switching is a synced value of its component, and only some of the ways
        // it switches raise the block's properties-changed event.
        const string Unswitched = "The game does not announce every switch of the autopilot.";

        static readonly OwnValue Flying = OwnValue.Flag(Reading.Of<IMyRemoteControl>(remote => remote.IsAutoPilotEnabled));

        static readonly OwnValue Waypoint = OwnValue.Text<IMyRemoteControl>(remote => remote.CurrentWaypoint.Name);

        // The flight switches the game's terminal shows on a remote control, each a value and the toggle that flips it.
        static readonly string[] Switches =
        {
            "HandBrake", "Park", "DampenersOverride", "ControlThrusters", "ControlWheels", "ControlGyros", "HorizonIndicator"
        };

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyRemoteControl;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("autopilot", "flying the route", Flying).Status().Looked(Unswitched)
                .WithValue("current-waypoint", "current waypoint", Waypoint).Announced().In(Category.Flight)
                .Vetting(true, Switches)
                .Vetting(false, Switches)
                .Renaming("AutoPilot_On", false, "fly the route")
                .Renaming("AutoPilot_Off", false, "stop the autopilot")
                .Renaming("AutoPilot", false, "toggle the autopilot")
                .Renaming("Reset", false, "reset waypoint")
                .Renaming("FlightMode", true, "flight mode")
                .Renaming("Direction", true, "forward direction")
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
