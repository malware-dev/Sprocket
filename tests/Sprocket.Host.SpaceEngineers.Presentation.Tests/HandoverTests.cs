using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A client asks the server for a Sprocket to edit, and is lent it with its program, or told who has it;
/// what it changes reaches the server, which builds the Sprocket again from it.
/// </summary>
/// <remarks>
/// One editor at a time, the host's own editing included, which is what lets a client apply its edits
/// the moment it makes them. The copy a client edits runs nothing: the server runs the program, and a
/// client running it too would do everything twice.
/// </remarks>
public class HandoverTests : IDisposable
{
    readonly Simulation _sim;
    readonly Wire _wire = new Wire();
    readonly Peer _server;
    readonly long _block;
    readonly Handover _serving;

    public HandoverTests()
    {
        _sim = Simulation.Fresh();
        _block = _sim.Machine.Sprocket.Block.EntityId;

        _wire.Install();
        _server = _wire.Server();
        _serving = Handover(_server, id => id == _block ? _sim.Unit : null, _atServer);
    }

    public void Dispose()
    {
        _wire.Uninstall();
    }

    sealed class Heard
    {
        public readonly List<string> Given = new List<string>();
        public readonly List<string> Refused = new List<string>();
        public readonly List<string> Foreign = new List<string>();
        public readonly List<string> Changed = new List<string>();
        public readonly List<string> Stored = new List<string>();
        public string Program = "";
        public string State = "";
        public string Switches = "";
    }

    readonly Heard _atServer = new Heard();

    const long Anna = 144115188075855873;
    const long Bert = 144115188075855874;

    Handover Handover(Peer peer, Func<long, SprocketUnit?> find, Heard heard)
    {
        Handover made = null!;
        _wire.As(peer, () =>
        {
            made = new Handover(find,
                (block, program, state) =>
                {
                    heard.Given.Add(block.ToString());
                    heard.Program = program;
                    heard.State = state;
                },
                (_, holder, program, state) =>
                {
                    heard.Refused.Add(holder);
                    heard.Program = program;
                    heard.State = state;
                },
                block => heard.Foreign.Add(block.ToString()),
                (_, program, switches) =>
                {
                    heard.Changed.Add(program);
                    heard.Switches = switches;
                },
                (_, program, _) => heard.Stored.Add(program));

            made.Listen(Network.Handover.Carry(NetworkChannel.Create("Sprocket.HandoverTests")).Build());
        });

        return made;
    }

    Handover Client(Peer peer, Heard heard)
    {
        return Handover(peer, _ => null, heard);
    }

    /// <summary>A system whose one trigger, a repeat, closes the outer door, so running it does something to see.</summary>
    void Repeating()
    {
        _sim.Editor.NewSystem("Airlock");
        _sim.Editor.PressStart("every");
        _sim.Editor.DeveloperMode = true;
        _sim.Tick();
        _sim.Editor.PressChoice("A task on blocks…");
        _sim.Editor.PressBlock("Airlock Outer Door");
        _sim.Editor.PressChoice("Close");
        _sim.Editor.PressPrimary();
    }

    [Fact]
    public void AClientIsLentTheProgramTheServerHolds()
    {
        Repeating();
        var anna = _wire.Client("Anna");
        var heard = new Heard();
        var annas = Client(anna, heard);

        _wire.As(anna, () => annas.Ask(_block, "Anna"));
        _wire.Settle();

        string program;
        string state;
        _sim.Unit.Written(out program, out state);

        Assert.Equal(new[] { _block.ToString() }, heard.Given);
        Assert.Equal(program, heard.Program);
        Assert.Equal(state, heard.State);
        Assert.Contains("every", program);
    }

    [Fact]
    public void ASecondClientHearsWhoHasIt()
    {
        var anna = _wire.Client("Anna");
        var bert = _wire.Client("Bert");
        var annas = Client(anna, new Heard());
        var heard = new Heard();
        var berts = Client(bert, heard);

        _wire.As(anna, () => annas.Ask(_block, "Anna"));
        _wire.Settle();
        _wire.As(bert, () => berts.Ask(_block, "Bert"));
        _wire.Settle();

        Assert.Empty(heard.Given);
        Assert.Equal(new[] { "Anna" }, heard.Refused);

        // With the program, to look at while Anna has it.
        Assert.False(string.IsNullOrEmpty(heard.Program), "the refusal came without the program");
    }

