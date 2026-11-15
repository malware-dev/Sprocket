using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Triggers;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel;
using Lumel.Controls;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The Triggers tab stands a logic system's triggers in the program's order, each run under one category headed by its
/// name, as the Sequences tab stands its sequences: carried by their grips, into or out of a category, each change one step back.
/// </summary>
public class TriggerCategoryTests
{
    /// <summary>A new category stands under the triggers with none under it, and goes again as it is undone.</summary>
    [Fact]
    public void ANewCategoryStandsUnderTheTriggers()
    {
        var screen = Triggers();

        AddCategory(screen);

        Assert.Equal(new[] { "every 1", "every 2", "every 3", "# Category 1", "+ Category 1" }, Rows(screen));
        Assert.Equal(new[] { "Category 1" }, Empty(screen));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Empty(Empty(screen));
    }

    /// <summary>
    /// A trigger carried right under a category's name goes into it, which the program says with the category on the
    /// trigger; one step back takes it out again.
    /// </summary>
    [Fact]
    public void ATriggerCarriedRightUnderACategoryIsInIt()
    {
        var screen = Triggers();
        AddCategory(screen);

        Carry(screen, Row(screen, "every 1"), Row(screen, "# Category 1"), true);

        Assert.Equal(new[] { "every 2", "every 3", "# Category 1", "every 1", "+ Category 1" }, Rows(screen));
        Assert.Equal("Category 1", Trigger(screen, 1).Category);
        Assert.Equal(new[] { 2, 3, 1 }, Order(screen));
        Assert.True(((TriggerRow)Row(screen, "every 1")).Indented);
        Assert.Contains("    #@ category: \"Category 1\"\n    every 1s", ProgramWriter.Save(screen.Editor.Program, screen.Editor.World.Values));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "every 1", "every 2", "every 3", "# Category 1", "+ Category 1" }, Rows(screen));
        Assert.Equal("", Trigger(screen, 1).Category);
    }

    /// <summary>A trigger dropped above a category's end goes into it, and dropped below the end stays out.</summary>
    [Fact]
    public void ADropAboveACategorysEndIsInItAndBelowIsOut()
    {
        var screen = Triggers();
        Put(screen, 1, "Timers");

        Carry(screen, Row(screen, "every 3"), Row(screen, "every 1"), true);
        Assert.Equal(new[] { "# Timers", "every 1", "every 3", "+ Timers", "every 2" }, Rows(screen));
        Assert.Equal("Timers", Trigger(screen, 3).Category);

        Carry(screen, Row(screen, "every 3"), Row(screen, "+ Timers"), true);
        Assert.Equal(new[] { "# Timers", "every 1", "+ Timers", "every 3", "every 2" }, Rows(screen));
        Assert.Equal("", Trigger(screen, 3).Category);
    }

    /// <summary>A category's end offers the ways a trigger starts, as the tab's own add does, and the trigger made stands at the category's end.</summary>
    [Fact]
    public void ACategorysEndAddsATriggerIntoIt()
    {
        var screen = Triggers();
        Put(screen, 1, "Timers");
        var system = screen.Editor.Program.Systems[0];
        var adds = ((CategoryEndRow)Row(screen, "+ Timers")).Adds;
        Assert.Equal(new[] { SentenceText.WhenWord, SentenceText.EveryWord }, adds.Choices.Select(choice => choice.Text));

        adds.Choices.Single(choice => choice.Text == SentenceText.EveryWord).Adds!.Execute();
        screen.Run(5);
        screen.Editor.TimerRate().Give("4", false);
        screen.Run(5);
        screen.Editor.PressPrimary();
        screen.Run(5);

        Assert.Equal(4, system.Triggers.Count);
        Assert.Equal("Timers", system.Triggers[1].Category);
        Assert.Equal(4, Seconds(system.Triggers[1]));
        Assert.Equal(new[] { 1, 4, 2, 3 }, Order(screen));
    }

    /// <summary>A category carried takes its triggers with it, and changes their order in the program.</summary>
    [Fact]
    public void ACategoryCarriesItsTriggers()
    {
        var screen = Triggers();
        Put(screen, 1, "Timers", 2, "Timers");

        Carry(screen, Row(screen, "# Timers"), Row(screen, "every 3"), true);

        Assert.Equal(new[] { "every 3", "# Timers", "every 1", "every 2", "+ Timers" }, Rows(screen));
        Assert.Equal(new[] { 3, 1, 2 }, Order(screen));
    }

    /// <summary>A category removed leaves its triggers where they stand, under no category, and one step back puts them under it again.</summary>
    [Fact]
    public void RemovingACategoryKeepsItsTriggers()
    {
        var screen = Triggers();
        Put(screen, 2, "Timers");

        ((CategoryRow)Row(screen, "# Timers")).Removes!.Execute();
        screen.Run(5);

        Assert.Equal(new[] { "every 1", "every 2", "every 3" }, Rows(screen));
        Assert.Equal("", Trigger(screen, 2).Category);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "every 1", "# Timers", "every 2", "+ Timers", "every 3" }, Rows(screen));
    }

    /// <summary>While the logic system is on, no trigger or category is carried, and the grips say why.</summary>
    [Fact]
    public void NothingMovesWhileTheSystemIsOn()
    {
        var screen = Triggers();
        Put(screen, 2, "Timers");
        screen.Editor.Program.Systems[0].Enabled = true;
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);

        var category = (CategoryRow)Row(screen, "# Timers");
        Assert.False(category.DragHandle.CanDrag);
        Assert.Null(category.Removes);
        Assert.False(((TriggerRow)Row(screen, "every 3")).DragHandle.CanDrag);
        Assert.Equal(screen.Editor.Texts.Codex.SystemTurnOffToEdit, category.DragHandle.Tip?.Text);
    }

    /// <summary>The Airlock logic system with three timers, every 1, 2 and 3 seconds, switched off, on its Triggers tab.</summary>
    static Screen Triggers()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        var number = system.Sequences[0].Number;
        system.DropTriggersFor(number);

        for (var seconds = 1; seconds <= 3; seconds++)
            system.AddTrigger(new EveryTrigger(Duration.OfSeconds(seconds)) { Does = new Run(number) });

        system.Enabled = false;
        editor.Program.Edited();
        editor.GoToRoot();
        editor.Push(new LogicSystemPage(editor, system));
        screen.Run(2);
        editor.PressTab(editor.Texts.SystemTriggers);
        return screen.Run(5);
    }

    /// <summary>Puts each timer, by its seconds in <paramref name="pairs" />, under the category after it, as a program would say it, and shows the tab again.</summary>
    static void Put(Screen screen, params object[] pairs)
    {
        for (var i = 0; i + 1 < pairs.Length; i += 2)
            Trigger(screen, (int)pairs[i]).Category = (string)pairs[i + 1];

        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);
    }

    static int Seconds(Trigger trigger) => (int)((EveryTrigger)trigger).Interval.Seconds;

    static Trigger Trigger(Screen screen, int seconds) => screen.Editor.Program.Systems[0].Triggers.Single(trigger => Seconds(trigger) == seconds);

    /// <summary>The timers of the logic system by their seconds, in the program's order.</summary>
    static int[] Order(Screen screen) => screen.Editor.Program.Systems[0].Triggers.Select(Seconds).ToArray();

    /// <summary>The names of the categories with no trigger under them.</summary>
    static string[] Empty(Screen screen) => screen.Editor.Program.Systems[0].EmptyTriggerCategories.Select(empty => empty.Name).ToArray();

    /// <summary>The rows of the Triggers tab: a timer as "every" and its seconds, a category by its name after "# ", its end after "+ ".</summary>
    static string[] Rows(Screen screen) => ((LogicSystemPage)screen.Editor.Current!).TriggerRows.Select(Said).ToArray();

    static string Said(Element row)
    {
        if (row is CategoryRow category)
            return "# " + category.Name;

        if (row is CategoryEndRow end)
            return "+ " + end.Category;

        return "every " + (int)((TriggerRow)row).Starts.OfType<DurationSlot>().Single().Length.Seconds;
    }

    static Element Row(Screen screen, string said) => ((LogicSystemPage)screen.Editor.Current!).TriggerRows.Single(row => Said(row) == said);

    static void AddCategory(Screen screen)
    {
        var page = (LogicSystemPage)screen.Editor.Current!;
        page.View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd) && Shown(button)).Command!.Execute();
        screen.Run(5);
    }

    /// <summary>Whether <paramref name="element" /> and everything it stands in are visible.</summary>
    static bool Shown(Element element)
    {
        for (var at = element; at != null; at = at.Parent)
        {
            if (!at.IsVisible)
                return false;
        }

        return true;
    }

    /// <summary>Takes <paramref name="row" /> by its grip and lets it go over the top or bottom edge of <paramref name="onto" />, as <paramref name="below" /> says.</summary>
    static void Carry(Screen screen, Element row, Element onto, bool below)
    {
        var grip = (Element)((IReorderable)row).DragHandle;
        var from = grip.ToScreen(grip.Bounds.Center);
        var edge = onto.ToScreen(new Vector2(onto.Bounds.X, below ? onto.Bounds.Bottom - 2f : onto.Bounds.Y + 2f));
        var down = row == onto ? 0f : edge.Y - row.ToScreen(row.Bounds.Center).Y + (below ? row.Bounds.Height / 2f : -row.Bounds.Height / 2f);
        var to = new Vector2(from.X, from.Y + down);

        screen.Scene.MoveTo(from);
        screen.Scene.Down(from);
        screen.Run(1);
        screen.Scene.Move(new Vector2(from.X, (from.Y + to.Y) / 2f));
        screen.Run(1);
        screen.Scene.Move(to);
        screen.Run(1);
        screen.Scene.Up();
        screen.Run(5);
    }
}
