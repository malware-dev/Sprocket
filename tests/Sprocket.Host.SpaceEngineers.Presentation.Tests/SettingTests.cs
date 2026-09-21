using Sprocket.Host.SpaceEngineers.Layout;
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
        Assert.True(screen.Shows("Falloff"), "the light does not offer its falloff");
        Assert.True(screen.Shows("Color"), "the light does not offer its colour");

        // Under a heading that says what they are, which is why they do not each say "set".
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
        screen.Editor.PressChoice("Falloff");
        screen.Run(2);

        Assert.True(screen.Editor.Screen.PrimaryAsks, "finishing does not ask for the value");
        Assert.Equal("This needs one value.", screen.Editor.Screen.PrimaryQuestion);

        // The numbers this block's own definition declares, and nobody else's: a corner light's
        // falloff stops at two where another light's stops somewhere else.
        Field one = Assert.Single(screen.Editor.Screen.PrimaryFields);
        Assert.Equal("Falloff 0-2", one.Label);
        Assert.Equal("Falloff has to be between 0 and 2.", one.Refuses("Falloff", "5"));
    }

    /// <summary>And the step holds the value the way an action's values are held.</summary>
    [Fact]
    public void TheStepHoldsWhatItWasGiven()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("Falloff");
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
        set.Sets = true;
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
        off.Sets = true;
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

    static string Named(Simulation sim, Fitting fitting)
    {
        GridEntity entity = sim.World.FindEntity(fitting.Id);
        return sim.Editor.Program.Declare(sim.World, entity).Name;
    }

    static Screen WithASequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Run a new sequence");
        return screen.Run(2);
    }
}
