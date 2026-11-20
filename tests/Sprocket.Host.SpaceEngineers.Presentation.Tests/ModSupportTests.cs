using System.IO;
using Sandbox.ModAPI;
using VRage;
using VRage.Utils;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Mods;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel.Input;
using Sprocket.ModSupport;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using Xunit;

// A mod's callbacks read the state its test changes as the test goes, standing in for a mod's block.
// ReSharper disable AccessToModifiedClosure

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A mod supports Sprocket through SprocketModSupport, and players use its blocks' members as Sprocket's own.</summary>
public class ModSupportTests
{
    const string Space = "test.glow_mod";

    readonly Dictionary<long, List<Action<object>>> _handlers = new();

    /// <summary>A mod finds Sprocket whichever of the two loads first, and its registration reaches Sprocket's members and namespaces.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AModFindsSprocketWhicheverLoadsFirst(bool sprocketFirst)
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var members = new ModMembers();
        var registration = Registered(members, sprocketFirst, api =>
            api.Block(Type(light), Subtype(light)).Flag("glowing", _ => true).Named("glowing", "Whether it glows.").Reading("glows", "is dark"));

        Assert.True(registration.IsRegistered, registration.Refusal ?? "the mod never heard Sprocket");
        Assert.True(members.Claims(light.Block), "the registration never reached Sprocket");
        Assert.True(HostNamespaces.Has(Space), "the mod's namespace was never registered");

