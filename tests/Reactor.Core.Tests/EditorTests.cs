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

        public void Beat(int beats = 1)
        {
            for (var i = 0; i < beats; i++)
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

    [Fact]
    public void ItOpensOnTheQuestion_NotOnAListOfBlocks()
    {
        var ship = Build();

        Assert.Equal("Your automation", ship.Editor.Screen.Title);
        Assert.Contains(ship.Editor.Component<ChoiceListComponent>().Entries,
            e => e.Text == "When something happens");
    }

    [Fact]
    public void TheQuestionsComeInReadingOrder()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressChoice("When something happens");
        Assert.Equal("What should this listen to?", editor.Screen.Title);

        // Nothing to move on to until something is chosen, which is also the nudge.
        Assert.False(editor.Screen.HasPrimary);
        editor.PressBlock("Airlock Button");
        Assert.True(editor.Screen.HasPrimary);

        editor.PressPrimary();
        Assert.Equal("What should it do?", editor.Screen.Title);

        editor.PressChoice("is pressed");
        Assert.NotNull(editor.Sentence());
    }

    [Fact]
    public void BackingOutOfTheQuestionsLeavesNothingBehind()
    {
        // The sequence is not created until the questions are answered, so an
        // abandoned run does not litter the list with an empty one.
        var ship = Build();
        var editor = ship.Editor;

        editor.PressChoice("When something happens");
        editor.PressBlock("Airlock Button");
        editor.Back();

        Assert.Empty(ship.Automation.Sequences);
        Assert.Equal("Your automation", editor.Screen.Title);
    }

    [Fact]
    public void BackRetreatsOneQuestionAtATime()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressChoice("When something happens");
        editor.PressBlock("Airlock Button");
        editor.PressPrimary();
        Assert.Equal("What should it do?", editor.Screen.Title);

        editor.Back();
        Assert.Equal("What should this listen to?", editor.Screen.Title);

        editor.Back();
        Assert.Equal("Your automation", editor.Screen.Title);
    }

    [Fact]
    public void NamingASequence_ReplacesTheSentenceEverywhere()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressChoice("When something happens");
        editor.PressBlock("Airlock Button");
        editor.PressPrimary();
        editor.PressChoice("is pressed");

        ship.Beat();
        Assert.StartsWith("When Airlock Button is pressed", editor.Screen.Title);

        editor.Name("Let me out");
        ship.Beat();
        Assert.Equal("Let me out", editor.Screen.Title);

        // And in the list, which is the reason naming exists at all.
        editor.PressAction("Sequences");
        ship.Beat();
        Assert.Equal("Let me out", editor.Component<SequenceListComponent>().Entries[0].Summary);
    }

    [Fact]
    public void NamingDoesNotRebuildTheScreen()
    {
        // Rebuilding would recreate the text field mid-word and drop the caret.
        var ship = Build();
        var editor = ship.Editor;

        editor.PressChoice("Every so often");
        editor.PressChoice("steady — every 100 beats");

        var version = editor.Screen.StructureVersion;
        editor.Name("Housekeeping");

        Assert.Equal(version, editor.Screen.StructureVersion);
    }

    [Fact]
    public void RunNow_StartsItWithoutWaitingForTheTrigger()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressChoice("When something happens");
        editor.PressBlock("Airlock Button");
        editor.PressPrimary();
        editor.PressChoice("is pressed");

        editor.PressAdd("do something");
        editor.PressStepSlot(0, 1);
        editor.PressBlock("Airlock Light");
        editor.PressPrimary();
        editor.PressStepSlot(0, 0);
        editor.PressChoice("turn on");

        ship.Beat(3);
        Assert.Empty(ship.World.Log);

        editor.RunNow();
        ship.Beat();

        Assert.Contains("Airlock Light:on", ship.World.Log);
    }
}
