using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Fakes;
using Xunit;
using Door = Sprocket.Sim.Machine.Door;

namespace Sprocket.Tests;

/// <summary>What watching a condition costs as the blocks it reads grow in number: how often each block is read, and what a look allocates.</summary>
public class ExpressionCostTests
{
    const int Ticks = 1000;

    readonly ITestOutputHelper _said;

    /// <summary>The tests, saying what they measured to <paramref name="said" />.</summary>
    public ExpressionCostTests(ITestOutputHelper said)
    {
        _said = said;
    }

    sealed class Plant
    {
        public readonly Rigged Ship = new();
        public readonly List<Door> Doors = new();
        public readonly Automation Program = new();
        public readonly AutomationRunner Runner;

        int _frame;

        // A when on whether any of the doors is open.
        public Plant(int doors)
        {
            var system = Program.AddSequence((Trigger)null!).System;
            var reading = new BlockReading(new MemberKey("sprocket", "status"), Combination.Any, new List<Literal>(), new List<string>(),
                Relation.Is, new Operand(Literal.Name("open", ChoiceTypes.DoorStatus)));
            for (var i = 0; i < doors; i++)
            {
                // Numbered from 1001, clear of the ids the test ship gives blocks of its own.
                var door = Ship.Door(1001 + i, "Door " + (i + 1));
                Doors.Add(door);
                reading.Targets.Add(Program.Declare(Ship.World, Ship.Entity(door.Id)).Name);
            }

            Program.AddSequence(system, new ExpressionTrigger(reading)).Steps.Add(new Wait(1));
            Program.Edited();
            Runner = new AutomationRunner(Program, Ship.World);
        }

        // Bytes the runner allocated of its own over its ticks, apart from the simulator's stand-ins for the game's blocks.
        public long Allocated { get; private set; }

        public void Tick(int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                Ship.Tick();
                var tick = ++_frame;
                Allocated += StandInLedger.Own(() => Runner.Tick(tick));
            }
        }

        public int Reads
        {
            get
            {
                var reads = 0;
                foreach (var door in Doors)
                    reads += door.StatusReads;
                return reads;
            }
        }
    }

    /// <summary>
    /// A when on whether any of its doors is open reads shut doors only on its backstop look every hundred ticks, since a
    /// shut door announces any change, and a look allocates nothing.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void ShutDoorsAreReadOnlyOnTheBackstop(int doors)
    {
        var p = new Plant(doors);
        p.Tick(300);

        var reads = p.Reads;
        var allocated = p.Allocated;
        p.Tick(Ticks);
        reads = p.Reads - reads;
        allocated = p.Allocated - allocated;

        _said.WriteLine($"{doors} shut doors: {reads} reads over {Ticks} ticks, {reads / doors} a door; {allocated} bytes of its own");
        // A backstop look reads each door's status for the condition and again for whether it is still shut.
        Assert.Equal(2 * Ticks / Rota.Hundred, reads / doors);
        Assert.Equal(0, allocated);
    }

    /// <summary>While a door stands open, which could close unannounced, the when looks on its cadence of ten ticks, and a look allocates nothing.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void AnOpenDoorIsLookedAtOnTheCadence(int doors)
    {
        var p = new Plant(doors);
        p.Doors[0].ForceOpen();
        p.Tick(300);

        var reads = p.Doors[0].StatusReads;
        var allocated = p.Allocated;
        p.Tick(Ticks);
        reads = p.Doors[0].StatusReads - reads;
        allocated = p.Allocated - allocated;

        _said.WriteLine($"{doors} doors, the first open: it read {reads} times over {Ticks} ticks; {allocated} bytes of its own");
        Assert.True(reads >= Ticks / Rota.Ten, $"the open door was read {reads} times");
        Assert.Equal(0, allocated);
    }
}
