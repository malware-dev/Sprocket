using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Sim.Machine;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block can be told to do whatever it says it can do, without Reactor being taught it.
/// </summary>
/// <remarks>
/// Reactor knows three families of block by hand, and a ship is made of hundreds. Everything else
/// had a switch and nothing more: a piston could not be extended, a connector could not be
/// connected, a tank could not stockpile. No number of hand-written extensions closes that, because
/// mods keep arriving and nobody can write one for a block that does not exist yet.
/// <para>
/// So the block is asked. What an extension is for is what a block <em>is</em> - its states and its
/// moments, which no action can report - and saying an act better than the terminal does.
/// </para>
/// </remarks>
public class ActionTests
{
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

    [Fact]
    public void ABlockOffersTheActionsItDeclares()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        // The game's own words, which are the ones the player has already read in the terminal.
        Assert.True(screen.Shows("Stockpile On"), "the tank offers nothing it declares");

        screen.Editor.PressChoice("Stockpile On");
        screen.Editor.PressPrimary();
        screen.Run(2);

        // What the program holds is the game's own name for it, which is what a slot holds too and
        // what the block will be asked for when the step runs.
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("Stockpile_On", act.ActionId);
        Assert.True(screen.Shows("Stockpile On"), "the step does not say what it does");
    }

    /// <summary>
    /// And the switch every block has is still said in Reactor's own words, above them.
    /// </summary>
    /// <remarks>
    /// The floor does not displace what was already better. "Switch on" is a sentence; "Turn block
    /// On" is a button in somebody else's menu.
    /// </remarks>
    [Fact]
    public void WhatWasSaidBetterIsStillSaidThatWay()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.True(screen.Shows("switch on"), "a block with a switch cannot be switched on");
        Assert.False(screen.Shows("Turn block On"), "the game's own on/off is offered twice");
    }

    /// <summary>
    /// A toggle whose two halves are both said already is not offered a third time.
    /// </summary>
    /// <remarks>
    /// The game's convention is that a switch called X offers X_On, X_Off and a bare X that flips
    /// whichever way it is. "Open or close the door depending" is not a thing anybody writing a
    /// procedure means, and a door that offered it beside open and close would be asking somebody
    /// to work out which of three they wanted.
    /// </remarks>
    [Fact]
    public void TheToggleAPairReplacesIsNotOffered()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);

        Assert.True(screen.Shows("open"), "a door cannot be opened");
        Assert.True(screen.Shows("close"), "a door cannot be closed");
        Assert.False(screen.Shows("Open/Closed"), "the door offers the toggle as well as both halves");
    }

    /// <summary>
    /// The tank declares a toggle of its own, and nothing has said either half, so all three stand.
    /// </summary>
    /// <remarks>
    /// The rule is not "hide every toggle". It is that Reactor does not say the same thing twice,
    /// and where it has said nothing there is nothing to be redundant with - so what the block
    /// offers is what the block offers.
    /// </remarks>
    [Fact]
    public void AToggleNothingReplacesIsLeftAlone()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.True(screen.Shows("Stockpile On/Off"), "the tank's own toggle was hidden");
    }

    /// <summary>
    /// An action that has to be given values asks for them, and then does them.
    /// </summary>
    /// <remarks>
    /// The compatibility case, and a light's colour setter is the awkward one worth proving: three
    /// values, each declared as text, with nothing saying they are a colour's channels. Nothing in
    /// Reactor mentions lights - the count comes off the action's own parameter list, so a modded
    /// block nobody anticipated behaves the same way.
    /// </remarks>
    [Fact]
    public void AnActionThatNeedsValuesAsksForThem()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.True(screen.Shows("Set Color"), "the light does not offer its colour setter");

        screen.Editor.PressChoice("Set Color");
        screen.Run(2);

        // Asked on the button that finishes the picking, rather than on a screen of its own.
        Assert.True(screen.Editor.Screen.PrimaryAsks, "finishing does not ask for the values");
        Assert.Equal("This needs three values.", screen.Editor.Screen.PrimaryQuestion);

        // One field each. One box holding three things is a separator for somebody to get wrong.
        Assert.Equal(3, screen.Editor.Screen.PrimaryFields);

        screen.Editor.Typed("255 136 0");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("SetColor", act.ActionId);
        Assert.Equal(3, act.Values.Count);
        Assert.Equal(255, act.Values[0].Whole);
        Assert.Equal(136, act.Values[1].Whole);
        Assert.Equal(0, act.Values[2].Whole);
    }

    /// <summary>And the step says what it was given, in the spelling its program uses.</summary>
    [Fact]
    public void TheStepSaysWhatItWasGiven()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("Set Color");
        screen.Editor.Typed("255 136 0");
        screen.Run(2);

        Assert.True(screen.Shows("255 136 0"), "the step does not say what it was given");

        // One chip for all of them, in the order the action wanted them. Three chips with no names
        // on them would be three controls somebody cannot tell apart.
        Assert.True(screen.Shows("Set Color"), "the step does not say what it does");
    }

    /// <summary>
    /// And the values reach the block, in the order the action asked for them.
    /// </summary>
    /// <remarks>
    /// The whole of the compatibility case in one test: a light's colour setter, chosen from what
    /// the light declares, given three values by somebody typing them, saved, and performed by the
    /// game when a button is pressed. Nothing in Reactor mentions lights.
    /// </remarks>
    [Fact]
    public void TheValuesReachTheBlock()
    {
        var rig = Simulation.On(Store.Nowhere());

        rig.Editor.NewSystem("Lights");
        rig.Editor.PressStart("When…");
        rig.Editor.PressBlock("Airlock Button");
        rig.Editor.PressChoice("is pressed");
        rig.Editor.PressPrimary();
        rig.Editor.PressChoice("Run a new sequence");

        rig.Editor.PressAdd("do something");
        rig.Editor.PressBlock("Airlock Light");
        rig.Editor.PressChoice("Set Color");
        rig.Editor.Typed("255 136 0");
        rig.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        rig.Machine.Log.Clear();
        rig.Press("Airlock Button");

        for (var i = 0; i < 5; i++)
            rig.Tick();

        Assert.Contains("Airlock Light:SetColor 255 136 0", rig.Machine.Log);
    }

    /// <summary>And doing it reaches the block, performed by the game rather than by Reactor.</summary>
    [Fact]
    public void DoingItReachesTheBlock()
    {
        var rig = Simulation.On(Store.Nowhere());

        rig.Editor.NewSystem("Airlock");
        rig.Editor.PressStart("When…");
        rig.Editor.PressBlock("Airlock Button");
        rig.Editor.PressChoice("is pressed");
        rig.Editor.PressPrimary();
        rig.Editor.PressChoice("Run a new sequence");

        rig.Editor.PressAdd("do something");
        rig.Editor.PressBlock("Hydrogen Tank");
        rig.Editor.PressChoice("Stockpile On");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        rig.Machine.Log.Clear();
        rig.Press("Airlock Button");

        for (var i = 0; i < 5; i++)
            rig.Tick();

        Assert.Contains("Hydrogen Tank:Stockpile_On", rig.Machine.Log);
    }
}