        registration.Dispose();
        Assert.False(members.Claims(light.Block), "the registration outlived its disposal");
        Assert.False(HostNamespaces.Has(Space), "the namespace outlived its mod");
    }

    /// <summary>A second mod claiming a namespace another has is refused, and says so.</summary>
    [Fact]
    public void ANamespaceTakenIsRefused()
    {
        var members = new ModMembers();
        var first = Registered(members, true, delegate { });
        var second = Registered(members, true, delegate { }, "Copycat");

        Assert.True(first.IsRegistered);
        Assert.False(second.IsRegistered);
        Assert.Contains(Space, second.Refusal);
    }

    /// <summary>A registration sent without the wrapper is held to the convention too: a namespace breaking it is refused, with why.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Glow Mod")]
    [InlineData("test..glow")]
    public void ARegistrationWithAMalformedNamespaceIsRefused(string space)
    {
        var answer = RegisteredRaw(new ModMembers(), new Dictionary<string, object>
        {
            { SprocketWire.Namespace, space }, { SprocketWire.Name, "Glow" }, { SprocketWire.Author, "Tester" }
        });

        Assert.True(answer.ContainsKey(SprocketWire.Refused));
        Assert.False(string.IsNullOrEmpty((string)answer[SprocketWire.Refused]));
        Assert.Null(HostNamespaces.Find(space));
    }

    /// <summary>A member whose id breaks the convention, sent without the wrapper, is left out alone, and the mod's other members stand.</summary>
    [Fact]
    public void AMemberWithAMalformedIdIsLeftOutAndTheRestStand()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var members = new ModMembers();
        var answer = RegisteredRaw(members, new Dictionary<string, object>
        {
            { SprocketWire.Namespace, Space }, { SprocketWire.Name, "Glow" }, { SprocketWire.Author, "Tester" },
            { SprocketWire.Scopes, new[] { new Dictionary<string, object> { { SprocketWire.BlockType, Type(light) } } } },
            {
                SprocketWire.Members, new[]
                {
                    new Dictionary<string, object> { { SprocketWire.Scope, 0 }, { SprocketWire.Id, "Ride-Height" } },
                    new Dictionary<string, object> { { SprocketWire.Scope, 0 }, { SprocketWire.Id, "ride_height" } }
                }
            }
        });

        Assert.False(answer.ContainsKey(SprocketWire.Refused));
        var found = new List<KeyValuePair<ModRegistration, Dictionary<string, object>>>();
        members.For(light.Block, found);
        Assert.Equal(new[] { "ride_height" }, found.Select(member => (string)member.Value[SprocketWire.Id]));
    }

    /// <summary>A mod's blocks are named by type and subtype only: a scope sent without the wrapper that names no block type takes no block.</summary>
    [Fact]
    public void AScopeNamingNoBlockTypeTakesNoBlock()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var members = new ModMembers();
        RegisteredRaw(members, new Dictionary<string, object>
        {
            { SprocketWire.Namespace, Space }, { SprocketWire.Name, "Glow" }, { SprocketWire.Author, "Tester" },
            { SprocketWire.Scopes, new[] { new Dictionary<string, object>() } },
            { SprocketWire.Members, new[] { new Dictionary<string, object> { { SprocketWire.Scope, 0 }, { SprocketWire.Id, "ride_height" } } } }
        });

        Assert.False(members.Claims(light.Block));
        var found = new List<KeyValuePair<ModRegistration, Dictionary<string, object>>>();
        members.For(light.Block, found);
        Assert.Empty(found);
    }

    /// <summary>An id breaking the naming convention is refused as it is given, so a mod's author meets it on the first load.</summary>
    [Theory]
    [InlineData("RideHeight")]
    [InlineData("ride-height")]
    [InlineData("ride.height")]
    [InlineData("ride__height")]
    [InlineData("_ride")]
    [InlineData("ride_")]
    [InlineData("2ride")]
    public void AnIdBreakingTheConventionThrows(string id)
    {
        Assert.False(SprocketIds.Follows(id));
        Assert.Throws<ArgumentException>(() => new SprocketFailure(id, "x"));
    }

    /// <summary>A namespace is checked part by part, and a mod without a name or author is refused as it registers.</summary>
    [Fact]
    public void AModsOwnDetailsAreChecked()
    {
        Assert.Throws<ArgumentException>(() => SprocketModSupport.Register(new SprocketMod { Namespace = "Malforge.Hover", Name = "x", Author = "y" }, delegate { }));
        Assert.Throws<ArgumentException>(() => SprocketModSupport.Register(new SprocketMod { Namespace = "malforge.hover", Author = "y" }, delegate { }));
        Assert.Throws<ArgumentException>(() => SprocketModSupport.Register(new SprocketMod { Namespace = "malforge.hover", Name = "x" }, delegate { }));
    }

    /// <summary>A value a mod defines is offered on its blocks in the step's selector under its name, and a step sets it through the mod's own setter.</summary>
    [Fact]
    public void AValueIsOfferedAndSetThroughTheMod()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var glowing = false;
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light), Subtype(light)).InGroup(SprocketGroup.Appearance)
            .Flag("glowing", _ => glowing, (_, on) => glowing = on).Named("glowing", "Whether it glows.").Reading("glows", "is dark"));

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();
        Assert.Contains("glowing", sim.Editor.Members());

        sim.Editor.PressChoice("glowing");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        sim.Editor.PressPrimary();
        Pressed(sim);

        Assert.True(glowing, "the step never reached the mod's setter");
    }

    /// <summary>A block type given as the TypeId its definition has, without <c>MyObjectBuilder_</c>, names the same blocks as the full object builder type.</summary>
    [Fact]
    public void ABlockTypeIsNamedByItsDefinitionsTypeId()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        const string builder = "MyObjectBuilder_";
        Assert.StartsWith(builder, Type(light));
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light).Substring(builder.Length), Subtype(light)).InGroup(SprocketGroup.Appearance)
            .Flag("glowing", _ => false, (_, _) => { }).Named("glowing", "Whether it glows.").Reading("glows", "is dark"));

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();
        Assert.Contains("glowing", sim.Editor.Members());
    }

    /// <summary>An action a mod defines is given its options as a step fills them, typed as the mod declared them.</summary>
    [Fact]
    public void AnActionRunsWithItsTypedOptions()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var height = double.NaN;
        var pace = Pace.Gentle;
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api =>
        {
            var paces = api.DefineType<Pace>().Naming(Pace.Gentle, "rolig").Naming(Pace.Brisk, "rask");
            api.Block(Type(light), Subtype(light)).InGroup(SprocketGroup.Behavior)
                .Action("rise", Option.Number("height").Unit("m").Range(1, 20), Option.Choice("pace", paces), (_, h, p) =>
                {
                    height = h;
                    pace = p;
                })
                .Named("rise to", "Rises to a height.");
        });

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("rise to");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "12";
        sim.Editor.Pick().Arguments!.Form.Fields[1].Text = "rask";
        sim.Editor.PressPrimary();
        Pressed(sim);

        Assert.Equal(12.0, height);
        Assert.Equal(Pace.Brisk, pace);
        sim.Unit.Written(out var written, out _);
        Assert.Contains("brisk", written);
        Assert.DoesNotContain("rask", written);
    }

    /// <summary>An option that is a length of time is given in seconds or ticks beside its number, stored in the program as a length, and given to the mod as a TimeSpan.</summary>
    [Fact]
    public void ADurationOptionReachesTheModAsATimeSpan()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var held = TimeSpan.Zero;
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Behavior)
            .Action("hold", Option.Duration("for"), (_, length) => held = length).Named("hold for", "Holds still for a while."));

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("hold for");
        var field = Assert.IsType<DurationField>(sim.Editor.Pick().Arguments!.Form.Fields[0]);
        field.Text = "2s";
        sim.Editor.PressPrimary();
        Pressed(sim);

        sim.Unit.Written(out var written, out _);
        Assert.True(written.Contains("hold(2s)"), written);
        Assert.Equal(TimeSpan.FromSeconds(2), held);
    }

    /// <summary>A value that is a length of time is set by a step in seconds or ticks, and given to the mod's setter as a TimeSpan.</summary>
    [Fact]
    public void ADurationValueIsSetAsATimeSpan()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var delay = TimeSpan.Zero;
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Behavior)
            .Duration("delay", _ => delay, (_, length) => delay = length).Named("delay", "How long it waits before it glows."));

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("delay");
        Assert.IsType<DurationField>(sim.Editor.Pick().Arguments!.Form.Fields[0]).Text = "90t";
        sim.Editor.PressPrimary();
        Pressed(sim);

        Assert.Equal(TimeSpan.FromSeconds(1.5), delay);
    }

    /// <summary>Switching a length's unit keeps the length and says it in the other unit.</summary>
    [Fact]
    public void ADurationsUnitKeepsTheLength()
    {
        var sim = Simulation.Fresh();
        var field = new DurationField(sim.Editor.Texts, new RelayCommand(delegate { }));
        field.Text = "2s";
        field.Children.OfType<Lumel.Controls.Switch>().Single().Command!.Execute();
        Assert.Equal("120t", field.Text);
        field.Children.OfType<Lumel.Controls.Switch>().Single().Command!.Execute();
        Assert.Equal("2s", field.Text);
    }

    /// <summary>A member a mod stands in a group of its own is listed under the mod's heading, and shows the mod's description.</summary>
    [Fact]
    public void AModsOwnGroupIsListedUnderItsHeading()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light), Subtype(light)).InGroup("Repulsion")
            .Flag("lifted", _ => true, delegate { }).Named("lifted", "Whether the light floats.").Reading("floats", "rests"));

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();

        var listed = sim.Editor.MembersHeaded();
        var heading = listed.IndexOf("#Repulsion");
        Assert.True(heading >= 0, string.Join(", ", listed));
        Assert.Equal("lifted", listed[heading + 1]);

        sim.Editor.PressChoice("lifted");
        Assert.Equal("Whether the light floats.", sim.Editor.Pick().Arguments!.About.Markdown);
    }

    /// <summary>A narrower set of blocks changes the details of a member it inherits, and a member stands in a group other than its scope's.</summary>
    [Fact]
    public void AnOverrideChangesOnlyTheNarrowerBlocks()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var members = new ModMembers();
        Registered(members, true, api =>
        {
            var lights = api.Block(Type(light)).InGroup("Repulsion");
            lights.Number("lift", _ => 1.0).Named("lift", "How far it lifts.").Range(0, 10);
            lights.Subtypes(Subtype(light)).Override("lift").Range(0, 3).Described("It lifts less here.");
            lights.Flag("glowing", _ => true).Named("glowing", "Whether it glows.").Reading("glows", "is dark").InGroup(SprocketGroup.Appearance);
        });

        var found = new List<KeyValuePair<ModRegistration, Dictionary<string, object>>>();
        members.For(light.Block, found);
        var lift = found.Single(member => (string)member.Value[SprocketWire.Id] == "lift").Value;
        Assert.Equal(3.0, ((Func<IMyTerminalBlock, double>)lift[SprocketWire.Most])(light.Block));
        Assert.Equal("It lifts less here.", ((Func<string>)lift[SprocketWire.Described])());
        Assert.Equal("lift", ((Func<string>)lift[SprocketWire.Named])());

        var glowing = found.Single(member => (string)member.Value[SprocketWire.Id] == "glowing").Value;
        Assert.Equal("appearance", glowing[SprocketWire.SprocketsGroup]);
        Assert.False(glowing.ContainsKey(SprocketWire.Group));
    }

    /// <summary>Two mods may each give a block a member of the same id: each stands in its own mod's namespace, two members of the block.</summary>
    [Fact]
    public void TwoModsMembersOfOneIdAreTwoMembers()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).Action("rise", _ => { }).Named("rise", "Rises."));
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).Action("rise", _ => { }).Named("rise", "Rises too."), "Other", "test.other_mod");

        var rises = sim.Kinds.For(light.Block).Actions.Where(action => action.Id == "rise").Select(action => action.Namespace).OrderBy(space => space).ToList();
        Assert.Equal(new[] { "test.glow_mod", "test.other_mod" }, rises);
    }

    /// <summary>A signal a mod raises starts the sequence waiting on it each time it is raised, with nothing watched in between.</summary>
    [Fact]
    public void ARaisedSignalStartsItsSequenceEachTime()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var chimed = 0;
        var sim = Simulation.On(rig);
        var gains = Chiming(sim, light, () => chimed++);
        ChimeOnGrip(sim);
        Assert.Equal(0, chimed);

        gains.Raise(light.Block);
        for (var i = 0; i < 5; i++)
            sim.Tick();
        gains.Raise(light.Block);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Equal(2, chimed);
    }

    /// <summary>A sequence waiting on a raised signal costs the rota nothing while it is not raised: no more shifts than the same sequence started by a button, which nothing watches either.</summary>
    [Fact]
    public void AnIdleRaisedSignalCostsNothing()
    {
        Assert.Equal(IdleShifts(false), IdleShifts(true));
    }

    /// <summary>The shifts the rota does over 200 idle ticks for a switched-on system whose sequence chimes, started by a raised signal where <paramref name="raised" /> says so and by a button otherwise.</summary>
    int IdleShifts(bool raised)
    {
        // Each run is a world of its own, which starts with no mod's namespace taken and no mod listening.
        HostNamespaces.Clear();
        _handlers.Clear();
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api =>
        {
            var lights = api.Block(Type(light)).InGroup(SprocketGroup.Behavior);
            lights.DefineSignal("gains_grip").Named("gains grip", "The field touches the ground.");
            lights.Action("chime", _ => { }).Named("chime", "Chimes.");
        }, unitOf: _ => sim.Unit);

        if (!raised)
            sim.Editor.NewButtonSystem("Glow");
        else
        {
            sim.Editor.NewSystem("Glow");
            sim.Editor.PressStart(SentenceText.WhenWord);
            sim.Editor.PressBlock("Glow Light");
            sim.Editor.PressChoice("gains grip");
            sim.Editor.PressPrimary();
            sim.Tick();
        }

        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("chime");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        for (var i = 0; i < 20; i++)
            sim.Tick();

        var before = sim.Rota.Done;
        for (var i = 0; i < 200; i++)
            sim.Tick();
        return sim.Rota.Done - before;
    }

    /// <summary>A raise on a block nothing waits on walks no trigger beyond the one look-up, and a signal waited on costs nothing while it is not raised.</summary>
    [Fact]
    public void ARaiseCostsNothingWhereNothingWaits()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        SprocketSignal? gains = null;
        Registered(sim.Kinds.Mods, true, api => gains = api.Block(Type(light)).DefineSignal("gains_grip").Named("gains grip", "Touches."), unitOf: _ => null);

        gains!.Raise(light.Block);
        Assert.Equal(0, sim.Unit.Runner.RaisesWalked);
    }

    /// <summary>A step on a mod's member, in a world loaded without that mod, is stored as it was written and waits for the mod's namespace.</summary>
    [Fact]
    public void AStepOnAMissingModsMemberIsKeptAndWaits()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Appearance)
            .Flag("glowing", _ => false, delegate { }).Named("glowing", "Whether it glows.").Reading("glows", "is dark"));

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("glowing");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        sim.Tick();
        sim.Unit.Written(out var before, out _);

        HostNamespaces.Clear();
        sim.Reload();
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Unit.Written(out var after, out _);
        Assert.Equal(Program(before), Program(after));
        var sequence = sim.Unit.Program.Systems[0].Sequences[0];
        var fault = SequenceSummary.Fault(sim.Runner.Get(sequence));
        Assert.True(fault.AwaitsNamespace, fault.Problem.ToString());
    }

    /// <summary>A value of a mod's own type is offered by its words' names and stored in the program by its words, so it reads the same in any language.</summary>
    [Fact]
    public void AValueOfAModsTypeIsKeptByItsWords()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var pace = Pace.Gentle;
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api =>
        {
            var paces = api.DefineType<Pace>().Naming(Pace.Gentle, "rolig").Naming(Pace.Brisk, "rask");
            api.Block(Type(light)).InGroup(SprocketGroup.Behavior)
                .Choice("pace", paces, _ => pace, (_, chosen) => pace = chosen).Named("pace", "How fast it goes.");
        });

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("pace");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "rask";
        sim.Editor.PressPrimary();
        Pressed(sim);

        sim.Unit.Written(out var written, out _);
        Assert.Contains("brisk", written);
        Assert.Equal(Pace.Brisk, pace);
        Assert.DoesNotContain("rask", written);
    }

    /// <summary>Registers the test mod's signal gains_grip and action chime, which calls <paramref name="chime" />, on the light.</summary>
    SprocketSignal Chiming(Simulation sim, Lamp light, Action chime, string space = Space)
    {
        SprocketSignal? gains = null;
        Registered(sim.Kinds.Mods, true, api =>
        {
            var lights = api.Block(Type(light)).InGroup(SprocketGroup.Behavior);
            gains = lights.DefineSignal("gains_grip").Named("gains grip", "The field touches the ground.");
            lights.Action("chime", _ => chime()).Named("chime", "Chimes.");
        }, space: space, unitOf: _ => sim.Unit);
        return gains!;
    }

    /// <summary>Makes a system that chimes the light when it gains grip, and switches it on.</summary>
    static void ChimeOnGrip(Simulation sim)
    {
        sim.Editor.NewSystem("Glow");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("gains grip");
        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("chime");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        for (var i = 0; i < 5; i++)
            sim.Tick();
    }

    /// <summary>What <paramref name="written" /> says from its first brace on, past the date a reload writes anew.</summary>
    static string Program(string written) => written.Substring(written.IndexOf("{", StringComparison.Ordinal));

    /// <summary>A mod's member named by its own texts is named again in the language the player changes to.</summary>
    [Fact]
    public void AModsMemberIsNamedAgainInANewLanguage()
    {
        var folder = Path.Combine(Path.GetTempPath(), "sprocket-mod-texts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Resx(Path.Combine(folder, "MyTexts.resx"), "TestModSupport_Glowing", "glowing");
            MyTexts.LoadTexts(folder);
            var rig = Rig.Demo();
            var light = rig.Light(40, "Glow Light");
            var sim = Simulation.On(rig);
            Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Appearance)
                .Flag("glowing", _ => true, delegate { })
                .Named(MyStringId.GetOrCompute("TestModSupport_Glowing"), MyStringId.GetOrCompute("TestModSupport_Glowing"))
                .Reading("glows", "is dark"));

            sim.Editor.NewButtonSystem("Glow");
            sim.Tick();
            sim.Editor.PressAdd("task");
            sim.Editor.PressBlock("Glow Light");
            sim.Tick();
            Assert.Contains("glowing", sim.Editor.Members());

            Resx(Path.Combine(folder, "MyTexts.resx"), "TestModSupport_Glowing", "lysende");
            MyTexts.LoadTexts(folder);
            sim.Kinds.Reword();
            sim.Unit.Reword();
            sim.Editor.Back();
            sim.Tick();
            sim.Editor.PressAdd("task");
            sim.Editor.PressBlock("Glow Light");
            sim.Tick();
            Assert.Contains("lysende", sim.Editor.Members());
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>Writes <paramref name="path" />, a texts file holding <paramref name="key" /> as <paramref name="text" />.</summary>
    static void Resx(string path, string key, string text)
    {
        File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n"
            + "  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>\n"
            + "  <resheader name=\"version\"><value>2.0</value></resheader>\n"
            + "  <resheader name=\"reader\"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>\n"
            + "  <resheader name=\"writer\"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>\n"
            + $"  <data name=\"{key}\" xml:space=\"preserve\"><value>{text}</value></data>\n</root>\n");
    }

    /// <summary>A step waiting for pending tasks waits on a mod's awaitable action until the task the mod returned completes.</summary>
    [Fact]
    public void AStepWaitsOnAnAwaitableUntilItsTaskCompletes()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var after = 0;
        SprocketTask? task = null;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => task = new SprocketTask()), () => after++);

        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.NotNull(task);
        Assert.Equal(0, after);

        task!.Complete();
        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Equal(1, after);
    }

    /// <summary>The shorthand awaitable waits until its condition holds of the block, looked at while a step waits.</summary>
    [Fact]
    public void AnAwaitableMadeWithPolledDoneWaitsForItsCondition()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var settled = false;
        var after = 0;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => { }).PolledDone(_ => settled), () => after++);

        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Equal(0, after);

        settled = true;
        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Equal(1, after);
    }

    /// <summary>An option that is a percentage with no range of its own is asked from 0 to 100, as Sprocket's own percentages are; one with a range keeps it.</summary>
    [Fact]
    public void APercentOptionWithNoRangeIsAskedFrom0To100()
    {
        var plain = new Dictionary<string, object> { { SprocketWire.Type, SprocketWire.TypeNumber }, { SprocketWire.Percent, true } };
        var ranged = new Dictionary<string, object>(plain) { { SprocketWire.Most, (Func<IMyTerminalBlock, double>)(_ => 0.5) } };

        var fields = ModFields.Asked(new[] { plain, ranged }, null!, new ModMembers());

        Assert.Equal(("0", "100", " %"), (fields[0].Least, fields[0].Most, fields[0].Unit));
        Assert.Equal(("0", "50"), (fields[1].Least, fields[1].Most));
    }

    /// <summary>A polled condition that throws fails the task, so the run ends as a failure rather than the mod's error reaching Sprocket's update.</summary>
    [Fact]
    public void APolledConditionThatThrowsFailsTheTask()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var after = 0;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => { }).PolledDone(_ => throw new InvalidOperationException("broken")), () => after++);

        for (var i = 0; i < 300; i++)
            sim.Tick();

        Assert.Equal(0, after);
        var runner = sim.Runner.Get(sim.Unit.Program.Systems[0].Sequences[0]);
        Assert.False(runner.IsRunning, "the run went on waiting on a condition that throws");
        Assert.Equal(SequenceProblem.Failed, SequenceSummary.Fault(runner).Problem);
    }

    /// <summary>A task that fails ends the run as a problem in the mod's own words, and nothing after it runs.</summary>
    [Fact]
    public void AFailedTaskEndsTheRunInTheModsWords()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var after = 0;
        SprocketTask? task = null;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => task = new SprocketTask()), () => after++);
        for (var i = 0; i < 60; i++)
            sim.Tick();

        task!.Fail(new SprocketFailure("lost_grip", "lost its grip on the ground"));
        for (var i = 0; i < 300; i++)
            sim.Tick();

        Assert.Equal(0, after);
        var sequence = sim.Unit.Program.Systems[0].Sequences[0];
        var fault = SequenceSummary.Fault(sim.Runner.Get(sequence));
        Assert.Equal(SequenceProblem.Failed, fault.Problem);
        Assert.Equal("lost its grip on the ground", fault.Because.About);
    }

    /// <summary>A task made by Until with a failure to look for fails in the mod's words once that failure is named, while a step waits on it.</summary>
    [Fact]
    public void AnUntilTaskFailsAsItsFailingSays()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var after = 0;
        var lost = false;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => SprocketTask.Polled(() => false,
            () => lost ? new SprocketFailure("lost_grip", "lost its grip on the ground") : null)), () => after++);
        for (var i = 0; i < 60; i++)
            sim.Tick();
        var runner = sim.Runner.Get(sim.Unit.Program.Systems[0].Sequences[0]);
        Assert.True(runner.IsRunning, "the task ended before it named a failure");

        lost = true;
        for (var i = 0; i < 300; i++)
            sim.Tick();

        Assert.Equal(0, after);
        var fault = SequenceSummary.Fault(runner);
        Assert.Equal(SequenceProblem.Failed, fault.Problem);
        Assert.Equal("lost its grip on the ground", fault.Because.About);
    }

    /// <summary>A task that fails is a failure the sequence's catch handles; one that completes passes the catch over.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACatchHandlesAFailedTask(bool fails)
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var caught = 0;
        SprocketTask? task = null;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => task = new SprocketTask()), () => caught++, true);
        for (var i = 0; i < 60; i++)
            sim.Tick();

        if (fails)
            task!.Fail(new SprocketFailure("lost_grip", "lost its grip on the ground"));
        else
            task!.Complete();
        for (var i = 0; i < 300; i++)
            sim.Tick();

        Assert.Equal(fails ? 1 : 0, caught);
    }

    /// <summary>A task something else took out of the mod's hands ends the run with the conflicted warning in the mod's words.</summary>
    [Fact]
    public void AConflictedTaskEndsTheRunAsAWarning()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        SprocketTask? task = null;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => task = new SprocketTask()), () => { });
        for (var i = 0; i < 60; i++)
            sim.Tick();

        task!.Conflicted(new SprocketFailure("overridden", "was told to rise by someone else"));
        for (var i = 0; i < 300; i++)
            sim.Tick();

        var fault = SequenceSummary.Fault(sim.Runner.Get(sim.Unit.Program.Systems[0].Sequences[0]));
        Assert.Equal(SequenceProblem.Conflicted, fault.Problem);
        Assert.True(fault.IsWarning);
        Assert.Equal("was told to rise by someone else", fault.Because.About);
    }

    /// <summary>A task that is stuck keeps the step waiting and says why in the mod's words; once moving again it completes as usual.</summary>
    [Fact]
    public void AStuckTaskWaitsOnAndSaysWhy()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var after = 0;
        SprocketTask? task = null;
        var sim = Waiting(rig, light, lights => lights.Awaitable("settle", _ => task = new SprocketTask()), () => after++);
        for (var i = 0; i < 60; i++)
            sim.Tick();

        task!.Stuck("can't find the ground");
        for (var i = 0; i < 300; i++)
            sim.Tick();
        var sequence = sim.Unit.Program.Systems[0].Sequences[0];
        var fault = SequenceSummary.Fault(sim.Runner.Get(sequence));
        Assert.Equal(SequenceProblem.Stuck, fault.Problem);
        Assert.Equal("can't find the ground", fault.Because.About);
        Assert.Equal(0, after);

        task.Stuck(null);
        task.Complete();
        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Equal(1, after);
    }

    /// <summary>A system with a button whose sequence runs the light's awaitable action, waits for it, then chimes, or chimes only in its catch where <paramref name="caught" /> says so; pressed once.</summary>
    Simulation Waiting(Rig rig, Lamp light, Func<Sprocket.ModSupport.SprocketBlock, SprocketMember> awaitable, Action chime, bool caught = false)
    {
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api =>
        {
            var lights = api.Block(Type(light)).InGroup(SprocketGroup.Behavior);
            awaitable(lights).Named("settle", "Settles the light.");
            lights.Action("chime", _ => chime()).Named("chime", "Chimes.");
        }, unitOf: _ => sim.Unit);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        Task(sim, "settle");
        sim.Editor.PressAdd(SentenceText.AwaitWords);
        Task(sim, "chime");
        if (caught)
        {
            var steps = sim.Editor.Program.Sequences[0].Steps;
            var chiming = steps[steps.Count - 1];
            steps.RemoveAt(steps.Count - 1);
            steps.Add(new Catch());
            steps.Add(chiming);
            sim.Editor.Program.Edited();
            sim.Tick();
        }

        Pressed(sim);
        return sim;
    }

    /// <summary>A task of the light's mod action <paramref name="action" />, added to the sequence on the screen.</summary>
    static void Task(Simulation sim, string action)
    {
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice(action);
        sim.Editor.PressPrimary();
    }

    /// <summary>An awaitable made with a kick and PolledDone is told its stop through Cancelling, with the cause, and has already said it stopped.</summary>
    [Fact]
    public void APolledAwaitableIsToldItsStop()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        SprocketCancel? asked = null;
        var lines = GameLog.Lines.Count;
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Behavior)
            .Awaitable("settle", _ => { }).PolledDone(_ => false).Cancelling((_, stop) => asked = stop)
            .Named("settle", "Settles the light."), unitOf: _ => sim.Unit, rota: sim.Rota);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        Task(sim, "settle");
        sim.Editor.PressAdd(SentenceText.AwaitWords);
        Pressed(sim);
        for (var i = 0; i < 30; i++)
            sim.Tick();
        var system = sim.Unit.Program.Systems[0];
        sim.Runner.Halt(system.Sequences[0], system.Uses[0], StopCause.Hand, 0);
        for (var i = 0; i < 100; i++)
            sim.Tick();

        Assert.Equal(SprocketCancelCause.Stopped, asked!.Cause);
        Assert.DoesNotContain(GameLog.Lines.Skip(lines), line => line.StartsWith("Guideline breach"));
    }

    /// <summary>A stop step of the program lets go of a mod's task at once, saying it was stopped, and the run is over.</summary>
    [Fact]
    public void AStopLetsGoOfTheModsTaskAtOnce()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        SprocketCancel? asked = null;
        var sim = Stopped(rig, light, () =>
        {
            var task = new SprocketTask();
            task.Cancelling += cancel => asked = cancel;
            return task;
        }, StopCause.Sequence);

        Assert.Equal(SprocketCancelCause.Stopped, asked!.Cause);
        var runner = sim.Runner.Get(sim.Unit.Program.Systems[0].Sequences[0]);
        Assert.False(runner.IsRunning);
        Assert.Equal(StopCause.Sequence, runner.StoppedBy);
    }

    /// <summary>An abort below a mod's task the run has not awaited yet stops the task, saying the program aborted.</summary>
    [Fact]
    public void AnAbortStopsATaskNotAwaitedSayingAborted()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        SprocketCancel? asked = null;
        var started = 0;
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Behavior)
            .Awaitable("settle", _ =>
            {
                started++;
                var task = new SprocketTask();
                task.Cancelling += cancel => asked = cancel;
                return task;
            }).Named("settle", "Settles the light."), unitOf: _ => sim.Unit, rota: sim.Rota);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        Task(sim, "settle");
        sim.Editor.PressAdd("abort");
        // The editor awaits a task as it is added; without that await, the task still runs as the abort is met.
        var steps = sim.Unit.Program.Systems[0].Sequences[0].Steps;
        steps.Remove(Assert.Single(steps.OfType<Await>()));
        Assert.IsType<Abort>(steps.Last());
        Pressed(sim);

        Assert.Equal(1, started);
        Assert.Equal(SprocketCancelCause.Aborted, asked!.Cause);
        Assert.False(sim.Runner.Get(sim.Unit.Program.Systems[0].Sequences[0]).IsRunning);
    }

    /// <summary>A person's stop, a switch-off and a stopping trigger of the program tell the mod's task their own causes.</summary>
    [Theory]
    [InlineData(StopCause.Hand, SprocketCancelCause.Stopped)]
    [InlineData(StopCause.Switch, SprocketCancelCause.SwitchedOff)]
    [InlineData(StopCause.Trigger, SprocketCancelCause.Stopped)]
    public void AStopSaysItsCause(StopCause by, SprocketCancelCause cause)
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        SprocketCancel? asked = null;
        Stopped(rig, light, () =>
        {
            var task = new SprocketTask();
            task.Cancelling += cancel => asked = cancel;
            return task;
        }, by);

        Assert.Equal(cause, asked!.Cause);
    }

    /// <summary>A person's stop of a run whose finally then fails tells the mod's task it was stopped, not that something failed.</summary>
    [Fact]
    public void AStopWhoseFinallyFailsSaysStopped()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        SprocketCancel? asked = null;
        var lost = new SprocketFailure("lost", "The light slipped.");
        Registered(sim.Kinds.Mods, true, api =>
        {
            var lamp = api.Block(Type(light)).InGroup(SprocketGroup.Behavior);
            lamp.Awaitable("settle", _ =>
            {
                var task = new SprocketTask();
                task.Cancelling += cancel => asked = cancel;
                return task;
            }).Named("settle", "Settles the light.");
            lamp.Awaitable("slip", _ => SprocketTask.Failed(lost)).Named("slip", "Slips the light.");
        }, unitOf: _ => sim.Unit, rota: sim.Rota);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        Task(sim, "settle");
        sim.Editor.PressAdd(SentenceText.AwaitWords);
        sim.Editor.PressAdd(SentenceText.FinallyWord);
        Task(sim, "slip");
        sim.Editor.PressAdd(SentenceText.AwaitWords);
        Pressed(sim);
        for (var i = 0; i < 30; i++)
            sim.Tick();
        var system = sim.Unit.Program.Systems[0];
        Assert.IsType<Finally>(system.Sequences[0].Steps[system.Sequences[0].FinallyAt]);

        sim.Runner.Halt(system.Sequences[0], system.Uses[0], StopCause.Hand, 0);
        for (var i = 0; i < 30; i++)
            sim.Tick();

        Assert.Equal(SprocketCancelCause.Stopped, asked!.Cause);
        Assert.False(sim.Runner.Get(system.Sequences[0]).IsRunning);
    }

    /// <summary>
    /// A task that has not said it stopped a second after its stop is logged once as breaking the guidelines; one that has is
    /// not; and the look costs one shift of the rota for each stopped task.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASlowStopIsLoggedOnceAtTheCostOfOneLook(bool confirms)
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var lines = GameLog.Lines.Count;
        var sim = Stopped(rig, light, () =>
        {
            var task = new SprocketTask();
            if (confirms)
                task.Cancelling += delegate { task.Cancel(); };
            return task;
        }, StopCause.Sequence);
        Assert.Equal(confirms ? 0 : 1, sim.Kinds.Mods.Stops!.Waiting);

        var before = sim.Rota.Done;
        for (var i = 0; i < 200; i++)
            sim.Tick();
        var spent = sim.Rota.Done - before;

        Assert.Equal(confirms ? 0 : 1, GameLog.Lines.Skip(lines).Count(line => line.StartsWith("Guideline breach")));
        Assert.Equal(0, sim.Kinds.Mods.Stops!.Waiting);
        var idle = IdleShifts(false);
        Assert.True(spent <= idle + (confirms ? 0 : 1), $"{spent} shifts against {idle} idle");
    }

    /// <summary>A system whose button runs the light's awaitable action <paramref name="start" /> and waits for it, pressed once and then stopped <paramref name="by" />.</summary>
    Simulation Stopped(Rig rig, Lamp light, Func<SprocketTask> start, StopCause by)
    {
        var sim = Simulation.On(rig);
        Registered(sim.Kinds.Mods, true, api => api.Block(Type(light)).InGroup(SprocketGroup.Behavior)
            .Awaitable("settle", _ => start()).Named("settle", "Settles the light."), unitOf: _ => sim.Unit, rota: sim.Rota);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        Task(sim, "settle");
        sim.Editor.PressAdd(SentenceText.AwaitWords);
        Pressed(sim);
        for (var i = 0; i < 30; i++)
            sim.Tick();

        var system = sim.Unit.Program.Systems[0];
        sim.Runner.Halt(system.Sequences[0], system.Uses[0], by, 0);
        return sim;
    }

    /// <summary>A member made without what it needs is refused as the mod registers: a member without a name, a flag without readings, an awaitable that never ends.</summary>
    [Fact]
    public void AMemberMissingWhatItNeedsIsRefused()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var members = new ModMembers();
        Assert.False(Registered(members, true, api => api.Block(Type(light)).Number("lift", _ => 1.0)).IsRegistered);
        Assert.False(Registered(members, true, api => api.Block(Type(light)).Flag("lit", _ => true).Named("lit", "Lit.")).IsRegistered);
        Assert.False(Registered(members, true, api => api.Block(Type(light)).Awaitable("rise", _ => { }).Named("rise", "Rises.")).IsRegistered);
        Assert.False(members.Claims(light.Block));
    }

    /// <summary>What a mod has a task call as it ends is called once, however it ends, and at once where it has already ended.</summary>
    [Theory]
    [InlineData(SprocketWire.Completed)]
    [InlineData(SprocketWire.Failed)]
    [InlineData(SprocketWire.Cancelled)]
    public void ATaskCallsItsEndingOnce(string outcome)
    {
        var task = new SprocketTask();
        var ended = 0;
        task.Ended(() => ended++);
        Assert.Equal(0, ended);

        if (outcome == SprocketWire.Completed)
            task.Complete();
        else if (outcome == SprocketWire.Failed)
            task.Fail(new SprocketFailure("lost_grip", "lost its grip on the ground"));
        else
            task.Cancel();
        task.Complete();
        Assert.Equal(1, ended);

        task.Ended(() => ended++);
        Assert.Equal(2, ended);
    }

    /// <summary>Once Sprocket stops a task, it ends as cancelled however the mod ends it, so a stop that raises the mod's own events first ends it the same.</summary>
    [Fact]
    public void AStoppedTaskEndsCancelledWhateverEndsIt()
    {
        var task = new SprocketTask();
        string? heard = null;
        ((Action<Action<string, string, string>>)task.Wire()[SprocketWire.Hear])((outcome, _, _) => heard = outcome);
        task.Cancelling += _ =>
        {
            task.Conflicted(new SprocketFailure("switched_back", "was switched back"));
            task.Cancel();
        };

        ((Action<bool, string>)task.Wire()[SprocketWire.Cancel])(true, "Stopped");

        Assert.Equal(SprocketWire.Cancelled, task.Outcome);
        Assert.Equal(SprocketWire.Cancelled, heard);
    }

    /// <summary>A task completes, fails or is cancelled once, chains through Then, and stops through the part running.</summary>
    [Fact]
    public void ATaskEndsOnceAndChains()
    {
        var lost = new SprocketFailure("lost_grip", "lost its grip");
        var first = new SprocketTask();
        var second = new SprocketTask();
        var chain = first.Then(() => second);
        string? heard = null;
        ((Action<Action<string, string, string>>)chain.Wire()[SprocketWire.Hear])((outcome, id, _) => heard = outcome + ":" + id);

        first.Complete();
        Assert.False(chain.IsEnded);
        SprocketCancel? asked = null;
        second.Cancelling += cancel => asked = cancel;
        ((Action<bool, string>)chain.Wire()[SprocketWire.Cancel])(true, "Aborted");
        Assert.Equal(SprocketCancelCause.Aborted, asked!.Cause);

        // Stopped, it ends as cancelled, once, whatever the mod tells it after.
        second.Fail(lost);
        second.Complete();
        Assert.Equal(SprocketWire.Cancelled + ":", heard);

        // Not stopped, it ends as the first thing it is told.
        var alone = new SprocketTask();
        alone.Fail(lost);
        alone.Complete();
        Assert.Equal(SprocketWire.Failed, alone.Outcome);
        Assert.Equal("lost_grip", alone.Failure.Id);
    }

    /// <summary>The test mod's paces, a type of its own.</summary>
    public enum Pace
    {
        /// <summary>Slowly.</summary>
        Gentle,

        /// <summary>Quickly.</summary>
        Brisk
    }

    /// <summary>The test mod registered into <paramref name="members" /> through the game's mod messages, Sprocket loading first where <paramref name="sprocketFirst" /> says so.</summary>
    SprocketRegistration Registered(ModMembers members, bool sprocketFirst, Action<SprocketModBuilder> build, string name = "Glow", string space = Space,
        Func<IMyTerminalBlock, SprocketUnit?>? unitOf = null, Rota? rota = null)
    {
        HostNamespacesOnce();
        var was = MyAPIGateway.Utilities;
        MyAPIGateway.Utilities = Relay();
        try
        {
            var host = new ModSupportHost(members, unitOf ?? (_ => null), rota ?? new Rota());
            if (sprocketFirst)
                host.Start();
            var registration = (SprocketRegistration)SprocketModSupport.Register(new SprocketMod { Namespace = space, Name = name, Author = "Tester" }, build);
            if (!sprocketFirst)
                host.Start();
            return registration;
        }
        finally
        {
            MyAPIGateway.Utilities = was;
        }
    }

    /// <summary>What Sprocket answers to <paramref name="said" />, a registration sent straight on the channel as a mod without the wrapper would, taken into <paramref name="members" />.</summary>
    Dictionary<string, object> RegisteredRaw(ModMembers members, Dictionary<string, object> said)
    {
        HostNamespacesOnce();
        var was = MyAPIGateway.Utilities;
        MyAPIGateway.Utilities = Relay();
        try
        {
            Dictionary<string, object>? heard = null;
            MyAPIGateway.Utilities.RegisterMessageHandler(SprocketWire.Channel, message => heard ??= message as Dictionary<string, object>);
            new ModSupportHost(members, _ => null, new Rota()).Start();
            var register = (Func<Dictionary<string, object>, Dictionary<string, object>>)heard![SprocketWire.Register];
            return register(said);
        }
        finally
        {
            MyAPIGateway.Utilities = was;
        }
    }

    bool _cleared;

    /// <summary>Clears the host's namespaces once per test, since they are one list for the process.</summary>
    void HostNamespacesOnce()
    {
        if (_cleared)
            return;

        HostNamespaces.Clear();
        _cleared = true;
    }

    /// <summary>The game's mod messages between the mods of one world: every handler on a channel hears what is sent on it.</summary>
    IMyUtilities Relay()
    {
        return Pretend.To<IMyUtilities>()
            .Does("RegisterMessageHandler", arguments =>
            {
                var channel = (long)arguments[0];
                if (!_handlers.TryGetValue(channel, out var on))
                    _handlers[channel] = on = new List<Action<object>>();
                on.Add((Action<object>)arguments[1]);
            })
            .Does("UnregisterMessageHandler", arguments =>
            {
                if (_handlers.TryGetValue((long)arguments[0], out var on))
                    on.Remove((Action<object>)arguments[1]);
            })
            .Does("SendModMessage", arguments =>
            {
                if (_handlers.TryGetValue((long)arguments[0], out var on))
                {
                    foreach (var handler in on.ToList())
                        handler(arguments[1]);
                }
            })
            .Done();
    }

    /// <summary>The block type of <paramref name="light" />, which the test's members are registered for.</summary>
    static string Type(Lamp light) => light.Block.BlockDefinition.TypeIdString;

    /// <summary>The subtype of <paramref name="light" />.</summary>
    static string Subtype(Lamp light) => light.Block.BlockDefinition.SubtypeName;

    /// <summary>Switches the system on and presses its button, ticking the harness on after each.</summary>
    static void Pressed(Simulation sim)
    {
        sim.Editor.SwitchOn();
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Press("Airlock Button");
        for (var i = 0; i < 5; i++)
            sim.Tick();
    }
}
