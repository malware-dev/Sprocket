using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Values;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block can be told to do whatever it says it can do, without Sprocket being taught it.
/// </summary>
/// <remarks>
/// Sprocket knows three families of block by hand, and a ship is made of hundreds. Everything else
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
        screen.Editor.NewButtonSystem("Airlock");
        return screen.Run(2);
    }

    [Fact]
    public void ABlockOffersTheActionsItDeclares()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
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
    /// And the switch every block has is still said in Sprocket's own words, above them.
    /// </summary>
    /// <remarks>
    /// The floor does not displace what was already better. "Switch on" is a sentence; "Turn block
    /// On" is a button in somebody else's menu.
    /// </remarks>
    [Fact]
    public void WhatWasSaidBetterIsStillSaidThatWay()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.True(screen.Shows("Switch on"), "a block with a switch cannot be switched on");
        Assert.False(screen.Shows("Turn block On"), "the game's own on/off is offered twice");
    }

    /// <summary>
    /// What an extension stands in for is in the terminal view, under the name the block gives it.
    /// </summary>
    /// <remarks>
    /// Replaced is not gone: a use nobody anticipated is still a use. The two views are disjoint, so
    /// what one offers in Sprocket's words the other offers in the block's.
    /// </remarks>
    [Fact]
    public void TheTerminalViewHoldsWhatTheOrganizedOneReplaced()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.False(screen.Shows("Turn block On"), "what switch on stands in for is showing");

        Terminal(screen).Command!.Execute();
        screen.Run(2);

        Assert.True(screen.Shows("Turn block On"), "the block's own switch is not in its own view");
        Assert.False(screen.Shows("Switch on"), "Sprocket's wording is in the block's own view");
        Assert.True(What(screen).ViewShown == 1);

        Organized(screen).Command!.Execute();
        screen.Run(2);

        Assert.False(screen.Shows("Turn block On"), "going back did not put it away again");
        Assert.True(screen.Shows("Switch on"));
    }

    /// <summary>
    /// A block's name is left where the game keeps it, and out of what a sequence can say.
    /// </summary>
    /// <remarks>
    /// A step names the block it is about, so a step that renamed one would break itself and every
    /// other step naming it. The game still offers it, in the block's own view, where a person
    /// renaming a block is doing it deliberately.
    /// </remarks>
    [Fact]
    public void ABlocksNameIsNotSomethingASequenceSets()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.False(screen.Shows("Name"), "a sequence is offered the block's own name");

        Terminal(screen).Command!.Execute();
        screen.Run(2);

        Assert.True(screen.Shows("Name"), "the block's own view has lost what the block carries");
    }

    /// <summary>
    /// Both views are offered, and the organized one is what a picker opens on.
    /// </summary>
    /// <remarks>
    /// Every functional block has an organizer whether or not anything was written for its kind:
    /// switch on and switch off are Sprocket's words for what the game calls Toggle block On/Off.
    /// </remarks>
    [Fact]
    public void APickerOpensOnTheOrganizedView()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.NotNull(Terminal(screen).Command);
        Assert.True(What(screen).ViewShown == 0, "the picker did not open on Sprocket's own words");
        Assert.True(screen.Shows("Switch on"));
    }

    [Fact]
    public void TheNextPickerOpensTheWayTheLastWasLeft()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);
        Terminal(screen).Command!.Execute();
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.True(What(screen).ViewShown == 1, "the picker forgot which view was being shown");
        Assert.True(screen.Shows("Turn block On"));
    }

    [Fact]
    public void ASelectionThatDiffersSaysTheListIsWhatItShares()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.TickBlock("Airlock Inner Door");
        screen.Editor.TickBlock("Airlock Outer Door");
        screen.Run(2);

        const string shares = "Showing only what the picked blocks share";
        Assert.DoesNotContain(shares, What(screen).Note);

        screen.Editor.TickBlock("Hydrogen Tank");
        screen.Run(2);

        // Beside what it says about how far the blocks have been checked, so looked for within it.
        Assert.Contains(shares, What(screen).Note);
    }

    [Fact]
    public void WhatAStepWaitsForIsMarked()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);

        var entries = What(screen).Entries();
        Assert.True(entries.Single(entry => entry.Text == "Open").Waits, "opening a door is not marked");
        Assert.False(entries.Single(entry => entry.Text == "Switch on").Waits, "switching on is marked, and holds nothing");
    }

    /// <summary>A step's member is marked in its sentence as it is in the picker: what the step waits for, and nothing else.</summary>
    [Fact]
    public void WhatAStepWaitsForIsMarkedInItsSentence()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("Open");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("Switch on");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var rows = screen.Editor.StepRows();
        var opens = rows[0].Within().OfType<MemberName>().Single();
        var switches = rows[1].Within().OfType<MemberName>().Single();

        Assert.True(opens.Waits, "opening a door is not marked");
        Assert.False(switches.Waits, "switching on is marked, and holds nothing");
    }

    /// <summary>The tab answers where it is drawn, which nothing else on this screen proves.</summary>
    [Fact]
    public void TheTabIsPressedWhereItIsDrawn()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter 60 163 200 384 560 340 1394 72 "
            + "70 175 200 509").Split(' '));

        StageColumn what = screen.Editor.Pick().Stages.Last();
        Assert.NotNull(what.Views[1].Command);

        screen.Do("820", "247");
        Assert.True(screen.Editor.ShowsTerminal, "pressing the tab did not change the view");
        Assert.True(screen.Shows("Toggle block On/Off"), "the block's own words are not listed");
    }

    /// <summary>What the first value in the column of values holds.</summary>
    static string Given(Screen screen) => screen.Editor.Pick().Values!.Form.Fields[0].Text;

    static StageColumn What(Screen screen)
    {
        return screen.Editor.Pick().Stages.Last();
    }

    static Button Organized(Screen screen)
    {
        return What(screen).Views[0];
    }

    static Button Terminal(Screen screen)
    {
        return What(screen).Views[1];
    }

    /// <summary>Turns the picker to the block's own view, where the game's actions are listed as they are.</summary>
    /// <remarks>
    /// Where a light's colour setter is offered: the organized view offers the colour itself, and a
    /// setter beside it would say the same thing twice.
    /// </remarks>
    static void ShowTerminal(SprocketEditor editor)
    {
        editor.Pick().Stages.Single(s => s.Views.Count > 0).PressView("Terminal");
    }

    /// <summary>
    /// The tank declares a toggle of its own, and nothing has said either half, so all three stand.
    /// </summary>
    /// <remarks>
    /// Only an extension hides anything, by standing in for the game's own form of what it does.
    /// Where nothing stands in, what the block offers is what the block offers.
    /// </remarks>
    [Fact]
    public void AToggleNothingReplacesIsLeftAlone()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
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
    /// Sprocket mentions lights - the count comes off the action's own parameter list, so a modded
    /// block nobody anticipated behaves the same way.
    /// </remarks>
    [Fact]
    public void AnActionThatNeedsValuesAsksForThem()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Light");
        ShowTerminal(screen.Editor);
        screen.Run(2);

        Assert.True(screen.Shows("Set Color"), "the light does not offer its colour setter");

        screen.Editor.PressChoice("Set Color");
        screen.Run(2);

        // Asked on the button that finishes the picking, rather than on a screen of its own.
        Assert.Equal("This needs a color.", screen.Editor.Picking().Question);

        // One field over all three, because somebody described these three as one colour - which for
        // a light is Sprocket itself, since these are the game's own blocks. The action still says
        // nothing about them, and a block nobody has described still gets a box for each.
        Field asked = Assert.Single(screen.Editor.Picking().Fields);
        Assert.Equal(Editor.Colour, asked.Edits);
        Assert.Equal(3, asked.Covers);

        screen.Editor.Typed("255 136 0");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("SetColor", act.ActionId);
        Assert.Equal(3, act.Values.Count);
        Assert.Equal(255, act.Values[0].Whole);
        Assert.Equal(136, act.Values[1].Whole);
        Assert.Equal(0, act.Values[2].Whole);
    }

    /// <summary>
    /// And the step shows what it was given as a colour, not as the numbers behind it.
    /// </summary>
    /// <remarks>
    /// The one slot in a sentence that is not words. "255 136 0" is three numbers somebody has to
    /// imagine, and the step is read far more often than it is written, so what it shows is the
    /// colour itself.
    /// </remarks>
    [Fact]
    public void TheStepShowsWhatItWasGiven()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task…");
        screen.Editor.PressBlock("Airlock Light");
        ShowTerminal(screen.Editor);
        screen.Editor.PressChoice("Set Color");
        screen.Editor.Typed("255 136 0");
        screen.Run(2);

        StepRow step = screen.Editor.StepRows()[0];
        ValuesSlot given = step.Slots.OfType<ValuesSlot>().Single();

        Assert.Equal(new Color(255, 136, 0), given.Sample);
        Assert.False(screen.Shows("255 136 0"), "the step still spells the colour out");

        // One slot for all of them, in the order the action wanted them. Three chips with no names
        // on them would be three controls somebody cannot tell apart.
        Assert.True(screen.Shows("Set Color"), "the step does not say what it does");
    }

    /// <summary>
    /// And the values reach the block, in the order the action asked for them.
    /// </summary>
    /// <remarks>
    /// The whole of the compatibility case in one test: a light's colour setter, chosen from what
    /// the light declares, given three values by somebody typing them, saved, and performed by the
    /// game when a button is pressed. Nothing in Sprocket mentions lights.
    /// </remarks>
    [Fact]
    public void TheValuesReachTheBlock()
    {
        var rig = Simulation.On(Store.Nowhere());

        rig.Editor.NewButtonSystem("Lights");

        rig.Editor.PressAdd("task…");
        rig.Editor.PressBlock("Airlock Light");
        ShowTerminal(rig.Editor);
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

    /// <summary>And doing it reaches the block, performed by the game rather than by Sprocket.</summary>
    [Fact]
    public void DoingItReachesTheBlock()
    {
        var rig = Simulation.On(Store.Nowhere());

        rig.Editor.NewButtonSystem("Airlock");

        rig.Editor.PressAdd("task…");
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

    /// <summary>
    /// A value that takes one of two words is switched rather than typed.
    /// </summary>
    /// <remarks>
    /// The one field a form can be certain about, and so the one where typing can be wrong with
    /// nothing to say so: a number outside its bounds is refused out loud, while a word the block
    /// does not know is written into the program and declined in silence when it runs.
    /// </remarks>
    [Fact]
    public void AValueOfTwoWordsIsSwitchedRatherThanTyped()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter 60 163 200 384 560 340 1394 72 "
            + "70 175 150 203 type Approach 200 360 800 565").Split(' '));

        Assert.True(screen.Shows("Chime On Approach"), "the sensor's chime does not ask for anything");
        Assert.Equal(Editor.Chosen, screen.Editor.Picking().Fields.Single().Edits);
        Assert.Equal(2, screen.Editor.Picking().Fields.Single().Words.Count);

        // A switch says the word it is on, and pressing it gives the other one.
        var word = screen.Text("off");
        var middle = Screen.Ui(word.At.X, screen.MiddleOf(word));
        var at = new[] { ((int)middle.X).ToString(), ((int)middle.Y).ToString() };

        screen.Do(at);
        Assert.Equal("on", Given(screen));

        screen.Do(at);
        Assert.Equal("off", Given(screen));
    }

    /// <summary>
    /// One of the player's own colours is chosen by pressing it, and the hue slider by pressing it.
    /// </summary>
    /// <remarks>
    /// The two halves of the picker that are not typing. The colour that comes out is written the way
    /// the language writes one, because the box is what the answer is read from - so what a press
    /// does and what somebody could have typed are the same thing.
    /// </remarks>
    [Fact]
    public void AColourIsChosenByPressingOne()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter 60 163 200 384 560 340 1394 72 "
            + "70 175 200 485 800 565").Split(' '));


        // The third of the fourteen. They are the harness's stand-in for a player who has painted.
        screen.Do("1064", "198");
        Assert.Equal("#386285", Given(screen));

        // And the far left of the hue track is red, at whatever saturation and value were already
        // chosen: one slider owns one axis and leaves the other two where they stood.
        screen.Do("1000", "265");
        Assert.Equal("#853838", Given(screen));
    }

    /// <summary>
    /// A value of more than two choices is picked from them, and the program holds what the block
    /// is told rather than what the screen said.
    /// </summary>
    /// <remarks>
    /// The two are not the same thing and that is the whole point: a battery's charge mode reads
    /// Auto, Recharge or Discharge on the terminal and the block is told nought, one or two. What
    /// somebody chose has to survive being written down and shown again, both ways round.
    /// </remarks>
    [Fact]
    public void AValueOfSeveralChoicesIsPickedFromThem()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter 60 163 200 384 560 340 1394 72 "
            + "70 175 150 203 type Standby 200 360 800 397").Split(' '));

        Assert.True(screen.Shows("Auto") && screen.Shows("Recharge") && screen.Shows("Discharge"),
            "the column does not show what the value may be");

        screen.Do("1247", "201");
        Assert.Equal("Recharge", Given(screen));

        screen.Do("enter");

        // What the block is told, which is the number the game keeps its charge modes as.
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Literal held = Assert.Single(act.Values);
        Assert.Equal(1, held.Whole);

        // And what a person reads, which is neither the number nor anywhere in the program.
        Assert.True(screen.Shows("Recharge"), "the step does not say what was chosen");
        Assert.False(screen.Shows("1 "), "the step says the number instead of the word");
    }

    /// <summary>
    /// More choices than a row can hold fold into a box that opens a list.
    /// </summary>
    /// <remarks>
    /// Three short words are a row somebody takes in at a glance; six are a wall to search, and long
    /// ones are wider than the form whatever their number. The box is one line either way, which is
    /// what lets a block carrying several of these stay a form rather than a page.
    /// </remarks>
    [Fact]
    public void MoreChoicesThanFitFoldIntoAList()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter 60 163 200 384 560 340 1394 72 "
            + "70 175 150 203 type Tug 200 360 800 565").Split(' '));

        Assert.True(screen.Shows("Forward Direction"), "the forward direction does not ask for anything");

        // Closed, so none of the six is on the screen and neither is a choice nobody has made.
        Assert.False(screen.Shows("Backward"), "the list is showing while it is shut");

        screen.Do("1316", "201");
        Assert.True(screen.Shows("Backward") && screen.Shows("Down"),
            "pressing the box did not open the list");

        screen.Do("1316", "319");
        Assert.Equal("Left", Given(screen));
        Assert.False(screen.Shows("Down"), "choosing did not put the list away");

        screen.Do("enter");

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(2, Assert.Single(act.Values).Whole);
        Assert.True(screen.Shows("Left"), "the step does not say what was chosen");
    }
}
