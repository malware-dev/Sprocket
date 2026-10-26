using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Barge = Sprocket.Sim.Machine.Simulation;

namespace Sprocket.Sim.Simulation;

/// <summary>A construct's automation in each state the status panel draws, for pictures of it.</summary>
/// <remarks>
/// Every scene is the same mining barge with five logic systems, one of them a blueprint with a long name and two installations.
/// In the troubled one, the airlock blueprint lost a door both
/// its installations close, the hangar has a sequence nothing starts, and the cargo lift was switched off when it could not
/// work. In the switched-off one, nothing is wrong and a player switched off the hangar and the airlock's starboard
/// installation. In the unheard one, nothing has been heard yet. In the one with no automation, the construct has none, and
/// so no name either.
/// </remarks>
public static class StatusScenes
{
    /// <summary>What the construct is called.</summary>
    public const string Construct = "Mining Barge";

    /// <summary>The scenes, by the name each is shot under.</summary>
    public static readonly IReadOnlyList<string> Names = ["trouble", "all-normal", "switched-off", "unheard", "no-automation"];

    /// <summary>The scene called <paramref name="name" />, and the texts it is worded in.</summary>
    public static ConstructStatus Make(string name, out Texts texts)
    {
        var rig = new Rig();
        rig.Door(2, "Port Inner Door");
        var outer = rig.Door(3, "Port Outer Door");
        rig.Door(4, "Hangar Door");
        rig.Door(5, "Lift Gate");
        rig.Door(6, "Lamp Room Door");
        var sim = Barge.On(rig);
        sim.Tick();
        texts = sim.Texts;

        var status = new ConstructStatus();
        if (name == "unheard")
        {
            status.Unheard(Construct);
            return status;
        }

        if (name == "no-automation")
        {
            status.Begin(sim.Now);
            status.End("");
            return status;
        }

        var program = sim.Unit.Program;
        var airlock = System(sim, "Forward Hangar Atmospheric Airlock", 2, 3);
        airlock.IsBlueprint = true;
        airlock.AddInstance().Name = "Port Personnel Lock";
        airlock.AddInstance().Name = "Starboard";
        var hangar = System(sim, "Hangar", 4, 4);
        var lift = System(sim, "Cargo Lift", 5, 5);
        System(sim, "Lights", 6, 6);
        System(sim, "Refinery Feed", 6, 6);

        if (name == "trouble")
        {
            var spare = program.AddSequence(hangar);
            spare.Name = "Seal up";
            var shut = new Act("close");
            shut.Targets.Add(Ref(sim, 4));
            spare.Steps.Add(shut);

            lift.Halt = new Halt(SequenceProblem.MissingTarget, 1, 0, BlockTrouble.None, "Lift Gate");
            lift.Enabled = false;
        }
        else if (name == "switched-off")
        {
            hangar.Enabled = false;
            airlock.Instances[1].Enabled = false;
        }
        else if (name != "all-normal")
            throw new ArgumentException("there is no scene called " + name);

        for (var i = 0; i < 3; i++)
            sim.Tick();

        if (name == "trouble")
        {
            rig.Grind(outer);
            for (var i = 0; i < 3; i++)
                sim.Tick();
            sim.Runner.Look();
        }

        status.Begin(sim.Now);
        status.Include(program, sim.Runner);
        status.End(Construct);
        return status;
    }

    /// <summary>A switched-on logic system with one sequence that door <paramref name="watches" /> opening starts, closing door <paramref name="acts" />.</summary>
    static AutomationSystem System(Barge sim, string name, long watches, long acts)
    {
        var program = sim.Unit.Program;
        var system = program.AddSystem();
        system.Name = name;
        system.Enabled = true;

        var opened = SignalTrigger.Written("ext", "finishes-opening");
        opened.Sources.Add(Ref(sim, watches));

        var sequence = program.AddSequence(system, opened);
        var act = new Act("close");
        act.Targets.Add(Ref(sim, acts));
        sequence.Steps.Add(act);
        return system;
    }

    static string Ref(Barge sim, long id)
    {
        return sim.Unit.Program.Declare(sim.World, sim.World.FindEntity(id)!).Name;
    }
}
