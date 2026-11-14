using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A remote control's autopilot: flying its route, whether it flies, where it is heading, and how.</summary>
    public sealed partial class RemoteControl : Capability
    {
        // Read from the game's code: the autopilot switching is a synced value of its component, and only some of the ways
        // it switches raise the block's properties-changed event.
        const string Unswitched = "The game does not announce every switch of the autopilot.";

        static readonly OwnValue Flying = OwnValue.Flag(Reading.Of<IMyRemoteControl>(remote => remote.IsAutoPilotEnabled));

        static readonly OwnValue Waypoint = OwnValue.Text<IMyRemoteControl>(remote => remote.CurrentWaypoint.Name);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyRemoteControl;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("autopilot", "flying the route", Flying).Status().Looked(Unswitched)
                .WithValue("current_waypoint", "current waypoint", Waypoint).Announced().In(Category.Flight)
                .Renaming("HandBrake", true, "handbrake")
                .Renaming("HandBrake", false, "toggle handbrake")
                .Renaming("Park", true, "parking brake")
                .Renaming("Park", false, "toggle parking brake")
                .Renaming("DampenersOverride", true, "dampeners")
                .Renaming("DampenersOverride", false, "toggle dampeners")
                .Renaming("ControlThrusters", true, "control thrusters")
                .Renaming("ControlThrusters", false, "toggle control thrusters")
                .Renaming("ControlWheels", true, "control wheels")
                .Renaming("ControlWheels", false, "toggle control wheels")
                .Renaming("ControlGyros", true, "control gyros")
                .Renaming("ControlGyros", false, "toggle control gyros")
                .Renaming("HorizonIndicator", true, "show horizon and altitude")
                .Renaming("HorizonIndicator", false, "toggle show horizon and altitude")
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
    }
}