    /// <summary>A holder who crashed or lost the connection never hands back, so the server takes it.</summary>
    [Fact]
    public void AHolderWhoHasGoneLosesIt()
    {
        Lent();
        _wire.As(_server, () => _serving.Reclaim(_ => false));

        var bert = _wire.Client("Bert");
        var heard = new Heard();
        var berts = Client(bert, heard);
        _wire.As(bert, () => berts.Ask(_block, "Bert"));
        _wire.Settle();

        Assert.Single(heard.Given);
    }

    [Fact]
    public void AHolderStillHereKeepsIt()
    {
        Lent();
        _wire.As(_server, () => _serving.Reclaim(_ => true));

        var bert = _wire.Client("Bert");
        var heard = new Heard();
        var berts = Client(bert, heard);
        _wire.As(bert, () => berts.Ask(_block, "Bert"));
        _wire.Settle();

        Assert.Equal(new[] { "Anna" }, heard.Refused);
    }

    /// <summary>A copy lent to look at while somebody else edits it moves around and changes nothing.</summary>
    [Fact]
    public void ACopyToLookAtChangesNothing()
    {
        Waiting();
        var (anna, _, copy) = Lent();

        _wire.As(anna, () =>
        {
            copy.Editor.Holder = "Bert";
            copy.Editor.Tick(1);
            copy.Editor.RootHeadings()[0].Opens!.Execute();
            copy.Editor.Tick(1);

            Assert.NotEmpty(copy.Editor.Triggers());
            Assert.All(copy.Editor.Triggers(), trigger => Assert.Null(trigger.Does[0].Opens));

            var row = copy.Editor.Rows()[0];
            Assert.Null(row.Flips);

            row.Opens!.Execute();
            copy.Editor.Tick(1);

            var sentence = copy.Editor.Sentence();
            Assert.False(sentence.Editable);
            Assert.Equal("Read only while Bert edits it.", sentence.LockedText);
            Assert.Null(copy.Editor.Screen.Flip);
            Assert.Null(copy.Editor.Screen.Runs);
            Assert.Null(copy.Editor.Screen.Deletes);
        });
    }

    /// <summary>
    /// Every client is told what the server wrote into a block, because a grid copied on a client is
    /// copied from that client's own copy of the block.
    /// </summary>
    [Fact]
    public void EveryClientIsToldWhatTheServerWrote()
    {
        var anna = _wire.Client("Anna");
        var bert = _wire.Client("Bert");
        var hers = new Heard();
        var his = new Heard();
        Client(anna, hers);
        Client(bert, his);

        _wire.As(_server, () => _serving.Tell(_block, "the program", "the switches"));
        _wire.Settle();

        Assert.Equal(new[] { "the program" }, hers.Stored);
        Assert.Equal(new[] { "the program" }, his.Stored);
        Assert.Empty(_atServer.Stored);
    }

    /// <summary>How somebody works in the editor outlives a rebuild.</summary>
    [Fact]
    public void HabitsCarryOnThroughARebuild()
    {
        _sim.Unit.Editor.ShowsTerminal = true;

        string program;
        _sim.Unit.Written(out program, out _);

        SprocketUnit rebuilt = null!;
        _wire.As(_server, () => rebuilt = _sim.Unit.Rebuilt(program, "", null, new Rota()));

        Assert.True(rebuilt.Editor.ShowsTerminal);
    }

    /// <summary>What a client is told goes into its copy of the block, where a copied grid picks it up.</summary>
    [Fact]
    public void WhatAClientIsToldIsInItsBlock()
    {
        var block = _sim.Machine.Sprocket.Block;
        SprocketUnit.Store(block, "the program", "the switches");

        string stored;
        Assert.True(block.Storage.TryGetValue(SprocketUnit.ProgramKey, out stored));
        Assert.Equal("the program", stored);
    }

    [Fact]
    public void AClientIsNotToldTheSameTwice()
    {
        var anna = _wire.Client("Anna");
        var hers = new Heard();
        Client(anna, hers);

        _wire.As(_server, () =>
        {
            _serving.Tell(_block, "the program", "the switches");
            _serving.Tell(_block, "the program", "the switches");
            _serving.Tell(_block, "the program", "other switches");
        });
        _wire.Settle();

        Assert.Equal(2, hers.Stored.Count);
    }

