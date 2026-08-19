using Reactor.Editing;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Ui;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// The authoring flow, driven through the component surface a target renders.
/// </summary>
public class EditorTests
{
    sealed class Ship
    {
        public TestWorld World = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public ReactorEditor Editor = null!;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                World.Tick();
                Editor.Tick(++_frame);
            }
        }
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.World.Add(new TestDoor(1, "Inner Door"));
        ship.World.Add(new TestDoor(2, "Outer Door"));
        ship.World.Add(new TestButton(3, "Airlock Button"));
        ship.World.Add(new TestLamp(4, "Airlock Light"));

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new ReactorEditor(ship.World, ship.Automation, ship.Runner);
        return ship;
    }

    static void StartOnTheButton(ReactorEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
    }

    /// <summary>
    /// Naming lives on the heading, and clearing it hands the heading back.
    /// </summary>
    /// <remarks>
    /// There used to be a naming box inside the sequence, underneath a heading that already
    /// showed the name: two places for one thing, one announcing it and one setting it. The
    /// name belongs on the heading because the heading is what a name is for.
    /// </remarks>
    [Fact]
    public void TheHeadingIsWhereNamingHappens()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        // Unnamed: the heading says what the thing does, and there is no name behind it for a
        // target to prefill a box with.
        Assert.True(ship.Editor.Screen.CanRename);
        Assert.StartsWith("When Airlock Button is pressed", ship.Editor.Screen.Title);
        Assert.Equal("", ship.Editor.Screen.CustomName);

        ship.Editor.Name("Let me out");
        ship.Tick();
        Assert.Equal("Let me out", ship.Editor.Screen.Title);
        Assert.Equal("Let me out", ship.Editor.Screen.CustomName);

        // Clearing is as ordinary an act as naming, and the sentence comes back.
        ship.Editor.Name("");
        ship.Tick();
        Assert.StartsWith("When Airlock Button is pressed", ship.Editor.Screen.Title);
        Assert.Equal("", ship.Editor.Screen.CustomName);
    }

    /// <summary>
    /// Backing out of a new machine's first sequence lands on the machine.
    /// </summary>
    /// <remarks>
    /// Making a machine goes straight into its first sequence, because there is nothing on an
    /// empty machine's screen worth stopping for. That shortcut used to leave the machine's
    /// own screen off the stack entirely, so the only way back out of that first sequence was
    /// all the way to the ship - past the very thing you had just made.
    /// </remarks>
    [Fact]
    public void BackingOutOfTheFirstSequenceLandsOnTheMachine()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);

        ship.Editor.Back();

        Assert.Equal("Airlock", ship.Editor.Screen.Title);
        Assert.True(ship.Editor.Screen.CanGoBack);

        ship.Editor.Back();
        Assert.Equal("Your ship", ship.Editor.Screen.Title);
    }

    /// <summary>
    /// Ticking a block must not take the rest of the ship off the screen.
    /// </summary>
    /// <remarks>
    /// The picker used to offer only blocks that could still be grouped with the ones already
    /// ticked, so picking a button - the one thing on this ship with an event nothing else
    /// shares - emptied the list down to itself. Reading that as "my blocks are gone" is the
    /// only reasonable reading. The stages run left to right: what is picked decides what can
    /// be said about it, never which blocks exist.
    /// </remarks>
    [Fact]
    public void PickingABlockLeavesEveryOtherBlockOnOffer()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When...");

        var before = Listed(ship.Editor);
        ship.Editor.PressBlock("Airlock Button");

        Assert.Equal(before, Listed(ship.Editor));

        // The lamp is absent throughout, and that is the question talking rather than the
        // selection: nothing can happen to it, so there is nothing to ask about it here.
        Assert.Equal("Inner Door, Outer Door, Airlock Button", before);
    }

    /// <summary>
    /// An impossible pair is allowed, and answered where the answer belongs.
    /// </summary>
    [Fact]
    public void BlocksWithNothingInCommonSaySo_RatherThanBeingUnpickable()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When...");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.PressBlock("Inner Door");

        var what = ship.Editor.Component<PickComponent>().Stages[1];
        Assert.Empty(what.Entries);
        Assert.NotEqual("", what.EmptyText);
    }

    static string Listed(ReactorEditor editor) =>
        string.Join(", ", editor.Component<PickComponent>().Stages[0].Entries.Select(e => e.Text));

    [Fact]
    public void ItOpensOnTheMachines_NotOnAListOfBlocks()
    {
        var ship = Build();

        // The same screen an established ship gets, with nothing in the list yet - not a
        // different screen that rearranges itself the moment anything is built.
        Assert.Equal("Your ship", ship.Editor.Screen.Title);
        Assert.Empty(ship.Editor.Component<StatusListComponent>().Entries);

        // Both ways in are offered. How a target asks for the name is its own business, so
        // there is nothing here about panels or dialogs.
        var start = ship.Editor.Component<StartComponent>();
        Assert.Equal("New system, Load system", string.Join(", ", start.Options.Select(o => o.Label)));
        Assert.Equal("What is it?", start.Options[0].Question);
    }

    [Fact]
    public void TheQuestionsComeInReadingOrder()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");

        // One pane to begin with; the second appears once it has something to be about.
        Assert.Single(editor.Component<PickComponent>().Stages);
        Assert.False(editor.Screen.HasPrimary);

        editor.PressBlock("Airlock Button");
        Assert.Equal(2, editor.Component<PickComponent>().Stages.Count);

        // Still nothing to move on to - the path is not finished.
        Assert.False(editor.Screen.HasPrimary);

        editor.PressChoice("is pressed");
        Assert.True(editor.Screen.HasPrimary);

        editor.PressPrimary();
        Assert.NotNull(editor.Sentence());
    }

    [Fact]
    public void SeveralBlocksReadAsEitherOnATrigger_AndBothOnAStep()
    {
        // A trigger fires on whichever happens first, a step acts on all of them.
        // Saying "and" in both places would state the wrong thing in one of them.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Inner Door");
        editor.PressBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        Assert.Equal("Inner Door or Outer Door", editor.Sentence().Trigger.Slots[0].Text);

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.PressBlock("Outer Door");
        editor.PressChoice("close");
        editor.PressPrimary();

        Assert.Equal("Inner Door and Outer Door", editor.Sentence().Steps[0].Slots[1].Text);
    }

    static TestDoor Door(Ship ship, string name) =>
        ship.World.Devices.OfType<TestDoor>().First(d => d.Name == name);

    [Fact]
    public void AllOfThemTogether_ReadsAsAndAndFiresWhenTheLastArrives()
    {
        // "When tank A and tank B are empty" is a different sentence from "either of
        // them", and fires at a different moment - when the last one gets there.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Inner Door");
        editor.PressBlock("Outer Door");
        editor.PressChoice("all of them, together");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        Assert.Equal("Inner Door and Outer Door", editor.Sentence().Trigger.Slots[0].Text);

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();

        ship.Tick(3);
        Door(ship, "Inner Door").ForceOpen();
        ship.Tick(3);
        Assert.Empty(ship.World.Log);

        Door(ship, "Outer Door").ForceOpen();
        ship.Tick(3);
        Assert.Contains("Airlock Light:on", ship.World.Log);
    }

    [Fact]
    public void TheAnyOrAllChoiceOnlyAppearsOnceItMatters()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Inner Door");
        Assert.False(editor.HasChoice("all of them, together"));

        editor.PressBlock("Outer Door");
        Assert.True(editor.HasChoice("all of them, together"));
    }

    [Fact]
    public void BackingOutOfTheQuestionsLeavesNothingBehind()
    {
        // The sequence is not created until the questions are answered, so an
        // abandoned run does not litter the list with an empty one.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.Back();

        Assert.Empty(ship.Automation.Sequences);

        // And one more back off the machine itself.
        editor.Back();
        Assert.Equal("Your ship", editor.Screen.Title);
    }

    [Fact]
    public void BackRetreatsOneQuestionAtATime()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        Assert.Equal("When something happens", editor.Screen.Title);

        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");

        // Both stages are one screen now, and the starters are no longer a screen at all, so
        // one step back reaches the machine and a second reaches the ship.
        editor.Back();
        Assert.Equal("Airlock", editor.Screen.Title);

        editor.Back();
        Assert.Equal("Your ship", editor.Screen.Title);
    }

    [Fact]
    public void NamingASequence_ReplacesTheSentenceEverywhere()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        ship.Tick();
        Assert.StartsWith("When Airlock Button is pressed", editor.Screen.Title);

        editor.Name("Let me out");
        ship.Tick();
        Assert.Equal("Let me out", editor.Screen.Title);

        // And in the list, which is the reason naming exists at all. Backing out of the
        // sequence lands on the machine it belongs to, which is where its sequences are listed.
        editor.Back();
        ship.Tick();

        Assert.Equal("Airlock", editor.Screen.Title);
        Assert.Equal("Let me out", editor.Component<StatusListComponent>().Entries[0].Summary);
    }

    [Fact]
    public void NamingDoesNotRebuildTheScreen()
    {
        // Rebuilding would recreate the text field mid-word and drop the caret.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("Every...");
        editor.PressChoice("steady - every 100 ticks");

        // The component instances must survive, or a target rebuilds its controls
        // and the field loses focus and the caret mid-word.
        var before = editor.Sentence();
        editor.Name("Housekeeping");

        Assert.Same(before, editor.Sentence());
    }

    [Fact]
    public void AddingAStep_AsksWhatThenAsksHow()
    {
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("do something");
        Assert.Equal("Do something", editor.Screen.Title);
        Assert.Single(editor.Component<PickComponent>().Stages);

        editor.PressBlock("Inner Door");
        Assert.Equal(2, editor.Component<PickComponent>().Stages.Count);

        editor.PressChoice("close");
        editor.PressPrimary();

        Assert.Single(editor.Sentence().Steps);
        Assert.Equal("close", editor.Sentence().Steps[0].Slots[0].Text);
        Assert.Equal("Inner Door", editor.Sentence().Steps[0].Slots[1].Text);
    }

    [Fact]
    public void BackingOutOfAddingAStep_LeavesNoHalfWrittenLine()
    {
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.Back();

        Assert.Empty(editor.Sentence().Steps);
    }

    [Fact]
    public void WaitingIsItsOwnKindOfStep_NotAnActionOnABlock()
    {
        // A timed wait has no block to act on, so forcing it through the same
        // question would mean answering one that does not apply.
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("wait");
        Assert.Equal("How long should it wait?", editor.Screen.Title);

        editor.PressChoice("5 seconds - 300 ticks");

        var step = Assert.Single(editor.Sentence().Steps);
        Assert.Equal("wait", step.Lead);
        Assert.Equal("5 seconds", step.Slots[0].Text);
    }

    [Fact]
    public void LastRunIsWordedForPeople_AndNotRebuiltEveryTick()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        ship.Tick();
        Assert.Equal("", editor.Sentence().LastRun);

        editor.RunNow();
        ship.Tick();
        Assert.Equal("just now", editor.Sentence().LastRun);

        ship.Tick(180);
        var words = editor.Sentence().LastRun;
        Assert.Equal("3 seconds ago", words);

        // Half a second later it still says the same thing, and says it with the very
        // same string - the words are not rebuilt sixty times a second.
        ship.Tick(30);
        Assert.Same(words, editor.Sentence().LastRun);

        ship.Tick(60);
        Assert.Equal("4 seconds ago", editor.Sentence().LastRun);

        ship.Tick(60 * 60);
        Assert.Equal("a minute ago", editor.Sentence().LastRun);
    }

    [Fact]
    public void RunNow_StartsItWithoutWaitingForTheTrigger()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();

        ship.Tick(3);
        Assert.Empty(ship.World.Log);

        editor.RunNow();
        ship.Tick();

        Assert.Contains("Airlock Light:on", ship.World.Log);
    }
}
