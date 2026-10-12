using System;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>How what went wrong is kept: each problem and each block's trouble by a spelling that outlives the enums that hold them.</summary>
    /// <remarks>Persisted: a spelling here cannot change without every kept reason being forgotten.</remarks>
    public static class Spellings
    {
        static readonly string[] Reasons =
        {
            "unstartable", "source gone", "block gone", "gate gone", "unoffered", "unfilled", "mistyped", "unawaited", "adrift",
            "slot empty", "circular", "unable", "stuck", "stalled", "conflicted"
        };

        static readonly SequenceProblem[] Problems =
        {
            SequenceProblem.NeverTriggered, SequenceProblem.MissingSource, SequenceProblem.MissingTarget,
            SequenceProblem.MissingGate, SequenceProblem.Unoffered, SequenceProblem.Unfilled, SequenceProblem.Mistyped,
            SequenceProblem.Unawaited, SequenceProblem.Adrift, SequenceProblem.EmptySlot, SequenceProblem.Circular,
            SequenceProblem.Unable, SequenceProblem.Stuck, SequenceProblem.Stalled, SequenceProblem.Conflicted
        };

        static readonly string[] Causes =
        {
            "vent nowhere", "vent leaking", "vent no oxygen", "vent told to fill", "vent told to empty",
            "battery told to discharge", "battery told to auto", "battery off", "battery damaged", "battery no power",
            "piston no power", "piston no head", "piston stopped", "piston reversed extending", "piston reversed retracting",
            "rotor no power", "rotor no head", "rotor locked", "rotor no torque", "rotor stopped", "rotor held",
            "tank stockpile off", "tank no power", "tank no gas",
            "jump drive recharge off",
            "script not run"
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
            Cause.ScriptNotRun
        };

        /// <summary>How <paramref name="problem" /> is kept, or "unknown" where it has no spelling.</summary>
        public static string Of(SequenceProblem problem)
        {
            var at = Array.IndexOf(Problems, problem);
            return at < 0 ? "unknown" : Reasons[at];
        }

        /// <summary>The problem kept as <paramref name="spelled" />, or none.</summary>
        public static SequenceProblem ProblemOf(string spelled)
        {
            var at = Array.IndexOf(Reasons, spelled);
            return at < 0 ? SequenceProblem.None : Problems[at];
        }

        /// <summary>How <paramref name="cause" /> is kept, or empty where it has no spelling.</summary>
        public static string Of(Cause cause)
        {
            var at = Array.IndexOf(Troubles, cause);
            return at < 0 ? "" : Causes[at];
        }

        /// <summary>The cause kept as <paramref name="spelled" />, or none.</summary>
        public static Cause CauseOf(string spelled)
        {
            var at = Array.IndexOf(Causes, spelled);
            return at < 0 ? Cause.None : Troubles[at];
        }

        /// <summary><paramref name="trouble" /> as it is kept, or null for none.</summary>
        public static TroubleMemory Kept(Trouble trouble)
        {
            return trouble.Exists ? new TroubleMemory { Cause = Of(trouble.Cause), About = trouble.About ?? "", Ends = trouble.Ends } : null;
        }

        /// <summary>The trouble <paramref name="kept" /> holds, or none.</summary>
        public static Trouble Trouble(TroubleMemory kept)
        {
            if (kept == null)
                return World.Trouble.None;

            var cause = CauseOf(kept.Cause);
            var about = kept.About.Length == 0 ? null : kept.About;
            return kept.Ends ? World.Trouble.Kept(cause, about) : World.Trouble.Waiting(cause, about);
        }

        /// <summary><paramref name="fault" /> as it is kept, or null for none.</summary>
        public static FaultMemory Kept(SequenceFault fault)
        {
            return fault.Exists
                ? new FaultMemory { Problem = Of(fault.Problem), Step = fault.Step, Which = fault.Which ?? "", Because = Kept(fault.Because) }
                : null;
        }

        /// <summary>The fault <paramref name="kept" /> holds, or none.</summary>
        public static SequenceFault Fault(FaultMemory kept)
        {
            if (kept == null)
                return SequenceFault.None;

            return new SequenceFault(ProblemOf(kept.Problem), kept.Step, Trouble(kept.Because), kept.Which.Length == 0 ? null : kept.Which);
        }
    }
}