    [Fact]
    public void HandingItBackLetsTheNextHaveIt()
    {
        var anna = _wire.Client("Anna");
        var bert = _wire.Client("Bert");
        var annas = Client(anna, new Heard());
        var heard = new Heard();
        var berts = Client(bert, heard);

        _wire.As(anna, () => annas.Ask(_block, "Anna"));
        _wire.Settle();
        _wire.As(anna, () => annas.Hand(_block));
        _wire.Settle();
        _wire.As(bert, () => berts.Ask(_block, "Bert"));
        _wire.Settle();

        Assert.Single(heard.Given);
        Assert.Empty(heard.Refused);
    }

    /// <summary>A host editing its own world holds the Sprocket as a client would.</summary>
    [Fact]
    public void AHostEditingHoldsItToo()
    {
        var anna = _wire.Client("Anna");
        var heard = new Heard();
        var annas = Client(anna, heard);

        Assert.True(_serving.Lend(_block, _server.Id, "Host", out _));

        _wire.As(anna, () => annas.Ask(_block, "Anna"));
        _wire.Settle();

        Assert.Equal(new[] { "Host" }, heard.Refused);
    }

    /// <summary>Somebody who owns none of the construct is told it is not theirs, and is lent nothing.</summary>
    [Fact]
    public void SomebodyOwningNoneOfItIsRefused()
    {
        _sim.Machine.Own(Anna);
        _sim.Tick();

        var bert = _wire.Client("Bert");
        var heard = new Heard();
        var his = Client(bert, heard);
        MyAPIGateway.Players = Pretend.To<IMyPlayerCollection>()
            .Does("TryGetIdentityId", arguments => (ulong)arguments[0] == bert.Id ? Bert : Anna)
            .Done();
        try
        {
            _wire.As(bert, () => his.Ask(_block, "Bert"));
            _wire.Settle();
        }
        finally
        {
            MyAPIGateway.Players = null;
        }

        Assert.Equal(new[] { _block.ToString() }, heard.Foreign);
        Assert.Empty(heard.Given);
    }

    [Fact]
    public void ABlockWithNoSprocketCannotBeHad()
    {
        var anna = _wire.Client("Anna");
        var heard = new Heard();
        var annas = Client(anna, heard);

        _wire.As(anna, () => annas.Ask(_block + 1, "Anna"));
        _wire.Settle();

        Assert.Equal(new[] { "" }, heard.Refused);
    }

    /// <summary>Anna holding the Sprocket, with the copy she was lent.</summary>
    (Peer Anna, Handover Hers, SprocketUnit Copy) Lent()
    {
        var anna = _wire.Client("Anna");
        var heard = new Heard();
        var hers = Client(anna, heard);

        _wire.As(anna, () => hers.Ask(_block, "Anna"));
        _wire.Settle();

        SprocketUnit copy = null!;
        _wire.As(anna, () =>
        {
            copy = SprocketUnit.Copy(_sim.Machine.Sprocket.Block, null, _sim.Texts, heard.Program, heard.State);
            hers.Kept(_block, copy);
        });

        return (anna, hers, copy);
    }

    [Fact]
    public void AClientsChangeReachesTheServer()
    {
        var (anna, hers, copy) = Lent();

        _wire.As(anna, () =>
        {
            copy.Editor.NewSystem("Hangar");
            hers.Keep(_block, copy);
        });
        _wire.Settle();

        Assert.Single(_atServer.Changed);
        Assert.Contains("Hangar", _atServer.Changed[0]);

        SprocketUnit rebuilt = null!;
        _wire.As(_server, () =>
            rebuilt = _sim.Unit.Rebuilt(_atServer.Changed[0], _atServer.Switches, null, new Rota()));

        Assert.Equal("Hangar", Assert.Single(rebuilt.Editor.Program.Systems).Name);
    }

