using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block can be told what one of its own values should be, not only what to do.
/// </summary>
/// <remarks>
/// Actions are the small hand-picked half of what a block can do, and the half that describes itself
/// worst: an action declares how many values it takes and nothing trustworthy about them. The values
/// a block carries describe themselves completely - what they are called, in the player's own
/// language, what kind of thing they hold, and what they will accept - so a light's falloff can be
/// set without one word being written about lights anywhere.
/// </remarks>
public class SettingTests
{
    [Fact]
    public void ABlockOffersTheValuesItCarries()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        // The game hands out an action for a light's radius and none at all for its falloff, which
        // is the arbitrariness this exists to end.
        Assert.True(screen.Shows("falloff"), "the light does not offer its falloff");
        Assert.True(screen.Shows("color"), "the light does not offer its color");

        // Said as setting them, since values and actions are listed together, and under a heading
        // saying what they are about.
        Assert.True(screen.Shows("APPEARANCE"), "a light's color is not grouped with how it looks");

        // And the block's own division is what the terminal view groups by instead.
        What(screen).Views[1].Command!.Execute();
        screen.Run(2);

        Assert.True(screen.Shows("VALUES"), "the values are not grouped");
        Assert.True(screen.Shows("TOOLBAR ACTIONS"), "the actions are not grouped");
    }

    /// <summary>And what it will accept comes from the block, with nobody authoring it.</summary>
    [Fact]
    public void AValueSaysWhatItTakes()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("falloff");
        screen.Run(2);

        // The numbers this block's own definition declares, and nobody else's: a corner light's
        // falloff stops at two where another light's stops somewhere else.
        Field one = Assert.Single(screen.Editor.Picking().Fields);
        Assert.Equal("falloff (0 - 2)", one.Label(new Texts()));
        Assert.Equal("Falloff must be between 0 and 2.", one.Refuses(new Texts(), "Falloff", "5"));
    }

    /// <summary>And the step holds the value the way an action's values are held.</summary>
    [Fact]
    public void TheStepHoldsWhatItWasGiven()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("falloff");
        screen.Editor.Typed("2");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("Falloff", act.ActionId);
        Assert.True(act.Sets, "a value was picked and the step does not say it sets one");
        Assert.Equal(2, Assert.Single(act.Arguments).Integer);
    }

    /// <summary>And running it tells the block, which is the whole point of the exercise.</summary>
    [Fact]
    public void RunningItTellsTheBlock()
    {
        var rig = new Rig();
        Panel button = rig.Panel(1, "Button");
        // Declared here rather than taken from the standard rig, so what the block carries is
        // in front of whoever reads the test.
        Fitting light = rig.Light(11, "Light")
            .Carrying("Falloff", "Falloff", ValueKind.Real, "0", "3");

        Simulation sim = Simulation.On(rig);
        Automation program = sim.Editor.Program;

        AutomationSystem system = program.AddSystem();
        system.Name = "Lighting";

        var pressed = Pressed.On(program, system, Named(sim, button));

        Sequence dim = program.AddSequence(system, pressed);
        dim.Name = "Dim";

        var set = new Act("Falloff");
        set.Form = ReachKind.Value;
        set.Targets.Add(Named(sim, light));
        set.Arguments.Add(Literal.Of(2L));
        dim.Steps.Add(set);

        for (var i = 0; i < 5; i++)
            sim.Tick();

        rig.Forget();

        sim.Press("Button");
        sim.Tick();

        Assert.Equal(new[] { "Light:Falloff 2" }, rig.Log);
    }

    /// <summary>
    /// Setting a switch writes it, and never flips the block with the toggle of the same name.
    /// </summary>
    /// <remarks>
    /// A switch's value and the game's bare toggle for it are both called <c>OnOff</c>. Setting it to
    /// off has to leave a block that is already off exactly as it was.
    /// </remarks>
    [Fact]
    public void SettingASwitchWritesItRatherThanFlippingIt()
    {
        var rig = new Rig();
        Panel button = rig.Panel(1, "Button");
        Fitting light = rig.Light(11, "Light")
            .Carrying("OnOff", "On", ValueKind.Flag);
        light.Enabled = false;

        Simulation sim = Simulation.On(rig);
        Automation program = sim.Editor.Program;

        AutomationSystem system = program.AddSystem();
        system.Name = "Lighting";

        var pressed = Pressed.On(program, system, Named(sim, button));

        Sequence dark = program.AddSequence(system, pressed);
        dark.Name = "Dark";

        var off = new Act("OnOff");
        off.Form = ReachKind.Value;
        off.Targets.Add(Named(sim, light));
        off.Arguments.Add(Literal.Of(false));
        dark.Steps.Add(off);

        for (var i = 0; i < 5; i++)
            sim.Tick();

        rig.Forget();

        sim.Press("Button");
        sim.Tick();

        Assert.Equal(new[] { "Light:OnOff off" }, rig.Log);
        Assert.False(light.Enabled, "setting the switch to off flipped the light on");
    }

    /// <summary>Choosing something else for a step keeps what was given for the new choice.</summary>
    /// <remarks>
    /// A step changed from one value to another is given its new value at the end of the choosing.
    /// Keeping the old one instead would set the new value to a number meant for something else.
    /// </remarks>
    [Fact]
    public void ChangingWhatAStepSetsTakesItsNewValue()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("falloff");
        screen.Editor.Typed("2");
        screen.Run(2);

        screen.Editor.PressStepSlot(0, 0);
        screen.Editor.PressChoice("radius");
        screen.Editor.Typed("4");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("Radius", act.ActionId);
        Assert.Equal(4, Assert.Single(act.Arguments).Integer);
    }

    static string Named(Simulation sim, Fitting fitting)
    {
        GridEntity entity = sim.World.FindEntity(fitting.Id);
        return sim.Editor.Program.Declare(sim.World, entity).Name;
    }

    /// <summary>A value of Sprocket's own is offered as something to set where it can be set, and not where it is only read.</summary>
    [Fact]
    public void AnOwnValueIsOfferedToSetOnlyWhereItCanBe()
    {
        var screen = WithASequence();
        var door = screen.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door");
        var speed = 0;
        door.Kind.WithValue("speed", "speed",
                OwnValue.OneOf<Sandbox.ModAPI.IMyTerminalBlock>(new ChoiceType(BlockKinds.Own, "speed", "low", "middle", "high"), _ => speed)
                    .Writing<Sandbox.ModAPI.IMyTerminalBlock>((_, given) => speed = (int)given.Integer))
            .Looked("A test value.");

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);

        Assert.True(screen.Editor.HasChoice("speed"), "a value of Sprocket's own that can be set is not offered");
        Assert.False(screen.Editor.HasChoice("status"), "a value of Sprocket's own that is only read is offered as a step");

        screen.Editor.PressChoice("speed");
        screen.Run(2);
        screen.Editor.Current!.View.Within().OfType<WordsField>().Single().Within().OfType<Lumel.Controls.Button>().Single(word => word.Text == "high").Command!.Execute();
        screen.Editor.PressPrimary();
        screen.Run(2);

        var set = screen.Editor.Program.Systems[0].Sequences[0].Steps.OfType<Act>().Single(step => step.ActionId == "speed");
        Assert.Equal(ReachKind.Value, set.Form);
        Assert.Equal(BlockKinds.Own, set.Provider);
        Assert.Equal(Literal.Name("high"), Assert.Single(set.Arguments));
    }

    static Screen WithASequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        return screen.Run(2);
    }

    /// <summary>
    /// A block says which of two things the game meant by one word.
    /// </summary>
    /// <remarks>
    /// A light's radius is how far it shines and a beacon's is how far it is heard, and the game
    /// calls both of them Radius. The table that reads ids knows nothing about blocks, so the block
    /// is what settles it.
    /// </remarks>
    [Fact]
    public void ABlockSaysWhatItMeantByAWordItShares()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Warning Beacon");
        screen.Run(2);

        Assert.True(screen.Shows("broadcast radius"), "the beacon does not offer its radius");
        Assert.True(screen.Shows("SIGNALS"), "a beacon's radius is not grouped with what it says");
        Assert.False(screen.Shows("APPEARANCE"), "a beacon's radius is grouped as how it looks");
    }

    /// <summary>A member nothing vouches for is left to the terminal view, and the organized view keeps Other for the custom data alone.</summary>
    [Fact]
    public void WhatNothingKnowsAboutIsLeftToTheTerminalView()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Approach Sensor");
        screen.Run(2);

        Assert.False(screen.Shows("Chime On Approach"), "a member nothing vouches for is offered");
        var entries = What(screen).Entries().ToList();
        var other = entries.FindIndex(entry => entry.IsHeading && entry.Text == "Other");
        Assert.Equal(new[] { "custom data" }, entries.Skip(other + 1).TakeWhile(entry => !entry.IsHeading).Select(entry => entry.Text));

        What(screen).PressView("Terminal");
        screen.Run(2);
        Assert.True(screen.Shows("Chime On Approach"), "the terminal view does not list what the block declares");
    }

    static StageColumn What(Screen screen)
    {
        return screen.Editor.Pick().Stages.Last();
    }
}
