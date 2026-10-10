using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A cryo chamber or bed a player sleeps and respawns in, which the game counts as a seat.</summary>
    /// <remarks>
    /// Being a seat, it keeps the ship's dampeners, handbrake and park, which act on the whole grid as from any seat. What
    /// a pilot steers and aims with does nothing in a chamber nobody flies from, so it is not offered. Ejecting the sleeper
    /// is not on the terminal, and would throw a sleeping or logged-off player out of bed on a server.
    /// </remarks>
    public sealed class Cryo : Capability
    {
        static readonly Being Sleeping = Being.Flag(Reading.Of<IMyCryoChamber>(chamber => chamber.IsOccupied));

        static readonly Being Sleeper = Being.Text<IMyCryoChamber>(chamber =>
            chamber.Pilot == null ? "" : chamber.Pilot.DisplayName);

        // The game reads the chamber's air as a fraction; every fill Sprocket says is in percent.
        static readonly Being Air = Being.Number<IMyCryoChamber>(chamber => chamber.OxygenFilledRatio * 100.0);

        static readonly Being Speed = Being.Number<IMyCryoChamber>(chamber => chamber.GetShipSpeed());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyCryoChamber;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("occupied", "occupied", Sleeping).Announced()
                .WithChange("becomes-occupied", "becomes occupied", "occupied", Literal.Of(true))
                .Reading("is occupied", "is empty").Announced()
                .WithChange("becomes-empty", "becomes empty", "occupied", Literal.Of(false))
                .Reading("is empty", "is occupied").Announced()
                .WithValue("occupant-name", "occupant name", Sleeper).Announced()
                .WithValue("oxygen-level", "oxygen level", Air)
                .Looked("A chamber announces nothing as its air rises or falls.").In(Category.Air)
                .WithValue("speed", "speed", Speed)
                .Looked("A ship announces nothing as its speed changes.").In(Category.Flight)
                .Renaming("DampenersOverride", true, "dampeners")
                .Renaming("DampenersOverride", false, "toggle dampeners")
                .Renaming("HandBrake", true, "handbrake")
                .Renaming("HandBrake", false, "toggle handbrake")
                .Renaming("Park", true, "parked")
                .Renaming("Park", false, "toggle park")
                .Renaming("MainCockpit", true, "main cockpit")
                .Renaming("MainCockpit", false, "toggle main cockpit")
                .Hiding("ControlThrusters", "ControlWheels", "ControlGyros", "TargetLocking", "HorizonIndicator");
        }

        /// <summary>Told as somebody gets in or out, which the chamber announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var chamber = block as IMyCryoChamber;
            return chamber == null ? null : new Listening(chamber, told);
        }

        /// <summary>A chamber being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyCryoChamber _chamber;
            readonly Action _told;

            public Listening(IMyCryoChamber chamber, Action told)
            {
                _chamber = chamber;
                _told = told;
                _chamber.IsOccupiedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _chamber.IsOccupiedChanged -= Changed;
            }

            void Changed(IMyCockpit cockpit)
            {
                _told();
            }
        }
    }
}
