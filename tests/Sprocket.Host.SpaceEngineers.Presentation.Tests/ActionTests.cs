using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel.Controls;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block can be told to do whatever it says it can do, without Sprocket being taught it.
/// </summary>
/// <remarks>
/// Sprocket knows three families of block by hand, and a ship is made of hundreds. Everything else
/// had a switch and nothing more: a piston could not be extended, a connector could not be
/// connected, a tank could not stockpile. No number of hand-written capabilities closes that, because
/// mods keep arriving and nobody can write one for a block that does not exist yet.
/// <para>
/// So the block is asked. What a capability is for is what a block <em>is</em> - its states and its
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

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Editor.Pick().Stages.Single(s => s.Views.Count > 0).PressView("Terminal");
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
    /// And the switch every block has is still said in Sprocket's own words, above them, as one value.
    /// </summary>
    /// <remarks>
    /// The floor does not displace what was already better. "Power" is set off, on or flipped; "Turn block On" is a
    /// button in somebody else's menu.
    /// </remarks>
    [Fact]
    public void WhatWasSaidBetterIsStillSaidThatWay()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.True(ListsPower(screen), "a block with a switch cannot be switched on");
        Assert.False(screen.Shows("switch on"), "switching on is offered beside the switch's value");
        Assert.False(screen.Shows("turn block on"), "the game's own on/off is offered twice");
    }

    /// <summary>
    /// What a capability stands in for is in the terminal view, under the name the block gives it.
    /// </summary>
    /// <remarks>
    /// Replaced is not gone: a use nobody anticipated is still a use. The two views are disjoint, so
    /// what one offers in Sprocket's words the other offers in the block's.
    /// </remarks>
    [Fact]
    public void TheTerminalViewHoldsWhatTheOrganizedOneReplaced()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.False(screen.Shows("turn block on"), "what the switch stands in for is showing");

        Terminal(screen).Command!.Execute();
        screen.Run(2);

        Assert.True(screen.Shows("Turn block On"), "the block's own switch is not in its own view");
        Assert.False(ListsPower(screen), "Sprocket's wording is in the block's own view");
        Assert.True(What(screen).ViewShown == 1);

        Organized(screen).Command!.Execute();
        screen.Run(2);

        Assert.False(screen.Shows("turn block on"), "going back did not put it away again");
        Assert.True(ListsPower(screen));
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

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.False(screen.Shows("name"), "a sequence is offered the block's own name");

        Terminal(screen).Command!.Execute();
        screen.Run(2);

        Assert.True(screen.Shows("Name"), "the block's own view has lost what the block carries");
    }

    /// <summary>
    /// Both views are offered, and the organized one is what a picker opens on.
    /// </summary>
    /// <remarks>
    /// Every functional block has an organizer whether or not anything was written for its kind:
    /// power is Sprocket's word for what the game calls Toggle block On/Off.
    /// </remarks>
    [Fact]
    public void APickerOpensOnTheOrganizedView()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.NotNull(Terminal(screen).Command);
        Assert.True(What(screen).ViewShown == 0, "the picker did not open on Sprocket's own words");
        Assert.True(ListsPower(screen));
    }

    [Fact]
    public void TheNextPickerOpensTheWayTheLastWasLeft()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);
        Terminal(screen).Command!.Execute();
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Run(2);

        Assert.True(What(screen).ViewShown == 1, "the picker forgot which view was being shown");
        Assert.True(screen.Shows("Turn block On"));
    }

    [Fact]
    public void ASelectionThatDiffersSaysTheListIsWhatItShares()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
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

    /// <summary>What a step waits for is marked by the clock, and anything else a block is told to do by the bolt.</summary>
    [Fact]
    public void WhatAStepWaitsForIsMarked()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);

        var entries = What(screen).Entries();
        Assert.Equal(Symbols.Lasting, entries.Single(entry => entry.Text == "open").Mark);
        Assert.Equal(Symbols.Action, entries.Single(entry => entry.Text == "reverse").Mark);
    }

    /// <summary>A step's member is marked in its sentence as it is in the picker: the clock for what the step waits for, the bolt for the rest.</summary>
    [Fact]
    public void WhatAStepWaitsForIsMarkedInItsSentence()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("open");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("reverse");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var rows = screen.Editor.StepRows();
        // The first marked name is the member; the slot after it carries its own mark.
        var opens = rows[0].Within().OfType<MemberName>().First();
        var switches = rows[2].Within().OfType<MemberName>().First();

        Assert.Equal(Symbols.Lasting, opens.Mark);
        Assert.Equal(Symbols.Action, switches.Mark);
    }

    /// <summary>
    /// The link to the terminal's list answers where it is drawn, at the end of the column's heading, and the list it leads to
    /// carries its banner and the link back.
    /// </summary>
    [Fact]
    public void TheTerminalLinkIsPressedWhereItIsDrawn()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
            + "press step press task 200 509").Split(' '));

        StageColumn what = screen.Editor.Pick().Stages.Last();
        Assert.NotNull(what.Views[1].Command);
        Assert.False(screen.Shows("‹ Organized"), "the link back shows while the organized list does");

        var drawn = screen.Drawings.First(d => d.IsText && d.Text == "Terminal ›");
        var at = Screen.Ui(drawn.At.X + 8f, drawn.At.Y + 8f);
        screen.Do(at.X.ToString(System.Globalization.CultureInfo.InvariantCulture),
            at.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.True(screen.Editor.ShowsTerminal, "pressing the link did not change the view");
        Assert.True(screen.Shows("Toggle block On/Off"), "the block's own words are not listed");
        Assert.StartsWith("Everything these blocks offer in the terminal", what.Note);
        Assert.True(screen.Shows("‹ Organized"), "there is no way back from the terminal's list");
        Assert.False(screen.Shows("Terminal ›"), "the link to the list showing still shows");
    }

    /// <summary>What the first value in the column of values holds.</summary>
    static string Given(Screen screen) => screen.Editor.Pick().Arguments!.Form.Fields[0].Text;

    /// <summary>Whether the column of what to do lists the power value, apart from the heading of the same name.</summary>
    static bool ListsPower(Screen screen) => What(screen).Entries().Any(e => e.Text == "power" && !e.IsHeading);

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
    /// Where a light's color setter is offered: the organized view offers the color itself, and a
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
    /// Only a capability hides anything, by standing in for the game's own form of what it does.
    /// Where nothing stands in, what the block offers is what the block offers.
    /// </remarks>
    [Fact]
    public void AToggleNothingReplacesIsLeftAlone()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hydrogen Tank");
        screen.Editor.Pick().Stages.Single(s => s.Views.Count > 0).PressView("Terminal");
        screen.Run(2);

        Assert.True(screen.Shows("Stockpile On/Off"), "the tank's own toggle was hidden");
    }

    /// <summary>
    /// An action that has to be given values asks for them, and then does them.
    /// </summary>
    /// <remarks>
    /// The compatibility case, and a light's color setter is the awkward one worth proving: three
    /// values, each declared as text, with nothing saying they are a color's channels. Nothing in
    /// Sprocket mentions lights - the count comes off the action's own parameter list, so a modded
    /// block nobody anticipated behaves the same way.
    /// </remarks>
    [Fact]
    public void AnActionThatNeedsValuesAsksForThem()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        ShowTerminal(screen.Editor);
        screen.Run(2);

        Assert.True(screen.Shows("Set Color"), "the light does not offer its color setter");

        screen.Editor.PressChoice("Set Color");
        screen.Run(2);

        // One field over all three, because somebody described these three as one color - which for
        // a light is Sprocket itself, since these are the game's own blocks. The action still says
        // nothing about them, and a block nobody has described still gets a box for each.
        Field asked = Assert.Single(screen.Editor.Picking().Fields);
        Assert.Equal(FieldEditor.Color, asked.Edits);
        Assert.Equal(3, asked.Covers);

        screen.Editor.Typed("255 136 0");
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("SetColor", act.ActionId);
        Assert.Equal(3, act.Arguments.Count);
        Assert.Equal(255, act.Arguments[0].Whole);
        Assert.Equal(136, act.Arguments[1].Whole);
        Assert.Equal(0, act.Arguments[2].Whole);
    }

    /// <summary>
    /// And the step shows what it was given as a color, not as the numbers behind it.
    /// </summary>
    /// <remarks>
    /// The one slot in a sentence that is not words. "255 136 0" is three numbers somebody has to
    /// imagine, and the step is read far more often than it is written, so what it shows is the
    /// color itself.
    /// </remarks>
    [Fact]
    public void TheStepShowsWhatItWasGiven()
    {
        var screen = WithASequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        ShowTerminal(screen.Editor);
        screen.Editor.PressChoice("Set Color");
        screen.Editor.Typed("255 136 0");
        screen.Run(2);

        StepRow step = screen.Editor.StepRows()[0];
        ArgumentsSlot given = step.Slots.OfType<ArgumentsSlot>().Single();

        Assert.Equal(new Color(255, 136, 0), given.Sample);
        Assert.False(screen.Shows("255 136 0"), "the step still spells the color out");

        // One slot for all of them, in the order the action wanted them. Three chips with no names
        // on them would be three controls somebody cannot tell apart.
        Assert.True(screen.Shows("Set Color"), "the step does not say what it does");
    }

    /// <summary>
    /// And the values reach the block, in the order the action asked for them.
    /// </summary>
    /// <remarks>
    /// The whole of the compatibility case in one test: a light's color setter, chosen from what
    /// the light declares, given three values by somebody typing them, saved, and performed by the
    /// game when a button is pressed. Nothing in Sprocket mentions lights.
    /// </remarks>
    [Fact]
    public void TheValuesReachTheBlock()
    {
        var rig = Simulation.On(Store.Nowhere());

        rig.Editor.NewButtonSystem("Lights");

        rig.Editor.PressAdd("task");
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

        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Hydrogen Tank");
        rig.Editor.Pick().Stages.Single(s => s.Views.Count > 0).PressView("Terminal");
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
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
            + "press step press task 150 203 type Approach 200 360 press \"Terminal ›\" press \"Chime On Approach\"").Split(' '));

        Assert.True(screen.Shows("Chime On Approach"), "the sensor's chime does not ask for anything");
        Assert.Equal(FieldEditor.Chosen, screen.Editor.Picking().Fields.Single().Edits);
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
    /// One of the player's own colors is chosen by pressing it, and the hue slider by pressing it.
    /// </summary>
    /// <remarks>
    /// The two halves of the picker that are not typing. The color that comes out is written the way
    /// the language writes one, because the box is what the answer is read from - so what a press
    /// does and what somebody could have typed are the same thing.
    /// </remarks>
    [Fact]
    public void AColorIsChosenByPressingOne()
    {
        var screen = WithASequence();
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Cockpit Light");
        screen.Run(2);
        screen.Editor.PressChoice("color");
        screen.Run(2);

        // The field is one line; pressing it hangs the picker open under it.
        var field = Assert.IsType<ColorField>(screen.Editor.Pick().Arguments!.Form.Fields[0]);
        field.Command!.Execute();
        screen.Run(2);
        Assert.True(field.IsOpen);

        // The third of the fourteen paint colors, the harness's stand-in for a player who has painted.
        ((ColorPalette)field.Panel.Palettes[0]).Children[2].Command!.Execute();
        screen.Run(2);
        Assert.Equal("#386285", ColorChannels.Code(field.Panel.Color!.Value));
        Assert.NotEqual("#386285", field.Text);

        // Done gives it to the field.
        field.Panel.Submit!.Execute();
        screen.Run(2);
        Assert.Equal("#386285", Given(screen));
        Assert.Equal("#386285", field.Text);
        Assert.False(field.IsOpen);
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
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
            + "press step press task 150 203 type Standby 200 360 press \"Terminal ›\" press \"Charge Mode\"").Split(' '));

        Assert.True(screen.Shows("Auto") && screen.Shows("Recharge") && screen.Shows("Discharge"),
            "the column does not show what the value may be");

        screen.Do("press", "Recharge");
        Assert.Equal("Recharge", Given(screen));

        screen.Do("1394", "72");

        // What the block is told, which is the number the game keeps its charge modes as.
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Literal held = Assert.Single(act.Arguments);
        Assert.Equal(1, held.Whole);

        // And what a person reads, which is neither the number nor anywhere in the program.
        Assert.True(screen.Shows("Recharge"), "the step does not say what was chosen");
        Assert.False(screen.Shows("1 "), "the step says the number instead of the word");
    }

    /// <summary>A step made from the terminal view opens on that view again, its member chosen, whichever view was showing.</summary>
    [Fact]
    public void AStepMadeInTheTerminalViewIsEditedThere()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
            + "press step press task 150 203 type Standby 200 360 press \"Terminal ›\" press \"Charge Mode\" press Recharge 1394 72").Split(' '));
        var editor = screen.Editor;
        Assert.IsType<Act>(editor.Program.Sequences[0].Steps[0]);

        editor.ShowsTerminal = false;
        editor.PressStepSlot(0, 0);
        screen.Run(2);

        Assert.True(editor.ShowsTerminal, "the picker did not open on the view that lists the step's member");
        Assert.Contains(editor.Pick().Stages.SelectMany(stage => stage.Entries()), entry => entry.Text == "Charge Mode" && entry.IsChosen);
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
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
            + "press step press task 150 203 type Tug 200 360 press \"Terminal ›\" press \"Forward Direction\"").Split(' '));

        Assert.True(screen.Shows("Forward Direction"), "the forward direction does not ask for anything");

        // Closed, so none of the six is on the screen and neither is a choice nobody has made.
        Assert.False(screen.Shows("Backward"), "the list is showing while it is shut");

        screen.Do("1316", "209");
        Assert.True(screen.Shows("Backward") && screen.Shows("Down"),
            "pressing the box did not open the list");

        screen.Do("1316", "327");
        Assert.Equal("Left", Given(screen));
        Assert.False(screen.Shows("Down"), "choosing did not put the list away");

        screen.Do("1394", "72");

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(2, Assert.Single(act.Arguments).Whole);
        Assert.True(screen.Shows("Left"), "the step does not say what was chosen");
    }

    /// <summary>An option left as it starts is written as what it starts with, because every parameter is given an argument.</summary>
    [Fact]
    public void AnOptionLeftAsItStartsIsWrittenAsItsDefault()
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Vent");
        screen.Editor.PressChoice("depressurize");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);

        var act = screen.Editor.Program.Sequences.SelectMany(sequence => sequence.Steps).OfType<Act>()
            .Single(step => step.ActionId == Framework.Game.Blocks.Vent.Depressurize);
        Assert.Equal(Framework.Game.Blocks.Vent.Hold, Assert.Single(act.Arguments).Whole);
    }

    /// <summary>The fields of a form end together against the right, whatever their widths: a dial with its box, a number, a list.</summary>
    [Fact]
    public void TheFieldsOfAFormEndTogether()
    {
        var rig = Rig.Demo();
        rig.Rotor(30, "Rotor");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Rotor");
        screen.Editor.PressChoice("rotate to");
        screen.Run(2);

        var form = screen.Editor.Pick().Arguments!.Form;
        Assert.Equal(3, form.Fields.Count);

        // A value that may come from a setting ends with its mark, which is part of it.
        var ends = Enumerable.Range(0, form.Fields.Count)
            .Select(i => (Lumel.Element?)form.MarkOf(i) ?? (Lumel.Element)form.Fields[i])
            .Select(part => part.Bounds.X + part.Bounds.Width)
            .ToList();
        Assert.All(ends, end => Assert.Equal(ends[0], end, 1));
    }

    /// <summary>
    /// Three words too long for the room beside their label fold into a list, where three short ones stay a row.
    /// </summary>
    /// <remarks>
    /// A rotor turns directly, clockwise or counter-clockwise, and in the game's own letters the three are wider than the
    /// column: a row of them ran past its edge. The list is one line whatever its words. These three are long enough not
    /// to fit in any letters, so the harness's stand-in widths cannot let them through.
    /// </remarks>
    [Fact]
    public void AFewWordsTooLongForTheirRoomFoldIntoAList()
    {
        var rig = Rig.Demo();
        // A definition of its own, since a kind is described once per definition from the first block of it seen.
        rig.Weld(new Plain(30, "Mood Light", typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_InteriorLight),
                "MoodLight", "Mood Light"))
            .Carrying("Mood", "Mood", "brightly lit all the way round", "0", "dimmed down to a gentle glow", "1",
                "flickering as if about to fail", "2");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Mood Light");
        screen.Editor.Pick().Stages.Single(s => s.Views.Count > 0).PressView("Terminal");
        screen.Editor.PressChoice("Mood");
        screen.Run(2);

        Assert.True(screen.Shows("Mood"), "the value is not asked for");
        Assert.False(screen.Shows("dimmed down to a gentle glow"), "the three are a row too wide for the column");
        Assert.False(screen.Shows("flickering as if about to fail"), "the three are a row too wide for the column");
    }
}