    /// <summary>The server lends, takes a change and rebuilds without ever making an editor: only the client edits.</summary>
    [Fact]
    public void TheServerMakesNoEditor()
    {
        var (anna, hers, copy) = Lent();

        _wire.As(anna, () =>
        {
            copy.Editor.NewSystem("Hangar");
            hers.Keep(_block, copy);
        });
        _wire.Settle();

        SprocketUnit rebuilt = null!;
        _wire.As(_server, () =>
            rebuilt = _sim.Unit.Rebuilt(_atServer.Changed[0], _atServer.Switches, null, new Rota()));

        Assert.False(_sim.Unit.HasEditor);
        Assert.False(rebuilt.HasEditor);
        Assert.Equal("Hangar", Assert.Single(rebuilt.Program.Systems).Name);
    }

    /// <summary>Both ends log what a change does, so a run in the game can be followed from its logs alone.</summary>
    [Fact]
    public void AChangeSaysWhatChangedAtBothEnds()
    {
        var (anna, hers, copy) = Lent();
        var from = Told.Lines.Count;

        _wire.As(anna, () =>
        {
            copy.Editor.NewSystem("Hangar");
            hers.Keep(_block, copy);
        });
        _wire.Settle();

        var said = Told.Lines.Skip(from).ToList();
        var sent = Assert.Single(said, l => l.StartsWith("sending the server a change"));
        var taken = Assert.Single(said, l => l.Contains("sent a change to block"));

        foreach (var line in new[] { sent, taken })
        {
            Assert.Contains("first difference at line", line);
            Assert.Contains("Hangar", line);

            // A new system arrives switched off, which the switches say.
            Assert.Contains("; switches first difference", line);
        }
    }

    [Fact]
    public void NothingIsSentWhereNothingChanged()
    {
        var (anna, hers, copy) = Lent();

        _wire.As(anna, () => hers.Keep(_block, copy));
        _wire.Settle();

        Assert.Empty(_atServer.Changed);
    }

    /// <summary>A picker half filled in has changed nothing yet, so there is nothing to send until it is done.</summary>
    [Fact]
    public void APickerInProgressSendsNothing()
    {
        var (anna, hers, copy) = Lent();

        _wire.As(anna, () =>
        {
            copy.Editor.NewSystem("Hangar");
            hers.Keep(_block, copy);
        });
        _wire.Settle();

        _wire.As(anna, () =>
        {
            copy.Editor.PressStart("when");
            copy.Editor.PressBlock("Airlock Button");
            hers.Keep(_block, copy);
        });
        _wire.Settle();

        Assert.Single(_atServer.Changed);
    }

    [Fact]
    public void AChangeFromSomebodyWithoutItIsNotTaken()
    {
        Lent();

        var bert = _wire.Client("Bert");
        var his = Client(bert, new Heard());
        _wire.As(bert, () =>
        {
            var copy = SprocketUnit.Copy(_sim.Machine.Sprocket.Block, null, _sim.Texts, "", "");
            copy.Editor.NewSystem("Mine");
            his.Keep(_block, copy);
        });
        _wire.Settle();

        Assert.Empty(_atServer.Changed);
    }

    /// <summary>
    /// A sequence part way through carries on through the server building the Sprocket again, the way
    /// it carries on through a save and a reload.
    /// </summary>
    [Fact]
    public void WhatIsRunningCarriesOnThroughARebuild()
    {
        _sim.Editor.NewButtonSystem("Airlock");

        var sequence = _sim.Editor.Program.Sequences[0];
        sequence.Steps.Add(new Wait(600));
        sequence.System.Enabled = true;

        // Ticked first, so the runner has caught up with a program it was handed a moment ago.
        _sim.Tick();
        _sim.Tick();
        _sim.Runner.Call(sequence);
        for (var i = 0; i < 10; i++)
            _sim.Tick();

        Assert.True(_sim.Runner.Get(sequence, sequence.System.First).IsRunning);

        string program;
        _sim.Unit.Written(out program, out _);
        var switches = Storage.StateText.Save(_sim.Editor.Program, null);

        SprocketUnit rebuilt = null!;
        _wire.As(_server, () =>
        {
            rebuilt = _sim.Unit.Rebuilt(program, switches, null, new Rota());

            // Its first tick, which is when a runner catches up with the program and takes back
            // what it was running - the same as after a reload.
            rebuilt.Tick(1);
        });

        var again = rebuilt.Editor.Program.Sequences[0];
        var run = rebuilt.Runner.Get(again, again.System.First);
        Assert.True(run.IsRunning, "the run did not carry across");
        Assert.True(run.WaitTicks > 0, "the wait did not carry across");
    }

