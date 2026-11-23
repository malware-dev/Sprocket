using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sandbox.ModAPI;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A member, value or signal a mod adds is bound by its namespace and its id: written qualified it binds to that mod's alone,
/// and written bare it binds to the one member of that id its blocks offer, whoever adds it, and is an error where they
/// offer more than one.
/// </summary>
public class HostMemberTests
{
    const string Hover = "malforge.hover_repulsors";
    const string Elevator = "malforge.elevator";

    /// <summary>A lamp with members of mods on it, a program on it and a runner ticking it.</summary>
    sealed class Pad
    {
        public readonly Rigged Rig = new();
        public readonly Lamp Lamp;
        public readonly List<string> Log = new();
        public double Height;
        public bool Landed;
        public Automation Program = new();
        public AutomationRunner Runner = null!;

        int _frame;

        /// <summary>A pad offering lift, drop, ride_height and landed in each of the namespaces <paramref name="spaces" />.</summary>
        public Pad(params string[] spaces)
        {
            Lamp = Rig.Light(1, "Pad");
            Lamp.Carrying("Radius", "Radius", ValueKind.Real);
            var kind = Rig.Entity(1).Kind;
            foreach (var space in spaces)
                Offer(kind, space, "lift", "ride_height");
        }

        /// <summary>Adds the action <paramref name="action" />, drop, the value <paramref name="value" /> and the signal landed, in the namespace <paramref name="space" />.</summary>
        public void Offer(EntityKind kind, string space, string action, string value)
        {
            kind.WithAction(action, action, OwnAction.Of<IMyTerminalBlock>(_ => Log.Add($"{space}:{action}")));
            kind.Actions[kind.Actions.Count - 1].InNamespace(space);
            kind.WithAction("drop", "drop", OwnAction.Of<IMyTerminalBlock>(_ => Log.Add($"{space}:drop")));
            kind.Actions[kind.Actions.Count - 1].InNamespace(space);
            kind.WithValue(value, "ride height", OwnValue.Real<IMyTerminalBlock>(_ => Height)
                .Writing<IMyTerminalBlock>((_, to) => Log.Add($"{space}:{value}={to.Real}")));
            kind.Actions[kind.Actions.Count - 1].InNamespace(space);
            kind.WithEvent("landed", Reading.Of<IMyTerminalBlock>(_ => Landed), SignalEdge.Rising, "lands").Looked("a test's signal");
            kind.Events[kind.Events.Count - 1].InNamespace(space);
        }

        /// <summary>
        /// Reads the system <paramref name="body" /> on the pad, with a last sequence that runs each of its sequences so every one
        /// has something to start it, and starts running it.
        /// </summary>
        public void Load(string body)
        {
            var starts = "\n    sequence 9 {\n";
            for (var number = 1; body.Contains($"    sequence {number} {{"); number++)
                starts += $"        run {number}\n";

            // The pad as the world declares it, which is what binds the program to the lamp.
            var declaring = new Automation();
            var pad = declaring.Declare(Rig.World, Rig.Entity(1)).Name;
            var naming = new Act("lift");
            naming.Targets.Add(pad);
            declaring.AddSequence((Trigger)null!).Steps.Add(naming);
            var declared = ProgramWriter.Save(declaring, Rig.Values).Split('\n').Single(line => line.StartsWith("let "));
            body = body.Replace("[ pad ]", $"[ {pad} ]");

            var text = "sprocket 3 alpha 3\n\n"
                       + declared + "\n\n"
                       + "system \"4b6e1f20-8c3a-4d5e-9f71-2a3b4c5d6e7f\" as Malforge.Hover\n{\n"
                       + body
                       + starts
                       + "    }\n"
                       + "}\n";
            Assert.Equal("", ProgramReader.Load(text, Program, Rig.Values));
            Assert.Equal(text, ProgramWriter.Save(Program, Rig.Values));
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(3);
        }

        public Sequence Sequence(int number) => Program.Systems[0].FindSequence(number)!;

        /// <summary>Runs the sequence <paramref name="number" /> through and ticks it on.</summary>
        public void Call(int number)
        {
            var sequence = Sequence(number);
            Runner.Call(sequence, sequence.System.First);
            Tick(5);
        }

        /// <summary>What checking the sequence <paramref name="number" /> finds wrong with it.</summary>
        public SequenceFault Fault(int number)
        {
            var sequence = Sequence(number);
            return SequenceSummary.Checked(Rig.World, Runner.Bindings, sequence, sequence.System.First);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }
    }

