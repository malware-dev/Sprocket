using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>The words earlier build stored problems and block troubles by, read so that what they stored carries over; nothing writes them.</summary>
    /// <remarks>Persisted: these are the words in saves and blocks those builds wrote.</remarks>
    public static class EarlierSpellings
    {
        static readonly string[] Reasons =
        {
            "unstartable", "source gone", "block gone", "gate gone", "unoffered", "unfilled", "mistyped", "adrift",
            "slot empty", "circular", "unable", "stuck", "stalled", "conflicted", "facet gone"
        };

        static readonly SequenceProblem[] Problems =
        {
            SequenceProblem.NeverTriggered, SequenceProblem.MissingSource, SequenceProblem.MissingTarget,
            SequenceProblem.MissingGate, SequenceProblem.Unoffered, SequenceProblem.Unfilled, SequenceProblem.Mistyped,
            SequenceProblem.Adrift, SequenceProblem.EmptySlot, SequenceProblem.Circular,
            SequenceProblem.Unable, SequenceProblem.Stuck, SequenceProblem.Stalled, SequenceProblem.Conflicted,
            SequenceProblem.MissingFacet
        };

        static readonly string[] Causes =
        {
            "vent nowhere", "vent leaking", "vent no oxygen", "vent told to fill", "vent told to empty",
            "battery told to discharge", "battery told to auto", "battery off", "battery damaged", "battery no power",
            "piston no power", "piston no head", "piston stopped", "piston reversed extending", "piston reversed retracting",
            "rotor no power", "rotor no head", "rotor locked", "rotor no torque", "rotor stopped", "rotor held",
            "tank stockpile off", "tank no power", "tank no gas",
            "jump drive recharge off",
            "script not run",
            "door told to open", "door told to close"
        };

        static readonly Cause[] Troubles =
        {
            Cause.VentNowhere, Cause.VentNotAirtight, Cause.VentNoOxygen, Cause.VentToldToFill, Cause.VentToldToEmpty,
            Cause.BatteryToldToDischarge, Cause.BatteryToldToAuto, Cause.BatteryOff, Cause.BatteryDamaged, Cause.BatteryNoPower,
            Cause.PistonNoPower, Cause.PistonNoHead, Cause.PistonStopped, Cause.PistonReversedExtending,
            Cause.PistonReversedRetracting,
            Cause.RotorNoPower, Cause.RotorNoHead, Cause.RotorLocked, Cause.RotorNoTorque, Cause.RotorStopped, Cause.RotorHeld,
            Cause.TankStockpileOff, Cause.TankNoPower, Cause.TankNoGas,
            Cause.JumpDriveRechargeOff,
            Cause.ScriptNotRun,
            Cause.DoorToldToOpen, Cause.DoorToldToClose
        };

        /// <summary>The problem an earlier build stored as <paramref name="spelled" />, or none.</summary>
        public static SequenceProblem ProblemOf(string spelled)
        {
            var at = Array.IndexOf(Reasons, spelled);
            return at < 0 ? SequenceProblem.None : Problems[at];
        }

        /// <summary>The cause an earlier build stored as <paramref name="spelled" />, or none.</summary>
        public static Cause CauseOf(string spelled)
        {
            var at = Array.IndexOf(Causes, spelled);
            return at < 0 ? Cause.None : Troubles[at];
        }
    }
}