    /// <summary>A sequence on the server that waits, so it is still going when it is looked at.</summary>
    Sequence Waiting()
    {
        _sim.Editor.NewButtonSystem("Airlock");

        var sequence = _sim.Editor.Program.Sequences[0];
        sequence.Steps.Add(new Wait(600));
        _sim.Tick();
        _sim.Tick();
        return sequence;
    }

    /// <summary>Running a sequence by hand on a copy runs it on the server, and stopping it stops it there.</summary>
    [Fact]
    public void AClientRunsAndStopsASequenceOnTheServer()
    {
        var sequence = Waiting();
        var (anna, hers, copy) = Lent();
        var theirs = copy.Editor.Program.Sequences[0];

        _wire.As(anna, () => hers.Run(_block, theirs, theirs.System.First, false));
        _wire.Settle();
        _sim.Tick();

        var run = _sim.Runner.Get(sequence, sequence.System.First);
        Assert.True(run.IsRunning, "the sequence did not run on the server");

        _wire.As(anna, () => hers.Run(_block, theirs, theirs.System.First, true));
        _wire.Settle();
        _sim.Tick();

        Assert.False(run.IsRunning, "the sequence did not stop on the server");
    }

    [Fact]
    public void AnOrderFromSomebodyWithoutItIsNotTaken()
    {
        var sequence = Waiting();
        var (_, _, copy) = Lent();
        var theirs = copy.Editor.Program.Sequences[0];

        var bert = _wire.Client("Bert");
        var his = Client(bert, new Heard());
        _wire.As(bert, () => his.Run(_block, theirs, theirs.System.First, false));
        _wire.Settle();
        _sim.Tick();

        var run = _sim.Runner.Get(sequence, sequence.System.First);
        Assert.True(run == null || !run.IsRunning, "somebody without the Sprocket ran it");
    }

    /// <summary>
    /// A button a client has just given a trigger is wired by the server as the Sprocket is built
    /// again, rather than read as one somebody cleared.
    /// </summary>
    [Fact]
    public void AButtonTheClientAddsIsWired()
    {
        Repeating();
        for (var i = 0; i < 3; i++)
            _sim.Tick();

        var (anna, hers, copy) = Lent();
        _wire.As(anna, () =>
        {
            copy.Editor.Tick(1);
            copy.Editor.RootHeadings()[0].Opens!.Execute();
            copy.Editor.Tick(1);
            copy.Editor.PressStart("when");
            copy.Editor.PressBlock("Airlock Button");
            copy.Editor.PressChoice("button 1 is pressed");
            copy.Editor.PressPrimary();
            hers.Keep(_block, copy);
        });
        _wire.Settle();

        SprocketUnit rebuilt = null!;
        _wire.As(_server, () =>
        {
            rebuilt = _sim.Unit.Rebuilt(_atServer.Changed[0], _atServer.Switches, null, new Rota());
            for (var frame = 1; frame <= 5; frame++)
                rebuilt.Tick(frame);
        });

        Assert.Equal(2, rebuilt.Editor.Program.Systems[0].Triggers.Count);
        Assert.Contains(_sim.Panels(), panel => panel.Toolbar.Holds(0));
    }

    /// <summary>A button a client takes over for a new trigger is written over what its slot held, as the client was asked.</summary>
    [Fact]
    public void AButtonAClientTakesOverIsWritten()
    {
        Repeating();
        for (var i = 0; i < 3; i++)
            _sim.Tick();

        var panel = _sim.Panels().First(each => each.Name == "Airlock Button");
        var door = _sim.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door");
        panel.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(door.Id, "Open_On"), new List<TerminalActionParameter>()).Apply();
        _sim.Tick();

