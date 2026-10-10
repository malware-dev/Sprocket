using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A cockpit or a seat: it knows when somebody sits down or gets up and who that is, holds the flight switches a docking
    /// or landing sequence sets, and reads the ship's gravity and mass.
    /// </summary>
    /// <remarks>
    /// A cryo chamber is a cockpit to the game, and is not this: it is judged on its own. Its hotbar and target lock are the
    /// signals and the speed every ship controller has - see <see cref="ShipController" />.
    /// <para>
    /// The handbrake and parking switches belong to the grid, and the game's setter for each flips it whatever it is told,
    /// so the terminal floor reads them before it sets them - see <c>GameSlots</c>.
    /// </para>
    /// </remarks>
    public sealed class Cockpit : Capability
    {
        /// <summary>Whether somebody sits in it, which is Sprocket's own.</summary>
        public const string Occupied = "occupied";

        const string Unheard = "The game raises no event when this changes.";

        /// <summary>One g in metres a second squared, as the game's gravity system counts it.</summary>
        const double G = 9.81;

        static readonly Reading Sat = Reading.Of<IMyCockpit>(cockpit => cockpit.IsOccupied);

        static readonly Being Seated = Being.Flag(Sat);

        static readonly Being Occupant = Being.Text<IMyCockpit>(cockpit => cockpit.Pilot == null ? "" : cockpit.Pilot.DisplayName);

        static readonly Being Controlled = Being.Flag(Reading.Of<IMyCockpit>(cockpit => cockpit.IsUnderControl));

        static readonly Being Air = Being.Number<IMyCockpit>(cockpit => cockpit.OxygenFilledRatio * 100.0);

        static readonly Being Gravity = Being.Number<IMyCockpit>(cockpit => cockpit.GetNaturalGravity().Length() / G);

        static readonly Being Mass = Being.Number<IMyCockpit>(cockpit => cockpit.CalculateShipMass().TotalMass);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyCockpit && !(block is IMyCryoChamber);
        }

        /// <inheritdoc />
        /// <remarks>Only an enclosed cockpit has air of its own, and only it is offered its oxygen level.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue(Occupied, "occupied", Seated).Announced()
                .WithChange("becomes-occupied", "becomes occupied", Occupied, Literal.Of(true))
                .Reading("is occupied", "is empty").Announced()
                .WithChange("becomes-empty", "becomes empty", Occupied, Literal.Of(false))
                .Reading("is empty", "is occupied").Announced()
                .WithValue("occupant-name", "occupant name", Occupant).Announced()
                .WithValue("under-control", "under control", Controlled).Looked(Unheard);

            var cockpit = block as IMyCockpit;
            if (cockpit != null && cockpit.OxygenCapacity > 0f)
                kind.WithValue("oxygen-level", "oxygen level", Air).Looked(Unheard);

            kind.WithValue("gravity", "gravity", Gravity).Looked(Unheard)
                .WithValue("ship-mass", "ship mass", Mass).Looked(Unheard)
                .Renaming("HandBrake", true, "handbrake")
                .Renaming("HandBrake", false, "toggle handbrake")
                .Renaming("Park", true, "parked")
                .Renaming("Park", false, "toggle parked")
                .Renaming("DampenersOverride", true, "dampeners")
                .Renaming("DampenersOverride", false, "toggle dampeners")
                .Renaming("ControlThrusters", true, "controls thrusters")
                .Renaming("ControlThrusters", false, "toggle controls thrusters")
                .Renaming("ControlWheels", true, "controls wheels")
                .Renaming("ControlWheels", false, "toggle controls wheels")
                .Renaming("ControlGyros", true, "controls gyroscopes")
                .Renaming("ControlGyros", false, "toggle controls gyroscopes")
                .Renaming("MainCockpit", true, "main cockpit")
                .Renaming("MainCockpit", false, "toggle main cockpit")
                .Renaming("TargetLocking", true, "target locking allowed")
                .Renaming("TargetLocking", false, "toggle target locking allowed")
                .Renaming("HorizonIndicator", true, "shows horizon")
                .Renaming("HorizonIndicator", false, "toggle shows horizon");
        }

        /// <summary>Told as somebody sits down or gets up, which every cockpit announces, on getting up, dying or the seat being lost alike.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var cockpit = block as IMyCockpit;
            return cockpit == null ? null : new Listening(cockpit, told);
        }

        /// <summary>A seat being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyCockpit _cockpit;
            readonly Action _told;

            public Listening(IMyCockpit cockpit, Action told)
            {
                _cockpit = cockpit;
                _told = told;
                _cockpit.IsOccupiedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _cockpit.IsOccupiedChanged -= Changed;
            }

            void Changed(IMyCockpit cockpit)
            {
                _told();
            }
        }
    }
}
