using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A cryo chamber or bed a player sleeps and respawns in, which the game counts as a seat.</summary>
    public sealed partial class CryoChamber : Capability
    {
        static readonly OwnValue Sleeping = OwnValue.Flag(Reading.Of<IMyCryoChamber>(chamber => chamber.IsOccupied));

        static readonly OwnValue Sleeper = OwnValue.Text<IMyCryoChamber>(chamber =>
            chamber.Pilot == null ? "" : chamber.Pilot.DisplayName);

        // The game reads the chamber's air as a fraction; every fill Sprocket says is in percent.
        static readonly OwnValue Air = OwnValue.Number<IMyCryoChamber>(chamber => chamber.OxygenFilledRatio * 100.0);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyCryoChamber;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("occupied", "occupied", Sleeping).Status().Announced()
                .WithChange("becomes_occupied", "becomes occupied", "occupied", Literal.Of(true))
                .Reading("is occupied", "is empty").Announced()
                .WithChange("becomes_empty", "becomes empty", "occupied", Literal.Of(false))
                .Reading("is empty", "is occupied").Announced()
                .WithValue("occupant_name", "occupant name", Sleeper).Announced().In(Category.Usage)
                .WithValue("oxygen_level", "oxygen level", Air).Percent()
                .Looked("A chamber announces nothing as its air rises or falls.").In(Category.LifeSupport)
                .Renaming("DampenersOverride", true, "dampeners")
                .Renaming("DampenersOverride", false, "toggle dampeners")
                .Renaming("HandBrake", true, "handbrake")
                .Renaming("HandBrake", false, "toggle handbrake")
                .Renaming("Park", true, "parking brake")
                .Renaming("Park", false, "toggle parking brake")
                .Renaming("MainCockpit", true, "main cockpit")
                .Renaming("MainCockpit", false, "toggle main cockpit")
                .Hiding("ControlThrusters", "ControlWheels", "ControlGyros", "TargetLocking", "HorizonIndicator");
        }

        /// <summary>Told as somebody gets in or out.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var chamber = block as IMyCryoChamber;
            return chamber == null ? null : new Listening(chamber, told);
        }
    }
}