        var (anna, hers, copy) = Lent();
        string question = null!;
        _wire.As(anna, () =>
        {
            copy.Editor.Wire = picked => hers.Wire(_block, picked);
            copy.Editor.Unwire = () => hers.Unwire(_block);
            copy.Editor.Tick(1);
            copy.Editor.RootHeadings()[0].Opens!.Execute();
            copy.Editor.Tick(1);
            copy.Editor.PressStart("when");
            copy.Editor.PressBlock("Airlock Button");
            copy.Editor.PressChoice("button 1 is pressed");
            copy.Editor.PressPrimary();
            copy.Editor.Tick(1);
            question = copy.Editor.Question();
            copy.Editor.PressChoice(copy.Editor.Texts.ButtonTakeOver);
            copy.Editor.Tick(1);
        });
        _wire.Settle();
        _wire.As(anna, () => hers.Keep(_block, copy));
        _wire.Settle();

        Assert.Equal("Button 1 of Airlock Button already does something else.", question);

        SprocketUnit rebuilt = null!;
        _wire.As(_server, () =>
        {
            rebuilt = _sim.Unit.Rebuilt(_atServer.Changed[0], _atServer.Switches, null, new Rota());
            for (var frame = 1; frame <= 5; frame++)
                rebuilt.Tick(frame);
        });

        Assert.Equal(2, rebuilt.Editor.Program.Systems[0].Triggers.Count);
        Assert.True(panel.Toolbar.TryGetItem(0, out var held));
        Assert.NotEqual("Open_On", held.ActionId);
    }

    /// <summary>
    /// A client picking a button again mends a slot cleared by hand, though the program it sends is the one the server
    /// already holds.
    /// </summary>
    [Fact]
    public void AButtonAClientPicksAgainIsWritten()
    {
        _sim.Editor.NewButtonSystem("Airlock");
        for (var i = 0; i < 3; i++)
            _sim.Tick();

        var panel = Assert.Single(_sim.Panels(), each => each.Toolbar.Holds(0));
        panel.Toolbar.Changing().Clear(0).Apply();
        _sim.Tick();

        var (anna, hers, _) = Lent();
        _wire.As(anna, () => hers.Wire(_block, panel.Id));
        _wire.Settle();
        for (var i = 0; i < 3; i++)
            _sim.Tick();

        Assert.True(panel.Toolbar.Holds(0), "the server did not write the button picked again");
    }

    /// <summary>A button a client picks and backs out of is left as it is, when the server's wiring next changes.</summary>
    [Fact]
    public void AButtonAClientBacksOutOfIsLeftAlone()
    {
        _sim.Editor.NewButtonSystem("Airlock");
        for (var i = 0; i < 3; i++)
            _sim.Tick();

        var panel = Assert.Single(_sim.Panels(), each => each.Toolbar.Holds(0));
        var door = _sim.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door");
        panel.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(door.Id, "Open_On"), new List<TerminalActionParameter>()).Apply();
        _sim.Tick();

        var (anna, hers, _) = Lent();
        _wire.As(anna, () =>
        {
            hers.Wire(_block, panel.Id);
            hers.Unwire(_block);
        });
        _wire.Settle();
        _sim.Editor.Program.Rewire();
        for (var i = 0; i < 3; i++)
            _sim.Tick();

        Assert.True(panel.Toolbar.TryGetItem(0, out var held));
        Assert.Equal("Open_On", held.ActionId);
    }

    /// <summary>
    /// The copy a client edits holds the program and runs none of it, and what it makes is named after
    /// the client's player.
    /// </summary>
    [Fact]
    public void ACopyRunsNothing()
    {
        Repeating();
        _sim.Editor.SwitchOn();

        string program;
        string state;
        _sim.Unit.Written(out program, out state);
        var anna = _wire.Client("Anna");
        SprocketUnit copy = null!;
        _wire.As(anna, () => copy = SprocketUnit.Copy(_sim.Machine.Sprocket.Block, null, _sim.Texts, program, state));

        Assert.Single(copy.Editor.Program.Systems);
        Assert.Equal("Anna", copy.Editor.Author);

        _sim.Machine.Forget();
        _wire.As(anna, () =>
        {
            for (var frame = 1; frame <= 300; frame++)
                copy.Tick(frame);
        });

        Assert.Empty(_sim.Machine.Log);

        // And the server's own, ticked the same, does what the copy would have.
        for (var i = 0; i < 300; i++)
            _sim.Tick();

        Assert.NotEmpty(_sim.Machine.Log);
    }
}
