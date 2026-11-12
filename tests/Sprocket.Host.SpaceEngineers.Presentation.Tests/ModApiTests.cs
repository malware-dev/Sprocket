using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Mods;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.ModApi;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using Xunit;

// A registered member's functions read the state its test changes as the test goes, standing in for a mod's block.
// ReSharper disable AccessToModifiedClosure

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Another mod gives its blocks members in Sprocket through Sprocket's mod API, and players use them as Sprocket's own.</summary>
public class ModApiTests
{
    readonly Dictionary<long, List<Action<object>>> _handlers = new();

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

    /// <summary>A mod finds Sprocket whichever of the two loads first, and its registration reaches Sprocket's members.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AModFindsSprocketWhicheverLoadsFirst(bool sprocketFirst)
    {
        var was = MyAPIGateway.Utilities;
        MyAPIGateway.Utilities = Relay();
        try
        {
            var members = new ModMembers();
            var host = new ModApiHost(members);
            var readied = 0;
            var api = new SprocketApi(_ => readied++);

            if (sprocketFirst)
                host.Start();
            api.Load();
            if (!sprocketFirst)
                host.Start();

            Assert.True(api.IsReady, "the mod never heard Sprocket");
            Assert.Equal(SprocketApi.Version, api.SprocketVersion);
            Assert.True(readied >= 1);

            var rig = Rig.Demo();
            var light = rig.Light(40, "Glow Light");
            api.Register(ModMember.Value("test.glow", light.Block.BlockDefinition.TypeIdString, light.Block.BlockDefinition.SubtypeName)
                .Flag(_ => true).Named(() => "glowing", () => "Whether it glows."));
            Assert.True(members.Claims(light.Block), "the registration never reached Sprocket");
        }
        finally
        {
            MyAPIGateway.Utilities = was;
        }
    }

    /// <summary>A value a mod registers is offered on its blocks in the step's selector under its name, and a step sets it through the mod's own writer.</summary>
    [Fact]
    public void ARegisteredValueIsOfferedAndSetThroughTheMod()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var glowing = false;
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(ModMember.Value("test.glow", light.Block.BlockDefinition.TypeIdString, light.Block.BlockDefinition.SubtypeName)
            .Flag(_ => glowing, (_, on) => glowing = on)
            .Named(() => "glowing", () => "Whether it glows.")
            .InSprocketsGroup("appearance")
            .Said);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();

        var offered = sim.Editor.Members();
        Assert.Contains("glowing", offered);

