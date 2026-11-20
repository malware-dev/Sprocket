using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What went wrong is stored by the number set on each value, and read back as itself.</summary>
public class StoredReasonsTests
{
    static readonly ProtoBinary Binary = new();

    /// <summary>Every problem survives being packed and read back, so one added past the last known number fails here.</summary>
    [Fact]
    public void EveryProblemIsReadBackAsItself()
    {
        foreach (SequenceProblem problem in Enum.GetValues(typeof(SequenceProblem)))
        {
            if (problem == SequenceProblem.None)
                continue;

            var stored = Binary.Unpack<FaultMemory>(Binary.Pack(StoredReasons.Stored(new SequenceFault(problem, 2))));
            Assert.Equal(problem, StoredReasons.Fault(stored).Problem);
        }
    }

    /// <summary>Every block trouble survives being packed and read back, so one added past the last known number fails here.</summary>
    [Fact]
    public void EveryCauseIsReadBackAsItself()
    {
        foreach (Cause cause in Enum.GetValues(typeof(Cause)))
        {
            if (cause == Cause.None)
                continue;

            var stored = Binary.Unpack<TroubleMemory>(Binary.Pack(StoredReasons.Stored(BlockTrouble.Waiting(cause, "hydrogen"))));
            Assert.Equal(cause, StoredReasons.Trouble(stored).Cause);
        }
    }

    /// <summary>A memory an earlier build wrote, with its words, reads as the problem and cause they name.</summary>
    [Fact]
    public void TheWordsOfAnEarlierBuildAreRead()
    {
        var stored = new FaultMemory { Spelled = "block gone", Step = 3, Because = new TroubleMemory { Spelled = "tank no gas", About = "hydrogen" } };

        var fault = StoredReasons.Fault(Binary.Unpack<FaultMemory>(Binary.Pack(stored)));
        Assert.Equal(SequenceProblem.MissingTarget, fault.Problem);
        Assert.Equal(Cause.TankNoGas, fault.Because.Cause);
    }

    /// <summary>A number a newer build stored, past every value this one has, reads as none.</summary>
    [Fact]
    public void ANumberNotKnownReadsAsNone()
    {
        Assert.Equal(SequenceProblem.None, StoredReasons.Fault(new FaultMemory { Problem = 900 }).Problem);
        Assert.Equal(Cause.None, StoredReasons.Trouble(new TroubleMemory { Cause = 900 }).Cause);
    }

    /// <summary>Each problem has the number saves hold it by; a value is only ever added after the last.</summary>
    [Fact]
    public void TheProblemsKeepTheirNumbers()
    {
        string[] numbered =
        {
            "None", "MissingSource", "MissingTarget", "MissingGate", "Unoffered", "Unfilled", "Mistyped", "NeverTriggered",
            "NothingStarts", "EmptySlot", "Adrift", "Stuck", "Conflicted", "Circular", "Unable", "Stalled", "NothingToAwait",
            "MissingFacet", "ScreenOnBlockSlot", "NothingToAverage"
        };

        for (var i = 0; i < numbered.Length; i++)
            Assert.Equal(numbered[i], ((SequenceProblem)i).ToString());
    }

    /// <summary>Each block trouble has the number saves hold it by; a value is only ever added after the last.</summary>
    [Fact]
    public void TheCausesKeepTheirNumbers()
    {
        string[] numbered =
        {
            "None", "VentNowhere", "VentNotAirtight", "VentNoOxygen", "VentToldToFill", "VentToldToEmpty",
            "BatteryToldToDischarge", "BatteryToldToAuto", "BatteryOff", "BatteryDamaged", "BatteryNoPower",
            "PistonNoPower", "PistonNoHead", "PistonStopped", "PistonReversedExtending", "PistonReversedRetracting",
            "RotorNoPower", "RotorNoHead", "RotorLocked", "RotorNoTorque", "RotorStopped", "RotorHeld",
            "TankStockpileOff", "TankNoPower", "TankNoGas", "JumpDriveRechargeOff", "ScriptNotRun",
            "DoorToldToOpen", "DoorToldToClose"
        };

        for (var i = 0; i < numbered.Length; i++)
            Assert.Equal(numbered[i], ((Cause)i).ToString());
    }
}
