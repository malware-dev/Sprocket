using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A cockpit or a seat, which reports who sits down or gets up, holds the flight switches and reads the ship's gravity and mass.</summary>
    public sealed partial class Cockpit : Capability
    {
        /// <summary>Whether somebody sits in it, which is Sprocket's own.</summary>
        public const string Occupied = "occupied";

        const string Unheard = "The game raises no event when this changes.";

        // One g in meters per second squared, as the game's gravity system counts it.
        const double G = 9.81;

        static readonly Reading Sat = Reading.Of<IMyCockpit>(cockpit => cockpit.IsOccupied);

        static readonly OwnValue Seated = OwnValue.Flag(Sat);

        static readonly OwnValue Occupant = OwnValue.Text<IMyCockpit>(cockpit => cockpit.Pilot == null ? "" : cockpit.Pilot.DisplayName);

        static readonly OwnValue Controlled = OwnValue.Flag(Reading.Of<IMyCockpit>(cockpit => cockpit.IsUnderControl));

        static readonly OwnValue Air = OwnValue.Number<IMyCockpit>(cockpit => cockpit.OxygenFilledRatio * 100.0);

        static readonly OwnValue Natural = OwnValue.Number<IMyCockpit>(cockpit => cockpit.GetNaturalGravity().Length() / G);

        static readonly OwnValue Artificial = OwnValue.Number<IMyCockpit>(cockpit => cockpit.GetArtificialGravity().Length() / G);

        static readonly OwnValue Mass = OwnValue.Number<IMyCockpit>(cockpit => cockpit.CalculateShipMass().TotalMass);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyCockpit && !(block is IMyCryoChamber);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue(Occupied, "occupied", Seated).Status().Announced()
                .WithChange("becomes_occupied", "becomes occupied", Occupied, Literal.Of(true))
                .Reading("is occupied", "is empty").Announced()
                .WithChange("becomes_empty", "becomes empty", Occupied, Literal.Of(false))
                .Reading("is empty", "is occupied").Announced()
                .WithValue("occupant_name", "occupant name", Occupant).Announced().In(Category.Usage)
                .WithValue("under_control", "under control", Controlled).Looked(Unheard).In(Category.Usage);

            var cockpit = block as IMyCockpit;
            if (cockpit != null && cockpit.OxygenCapacity > 0f)
                kind.WithValue("oxygen_level", "oxygen level", Air).Percent().Looked(Unheard).In(Category.LifeSupport);

            kind.WithValue("natural_gravity", "natural gravity", Natural).Measured(" g").Looked(Unheard).In(Category.Detection)
                .WithValue("artificial_gravity", "artificial gravity", Artificial).Measured(" g").Looked(Unheard).In(Category.Detection)
                .WithValue("ship_mass", "ship mass", Mass).Measured(" kg").Looked(Unheard).In(Category.State)
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
                .Renaming("MainCockpit", true, "main cockpit")
                .Renaming("MainCockpit", false, "toggle main cockpit")
                .Renaming("TargetLocking", true, "target locking")
                .Renaming("TargetLocking", false, "toggle target locking")
                .Renaming("HorizonIndicator", true, "show horizon and altitude")
                .Renaming("HorizonIndicator", false, "toggle show horizon and altitude");
        }

        /// <summary>Told as somebody sits down or gets up, dies there or loses the seat.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var cockpit = block as IMyCockpit;
            return cockpit == null ? null : new Listening(cockpit, told);
        }
    }
}
