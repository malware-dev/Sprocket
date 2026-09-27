using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
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

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        // The game hands out an action for a light's radius and none at all for its falloff, which
        // is the arbitrariness this exists to end.
        Assert.True(screen.Shows("Set Falloff"), "the light does not offer its falloff");
        Assert.True(screen.Shows("Set Color"), "the light does not offer its colour");

        // Said as setting them, since values and actions are listed together, and under a heading
        // saying what they are about.
        Assert.True(screen.Shows("APPEARANCE"), "a light's colour is not grouped with how it looks");

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

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("Set Falloff");
        screen.Run(2);

        Assert.True(screen.Editor.Screen.PrimaryAsks, "finishing does not ask for the value");
        Assert.Equal("This needs one value.", screen.Editor.Screen.PrimaryQuestion);

        // The numbers this block's own definition declares, and nobody else's: a corner light's
        // falloff stops at two where another light's stops somewhere else.
        Field one = Assert.Single(screen.Editor.Screen.PrimaryFields);
        Assert.Equal("Falloff (0 - 2)", one.Label);
        Assert.Equal("Falloff has to be between 0 and 2.", one.Refuses("Falloff", "5"));
    }

    /// <summary>And the step holds the value the way an action's values are held.</summary>
    [Fact]
    public void TheStepHoldsWhatItWasGiven()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("Set Falloff");
        screen.Editor.Typed("2");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("Falloff", act.ActionId);
        Assert.True(act.Sets, "a value was picked and the step does not say it sets one");
        Assert.Equal(2, Assert.Single(act.Values).Whole);
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

        var pressed = new RequiredTrigger("pressed_1");
        pressed.Sources.Add(Named(sim, button));

        Sequence dim = program.AddSequence(system, pressed);
        dim.Name = "Dim";

        var set = new Act("Falloff");
        set.Form = Form.Value;
        set.Targets.Add(Named(sim, light));
        set.Values.Add(Literal.Of(2L));
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

        var pressed = new RequiredTrigger("pressed_1");
        pressed.Sources.Add(Named(sim, button));

        Sequence dark = program.AddSequence(system, pressed);
        dark.Name = "Dark";

        var off = new Act("OnOff");
        off.Form = Form.Value;
        off.Targets.Add(Named(sim, light));
        off.Values.Add(Literal.Of(false));
        dark.Steps.Add(off);

        for (var i = 0; i < 5; i++)
            sim.Tick();

        rig.Forget();

        sim.Press("Button");
        sim.Tick();

        Assert.Equal(new[] { "Light:OnOff off" }, rig.Log);
        Assert.False(light.Enabled, "setting the switch to off flipped the light on");
    }

    /// <summary>
    /// A trigger that sets a value says it the way the step does, value and all.
    /// </summary>
    /// <remarks>
    /// Its line in the trigger list is put together apart from the step's, so it is where the two
    /// would drift: a trigger that set the charge mode once read as the value and the battery, and
    /// never said what it was set to.
    /// </remarks>
    [Fact]
    public void ATriggerThatSetsAValueSaysAllOfIt()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Power");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Standby Battery");
        screen.Editor.PressChoice("Set Charge Mode");
        screen.Editor.Typed("1");
        screen.Run(2);

        Assert.Equal("then set Charge Mode on Standby Battery to Recharge", screen.Editor.Doing(0));
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

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("Set Falloff");
        screen.Editor.Typed("2");
        screen.Run(2);

        screen.Editor.PressStepSlot(0, 0);
        screen.Editor.PressChoice("Set Radius");
        screen.Editor.Typed("4");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("Radius", act.ActionId);
        Assert.Equal(4, Assert.Single(act.Values).Whole);
    }

    static string Named(Simulation sim, Fitting fitting)
    {
        GridEntity entity = sim.World.FindEntity(fitting.Id);
        return sim.Editor.Program.Declare(sim.World, entity).Name;
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

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Warning Beacon");
        screen.Run(2);

        Assert.True(screen.Shows("Set Radius"), "the beacon does not offer its radius");
        Assert.True(screen.Shows("SIGNALS"), "a beacon's radius is not grouped with what it says");
        Assert.False(screen.Shows("APPEARANCE"), "a beacon's radius is grouped as how it looks");
    }

    /// <summary>
    /// What nothing has been said about is offered last, under Other.
    /// </summary>
    /// <remarks>
    /// Which is what every member of a modded block is until somebody writes a category for it. Last
    /// rather than left out: a member Sprocket cannot place is still a member the block can perform.
    /// </remarks>
    [Fact]
    public void WhatNothingKnowsAboutComesLast()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Approach Sensor");
        screen.Run(2);

        var entries = What(screen).Entries();
        var other = entries.ToList().FindIndex(entry => entry.IsHeading && entry.Text == "Other");
        var power = entries.ToList().FindIndex(entry => entry.IsHeading && entry.Text == "Power");

        Assert.True(other >= 0, "nothing is grouped under Other");
        Assert.True(power >= 0 && power < other, "Other is not last");
        Assert.True(screen.Shows("Set Chime On Approach"), "the unknown member is not offered at all");
    }

    static StageColumn What(Screen screen)
    {
        return screen.Editor.Pick().Stages.Last();
    }
}