        sim.Editor.PressChoice("glowing");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Press("Airlock Button");
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.True(glowing, "the step never reached the mod's writer");
    }

    /// <summary>An action a mod registers takes its options as a step gives them, in the mod's own types.</summary>
    [Fact]
    public void ARegisteredActionRunsWithItsOptions()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var height = double.NaN;
        var pace = -1;
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(Member(ModMember.Action("test.rise", Type(light), Subtype(light)), "rise to")
            .Taking(ModMember.Option(() => "height", ModMemberKeys.TypeNumber, "m", 1, 20),
                ModMember.Option(() => "pace", ModMemberKeys.TypeChoice, choices: new[] { "gentle", "brisk" }, choiceNames: () => new[] { "rolig", "rask" }))
            .Running((_, options) =>
            {
                height = (double)options[0];
                pace = (int)options[1];
            })
            .Said);

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
        Assert.Equal(1, pace);
        sim.Unit.Written(out var written, out _);
        Assert.Contains("brisk", written);
    }

    /// <summary>A step waiting for pending tasks waits on a mod's action until the mod says it is done.</summary>
    [Fact]
    public void AStepWaitsOnARegisteredActionUntilItIsDone()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var settled = false;
        var after = 0;
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(Member(ModMember.Action("test.settle", Type(light), Subtype(light)), "settle").Running(delegate { }).DoneWhen(_ => settled).Said);
        sim.Kinds.Mods.Register(Member(ModMember.Action("test.chime", Type(light), Subtype(light)), "chime").Running(delegate { after++; }).Said);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        Task(sim, "settle");
        sim.Editor.PressAdd(SentenceText.AwaitWords);
        Task(sim, "chime");
        Pressed(sim);

        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Equal(0, after);

        settled = true;
        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Equal(1, after);
    }

    /// <summary>A signal a mod registers, a change of one of its values, starts a sequence when the mod says the value has changed.</summary>
    [Fact]
    public void ARegisteredSignalStartsASequence()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var settled = false;
        var chimed = 0;
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(Member(ModMember.Value("test.settled", Type(light), Subtype(light)), "settled").Flag(_ => settled).Announced().Said);
        sim.Kinds.Mods.Register(Member(ModMember.Signal("test.settles", Type(light), Subtype(light)), "settles")
            .Becoming("test.settled", true, () => new[] { "is settled", "is up" }).Announced().Said);
        sim.Kinds.Mods.Register(Member(ModMember.Action("test.chime", Type(light), Subtype(light)), "chime").Running(delegate { chimed++; }).Said);

        sim.Editor.NewSystem("Glow");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("settles");
        sim.Editor.PressPrimary();
        sim.Tick();
        Task(sim, "chime");
        sim.Editor.SwitchOn();
        for (var i = 0; i < 5; i++)
            sim.Tick();
        Assert.Equal(0, chimed);

        settled = true;
        sim.Kinds.Mods.Changed(light.Block, "test.settled");
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Equal(1, chimed);
    }

    /// <summary>A choice a mod registers is said in the player's language and kept in the program by its id, so the program reads the same in any language.</summary>
    [Fact]
    public void ARegisteredChoiceIsKeptByItsId()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var mode = 0;
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(Member(ModMember.Value("test.mode", Type(light), Subtype(light)), "mode")
            .OneOf(new[] { "low", "high" }, () => new[] { "lav", "hoy" }, _ => mode, (_, chosen) => mode = chosen)
            .Said);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice("mode");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "hoy";
        sim.Editor.PressPrimary();
        Pressed(sim);

        Assert.Equal(1, mode);
        sim.Unit.Written(out var written, out _);
        Assert.Contains("high", written);
        Assert.DoesNotContain("hoy", written);
    }

    /// <summary>A member a mod stands in a group of its own is listed under the mod's heading, after Sprocket's groups and before Other, and shows the mod's description.</summary>
    [Fact]
    public void AModsOwnGroupIsListedUnderItsHeading()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(ModMember.Value("test.lift", Type(light), Subtype(light)).Flag(_ => true, delegate { })
            .Named(() => "lifted", () => "Whether the light floats.").InGroup(() => "Repulsion").Said);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();

        var listed = sim.Editor.MembersHeaded();
        var heading = listed.IndexOf("#Repulsion");
        Assert.True(heading >= 0, string.Join(", ", listed));
        Assert.Equal("lifted", listed[heading + 1]);
        var other = listed.FindIndex(said => said.StartsWith("#") && said != "#Repulsion" && listed.IndexOf(said) > heading);
        Assert.True(other < 0 || listed[other] == "#" + sim.Editor.Texts.CategoryOther, string.Join(", ", listed));

        sim.Editor.PressChoice("lifted");
        Assert.Equal("Whether the light floats.", sim.Editor.Pick().Arguments!.About.Markdown);
    }

    /// <summary>A mod's member is named again in the language the player changes to.</summary>
    [Fact]
    public void AModsMemberIsNamedAgainInANewLanguage()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var named = "glowing";
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(Member(ModMember.Value("test.glow", Type(light), Subtype(light)), "x").Flag(_ => true, delegate { })
            .Named(() => named, () => named + ".").Said);

        sim.Editor.NewButtonSystem("Glow");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();
        Assert.Contains("glowing", sim.Editor.Members());

        named = "lysende";
        sim.Kinds.Reword();
        sim.Unit.Reword();
        sim.Editor.Back();
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Tick();
        Assert.Contains("lysende", sim.Editor.Members());
    }

    /// <summary>A step on a mod's member, in a world loaded without that mod, is kept as it was written and marked as asking its blocks for what they do not offer.</summary>
    [Fact]
    public void AStepOnAMissingModsMemberIsKeptAndMarked()
    {
        var rig = Rig.Demo();
        var light = rig.Light(40, "Glow Light");
        var sim = Simulation.On(rig);
        sim.Kinds.Mods.Register(Member(ModMember.Value("test.glow", Type(light), Subtype(light)), "glowing").Flag(_ => false, delegate { }).Said);

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

        sim.Reload();
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Unit.Written(out var after, out _);
        Assert.Equal(Program(before), Program(after));
        var sequence = sim.Unit.Program.Systems[0].Sequences[0];
        var fault = SequenceSummary.Fault(sim.Runner.Get(sequence));
        Assert.Equal(SequenceProblem.Unoffered, fault.Problem);
    }

    /// <summary>What <paramref name="written" /> says from its first brace on, past the date a reload writes anew.</summary>
    static string Program(string written) => written.Substring(written.IndexOf("{", StringComparison.Ordinal));

    /// <summary>The block type of <paramref name="light" />, which the test's members are registered for.</summary>
    static string Type(Lamp light) => light.Block.BlockDefinition.TypeIdString;

    /// <summary>The subtype of <paramref name="light" />.</summary>
    static string Subtype(Lamp light) => light.Block.BlockDefinition.SubtypeName;

    /// <summary>The member <paramref name="member" /> named <paramref name="name" />, in the group of Sprocket's every test member stands in.</summary>
    static ModMember Member(ModMember member, string name) => member.Named(() => name, () => name + ".").InSprocketsGroup("behavior");

    /// <summary>A task of the light's mod action <paramref name="action" />, added to the sequence on the screen.</summary>
    static void Task(Simulation sim, string action)
    {
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Glow Light");
        sim.Editor.PressChoice(action);
        sim.Editor.PressPrimary();
    }

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