    /// <summary>Runs <paramref name="test" /> with the namespaces <paramref name="spaces" /> registered, as a mod registers its own.</summary>
    static void Registered(Action test, params string[] spaces)
    {
        try
        {
            foreach (var space in spaces)
                Assert.True(HostNamespaces.Register(space, space, space, "Malforge"));

            test();
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>A call and a set written in a mod's namespace bind to that mod's members and run.</summary>
    [Fact]
    public void ACallAndASetInAModsNamespaceRun()
    {
        Registered(() =>
        {
            var pad = new Pad(Hover);
            pad.Load("    sequence 1 {\n"
                     + "        call malforge.hover_repulsors.lift on [ pad ]\n"
                     + "        set malforge.hover_repulsors.ride_height on [ pad ] to 4.5\n"
                     + "    }\n");

            Assert.False(pad.Fault(1).Exists, pad.Fault(1).Problem.ToString());
            pad.Call(1);

            Assert.Equal(new[] { "malforge.hover_repulsors:lift", "malforge.hover_repulsors:ride_height=4.5" }, pad.Log);
        }, Hover);
    }

    /// <summary>An if, a when and a whenever written in a mod's namespace read that mod's value and signal, and run what they guard and start.</summary>
    [Fact]
    public void AnIfAndTriggersInAModsNamespaceRun()
    {
        Registered(() =>
        {
            var pad = new Pad(Hover);
            pad.Load("    when any malforge.hover_repulsors.ride_height on [ pad ] > 5 then run 2\n\n"
                     + "    whenever malforge.hover_repulsors.landed on [ pad ] then run 3\n\n"
                     + "    sequence 1 {\n"
                     + "        if all malforge.hover_repulsors.ride_height on [ pad ] > 2 then {\n"
                     + "            call malforge.hover_repulsors.lift on [ pad ]\n"
                     + "        }\n"
                     + "    }\n\n"
                     + "    sequence 2 {\n"
                     + "        call malforge.hover_repulsors.lift on [ pad ]\n"
                     + "    }\n\n"
                     + "    sequence 3 {\n"
                     + "        call malforge.hover_repulsors.drop on [ pad ]\n"
                     + "    }\n");

            for (var number = 1; number <= 3; number++)
                Assert.False(pad.Fault(number).Exists, $"{number}: {pad.Fault(number).Problem} {pad.Fault(number).Unwired}");

            pad.Call(1);
            Assert.Empty(pad.Log);

            pad.Height = 3;
            pad.Call(1);
            Assert.Equal(new[] { "malforge.hover_repulsors:lift" }, pad.Log);

            pad.Log.Clear();
            pad.Height = 6;
            pad.Tick(300);
            Assert.Equal(new[] { "malforge.hover_repulsors:lift" }, pad.Log);

            pad.Log.Clear();
            pad.Landed = true;
            pad.Tick(300);
            Assert.Equal(new[] { "malforge.hover_repulsors:drop" }, pad.Log);
        }, Hover);
    }

    /// <summary>A member written in another namespace than the one its blocks offer it in is not offered there.</summary>
    [Fact]
    public void AnotherNamespaceDoesNotBindAModsMember()
    {
        Registered(() =>
        {
            var pad = new Pad(Hover);
            pad.Load("    sequence 1 {\n"
                     + "        call malforge.elevator.lift on [ pad ]\n"
                     + "    }\n\n"
                     + "    sequence 2 {\n"
                     + "        if all malforge.elevator.ride_height on [ pad ] > 2 then {\n"
                     + "            call malforge.hover_repulsors.lift on [ pad ]\n"
                     + "        }\n"
                     + "    }\n");

            Assert.Equal(SequenceProblem.Unoffered, pad.Fault(1).Problem);
            Assert.Equal(SequenceProblem.Unoffered, pad.Fault(2).Problem);
            pad.Call(1);
            Assert.False(pad.Program.Systems[0].Enabled);
            pad.Call(2);
            Assert.Empty(pad.Log);
        }, Hover, Elevator);
    }

    /// <summary>A bare name binds to the one member of that id its blocks offer, whichever namespace it is in.</summary>
    [Fact]
    public void ABareNameMeetingOneMemberBindsToIt()
    {
        Registered(() =>
        {
            var pad = new Pad(Hover);
            pad.Load("    sequence 1 {\n"
                     + "        call lift on [ pad ]\n"
                     + "        set ride_height on [ pad ] to 2\n"
                     + "    }\n");

            Assert.False(pad.Fault(1).Exists, pad.Fault(1).Problem.ToString());
            pad.Call(1);

            Assert.Equal(new[] { "malforge.hover_repulsors:lift", "malforge.hover_repulsors:ride_height=2" }, pad.Log);
        }, Hover);
    }

    /// <summary>
    /// Two namespaces offering the same id on one block are two members: each qualified call binds to its own, and the name
    /// written bare is an error that names it.
    /// </summary>
    [Fact]
    public void TwoNamespacesOfferingOneIdAreTwoMembers()
    {
        Registered(() =>
        {
            // A system with a step that is an error is switched off whole, so what runs is shown on a pad of its own.
            var qualified = new Pad(Hover, Elevator);
            qualified.Load("    sequence 1 {\n"
                           + "        call malforge.hover_repulsors.lift on [ pad ]\n"
                           + "        call malforge.elevator.lift on [ pad ]\n"
                           + "    }\n");

            Assert.False(qualified.Fault(1).Exists, qualified.Fault(1).Problem.ToString());
            qualified.Call(1);
            Assert.Equal(new[] { "malforge.hover_repulsors:lift", "malforge.elevator:lift" }, qualified.Log);

            var pad = new Pad(Hover, Elevator);
            pad.Load("    when any ride_height on [ pad ] > 2 then run 4\n\n"
                     + "    sequence 1 {\n"
                     + "        call malforge.hover_repulsors.lift on [ pad ]\n"
                     + "    }\n\n"
                     + "    sequence 2 {\n"
                     + "        call lift on [ pad ]\n"
                     + "    }\n\n"
                     + "    sequence 3 {\n"
                     + "        if any ride_height on [ pad ] > 2 then {\n"
                     + "            call malforge.elevator.drop on [ pad ]\n"
                     + "        }\n"
                     + "    }\n\n"
                     + "    sequence 4 {\n"
                     + "        call malforge.elevator.drop on [ pad ]\n"
                     + "    }\n");

            var bare = pad.Fault(2);
            Assert.Equal(SequenceProblem.Ambiguous, bare.Problem);
            Assert.Equal(0, bare.Step);
            Assert.Equal("lift", bare.Which);
            Assert.Equal("This step's lift could mean more than one thing. Select which one in the step.",
                SequenceText.Problem(new Texts(), bare, pad.Sequence(2)));

            Assert.Equal(SequenceProblem.Ambiguous, pad.Fault(3).Problem);

            var watched = pad.Fault(4);
            Assert.Equal(SequenceProblem.Ambiguous, watched.Problem);
            Assert.Equal(SequenceFault.Trigger, watched.Step);

            pad.Call(2);
            Assert.Empty(pad.Log);
        }, Hover, Elevator);
    }

    /// <summary>
    /// A bare name the game's member and a mod's member share is an error, and each written qualified binds: the game's as
    /// <c>game.Radius</c>, written back so.
    /// </summary>
    [Fact]
    public void ABareNameTheGameAndAModShareIsAnError()
    {
        Registered(() =>
        {
            var pad = new Pad();
            pad.Offer(pad.Rig.Entity(1).Kind, Hover, "lift", "Radius");
            pad.Load("    sequence 1 {\n"
                     + "        set Radius on [ pad ] to 4\n"
                     + "    }\n");

            var bare = pad.Fault(1);
            Assert.Equal(SequenceProblem.Ambiguous, bare.Problem);
            Assert.Equal("Radius", bare.Which);

            var qualified = new Pad();
            qualified.Offer(qualified.Rig.Entity(1).Kind, Hover, "lift", "Radius");
            qualified.Load("    sequence 1 {\n"
                           + "        set malforge.hover_repulsors.Radius on [ pad ] to 4\n"
                           + "        set game.Radius on [ pad ] to 2.5\n"
                           + "    }\n");

            var game = (Act)qualified.Sequence(1).Steps[1];
            Assert.Equal(new MemberKey(BlockKinds.Game, "Radius"), game.Key);
            Assert.False(qualified.Fault(1).Exists, qualified.Fault(1).Problem.ToString());
            qualified.Call(1);
            Assert.Equal(new[] { "malforge.hover_repulsors:Radius=4" }, qualified.Log);
            Assert.Equal(2.5, qualified.Lamp.Read("Radius").Real, 3);
        }, Hover);
    }

    /// <summary>Beside a mod's members, Sprocket's own bind qualified and the game's bind bare, as they always have.</summary>
    [Fact]
    public void SprocketsOwnAndTheGamesBindAsBefore()
    {
        Registered(() =>
        {
            var pad = new Pad(Hover);
            pad.Load("    sequence 1 {\n"
                     + "        set sprocket.power on [ pad ] to off\n"
                     + "        set Radius on [ pad ] to 3.5\n"
                     + "    }\n");

            Assert.False(pad.Fault(1).Exists, pad.Fault(1).Problem.ToString());
            pad.Call(1);

            Assert.Contains("Pad:off", pad.Rig.Rig.Log);
            Assert.Equal(3.5, pad.Lamp.Read("Radius").Real, 3);
        }, Hover);
    }

    /// <summary>A key compares by namespace and id alike, and a bare one names a member of that id in any namespace.</summary>
    [Fact]
    public void AKeyComparesItsNamespaceAndItsId()
    {
        var hover = new MemberKey(Hover, "lift");
        Assert.Equal(hover, new MemberKey("malforge." + "hover_repulsors", "li" + "ft"));
        Assert.NotEqual(hover, new MemberKey(Elevator, "lift"));
        Assert.NotEqual(hover, MemberKey.Bare("lift"));
        Assert.Equal(hover.GetHashCode(), new MemberKey(Hover, "lift").GetHashCode());
        Assert.True(MemberKey.Bare("lift").Names(hover));
        Assert.False(new MemberKey(Elevator, "lift").Names(hover));
        Assert.Equal("malforge.hover_repulsors.lift", hover.ToString());
        Assert.True(default(MemberKey).IsEmpty);
        Assert.Equal(MemberKey.Bare(""), default);
    }
}
