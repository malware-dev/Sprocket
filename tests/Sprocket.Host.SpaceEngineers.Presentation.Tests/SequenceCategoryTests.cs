using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The Sequences tab stands a logic system's sequences in the program's order, each run under one category headed by its
/// name, as the installation panel stands its slots: carried by their grips, into or out of a category by how far across,
/// each change one step back.
/// </summary>
public class SequenceCategoryTests
{
    /// <summary>A new category stands under the sequences with none under it, and goes again as it is undone.</summary>
    [Fact]
    public void ANewCategoryStandsUnderTheSequences()
    {
        var screen = Sequences();

        AddCategory(screen);

        Assert.Equal(new[] { "Cycle In", "Cycle Out", "Purge", "# Category 1", "+ Category 1" }, Rows(screen));
        Assert.Equal(new[] { "Category 1" }, Empty(screen));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Empty(Empty(screen));
    }

    /// <summary>
    /// A sequence carried right under a category's name goes into it and stands there in the program's order, which the
    /// program says with the category on the sequence; one step back takes it out again.
    /// </summary>
    [Fact]
    public void ASequenceCarriedRightUnderACategoryIsInIt()
    {
        var screen = Sequences();
        AddCategory(screen);

        Carry(screen, Row(screen, "Cycle In"), Row(screen, "# Category 1"), true);

        Assert.Equal(new[] { "Cycle Out", "Purge", "# Category 1", "Cycle In", "+ Category 1" }, Rows(screen));
        Assert.Equal("Category 1", Sequence(screen, "Cycle In").Category);
        Assert.Equal(new[] { "Cycle Out", "Purge", "Cycle In" }, Order(screen));
        Assert.True(((StatusRow)Row(screen, "Cycle In")).Indented);
        Assert.Contains("    #@ category: \"Category 1\"\n    sequence 1 as 'Cycle In' {", ProgramWriter.Save(screen.Editor.Program, screen.Editor.World.Values));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "Cycle In", "Cycle Out", "Purge", "# Category 1", "+ Category 1" }, Rows(screen));
        Assert.Equal("", Sequence(screen, "Cycle In").Category);
    }

    /// <summary>A sequence dropped above a category's end goes into it, and dropped below the end stays out, with no carrying across.</summary>
    [Fact]
    public void ADropAboveACategorysEndIsInItAndBelowIsOut()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling");

        Carry(screen, Row(screen, "Purge"), Row(screen, "Cycle In"), true);
        Assert.Equal(new[] { "# Cycling", "Cycle In", "Purge", "+ Cycling", "Cycle Out" }, Rows(screen));
        Assert.Equal("Cycling", Sequence(screen, "Purge").Category);

        Carry(screen, Row(screen, "Purge"), Row(screen, "+ Cycling"), true);
        Assert.Equal(new[] { "# Cycling", "Cycle In", "+ Cycling", "Purge", "Cycle Out" }, Rows(screen));
        Assert.Equal("", Sequence(screen, "Purge").Category);
    }

    /// <summary>A category's end adds a new sequence into it, at its end; left with nothing done to it, the sequence goes again, as any new one does.</summary>
    [Fact]
    public void ACategorysEndAddsASequenceIntoIt()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling");
        var system = screen.Editor.Program.Systems[0];

        ((CategoryEndRow)Row(screen, "+ Cycling")).Adds.Command!.Execute();
        screen.Run(5);

        Assert.Equal(4, system.Sequences.Count);
        var made = system.Sequences[1];
        Assert.Equal("Cycling", made.Category);
        Assert.Equal(new[] { "Cycle In", made.Name, "Cycle Out", "Purge" }, Order(screen));

        screen.Editor.Back();
        screen.Run(5);
        Assert.Equal(3, system.Sequences.Count);
        Assert.Equal(new[] { "# Cycling", "Cycle In", "+ Cycling", "Cycle Out", "Purge" }, Rows(screen));
    }

    /// <summary>An empty category has an end too, which takes its first sequence.</summary>
    [Fact]
    public void AnEmptyCategoryTakesItsFirstSequence()
    {
        var screen = Sequences();
        AddCategory(screen);
        Assert.Equal(new[] { "Cycle In", "Cycle Out", "Purge", "# Category 1", "+ Category 1" }, Rows(screen));

        Carry(screen, Row(screen, "Cycle In"), Row(screen, "# Category 1"), true);

        Assert.Equal("Category 1", Sequence(screen, "Cycle In").Category);
        Assert.Empty(Empty(screen));
    }

    /// <summary>A category carried takes its sequences with it, and changes their order in the program.</summary>
    [Fact]
    public void ACategoryCarriesItsSequences()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling", "Cycle Out", "Cycling");

        Assert.Equal(new[] { "# Cycling", "Cycle In", "Cycle Out", "+ Cycling", "Purge" }, Rows(screen));

        Carry(screen, Row(screen, "# Cycling"), Row(screen, "Purge"), true);

        Assert.Equal(new[] { "Purge", "# Cycling", "Cycle In", "Cycle Out", "+ Cycling" }, Rows(screen));
        Assert.Equal(new[] { "Purge", "Cycle In", "Cycle Out" }, Order(screen));
    }

    /// <summary>A category removed leaves its sequences where they stand, under no category, and one step back puts them under it again.</summary>
    [Fact]
    public void RemovingACategoryKeepsItsSequences()
    {
        var screen = Sequences();
        Put(screen, "Cycle Out", "Cycling");

        Category(screen, "Cycling").Removes!.Execute();
        screen.Run(5);

        Assert.Equal(new[] { "Cycle In", "Cycle Out", "Purge" }, Rows(screen));
        Assert.Equal("", Sequence(screen, "Cycle Out").Category);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "Cycle In", "# Cycling", "Cycle Out", "+ Cycling", "Purge" }, Rows(screen));
    }

    /// <summary>A category's name is a button with no pencil, opening a panel on its name and whether it runs one at a time.</summary>
    [Fact]
    public void ACategorysNameOpensItsPanel()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling");
        var plate = Category(screen, "Cycling").Plate;
        Assert.False(plate.CanRename);
        Assert.True(plate.Boxed);

        Open(screen, "Cycling");

        var form = Settling(screen);
        Assert.Equal("Cycling", form.Fields[0].Text);
        Assert.Equal("off", form.Fields[1].Text);
    }

    /// <summary>
    /// Switched to one at a time, every sequence in the category names it as its exclusive id, in one change; one step back,
    /// and none does.
    /// </summary>
    [Fact]
    public void OneAtATimeHasTheSequencesNameTheCategory()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling", "Cycle Out", "Cycling");

        Settle(screen, "Cycling", null, true);

        Assert.Equal("Cycling", Sequence(screen, "Cycle In").Exclusive);
        Assert.Equal("Cycling", Sequence(screen, "Cycle Out").Exclusive);
        Assert.Equal("", Sequence(screen, "Purge").Exclusive);
        Assert.Contains("    sequence 1 as 'Cycle In' exclusive Cycling {", ProgramWriter.Save(screen.Editor.Program, screen.Editor.World.Values));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal("", Sequence(screen, "Cycle In").Exclusive);
        Assert.Equal("", Sequence(screen, "Cycle Out").Exclusive);
    }

    /// <summary>A sequence carried into a category that runs one at a time takes its id, and loses it carried out again.</summary>
    [Fact]
    public void ASequenceCarriedInOrOutTakesOrLosesTheId()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling");
        Settle(screen, "Cycling", null, true);

        Carry(screen, Row(screen, "Purge"), Row(screen, "Cycle In"), true);
        Assert.Equal("Cycling", Sequence(screen, "Purge").Exclusive);

        Carry(screen, Row(screen, "Purge"), Row(screen, "+ Cycling"), true);
        Assert.Equal("", Sequence(screen, "Purge").Exclusive);
        Assert.Equal("Cycling", Sequence(screen, "Cycle In").Exclusive);
    }

    /// <summary>Renamed in its panel, a category that runs one at a time has its sequences name the new name; removed, none names it.</summary>
    [Fact]
    public void RenamingOrRemovingCarriesTheId()
    {
        var screen = Sequences();
        Put(screen, "Cycle In", "Cycling");
        Settle(screen, "Cycling", null, true);

        Settle(screen, "Cycling", "Doors", true);
        Assert.Equal("Doors", Sequence(screen, "Cycle In").Category);
        Assert.Equal("Doors", Sequence(screen, "Cycle In").Exclusive);

        Category(screen, "Doors").Removes!.Execute();
        screen.Run(5);
        Assert.Equal("", Sequence(screen, "Cycle In").Exclusive);
    }

    /// <summary>An empty category keeps one at a time for the first sequence put in it.</summary>
    [Fact]
    public void AnEmptyCategoryKeepsOneAtATime()
    {
        var screen = Sequences();
        AddCategory(screen);
        Open(screen, "Category 1");
        Settling(screen).Fields[1].Text = "on";
        Done(screen).Command!.Execute();
        screen.Run(5);

        Carry(screen, Row(screen, "Cycle In"), Row(screen, "# Category 1"), true);

        Assert.Equal("Category 1", Sequence(screen, "Cycle In").Exclusive);
    }

    /// <summary>While the logic system is on, no sequence or category is carried, and the grips say why.</summary>
    [Fact]
    public void NothingMovesWhileTheSystemIsOn()
    {
        var screen = Sequences();
        Put(screen, "Cycle Out", "Cycling");
        screen.Editor.Program.Systems[0].Enabled = true;
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);

        var category = Category(screen, "Cycling");
        Assert.False(category.DragHandle.CanDrag);
        Assert.Null(category.Removes);
        Assert.False(((StatusRow)Row(screen, "Purge")).DragHandle.CanDrag);
        Assert.Equal(screen.Editor.Texts.Codex.SystemTurnOffToEdit, category.DragHandle.Tip?.Text);
    }

    /// <summary>The Airlock logic system with three sequences, switched off, on its Sequences tab.</summary>
    static Screen Sequences()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        system.Sequences[0].Name = "Cycle In";
        editor.Program.AddSequence(system).Name = "Cycle Out";
        editor.Program.AddSequence(system).Name = "Purge";
        system.Enabled = false;
        editor.Program.Edited();
        editor.GoToRoot();
        editor.Push(new LogicSystemPage(editor, system));
        screen.Run(2);
        editor.PressTab(editor.Texts.SystemSequences);
        return screen.Run(5);
    }

    /// <summary>Puts each sequence named in <paramref name="pairs" /> under the category after it, as a program would say it, and shows the tab again.</summary>
    static void Put(Screen screen, params string[] pairs)
    {
        for (var i = 0; i + 1 < pairs.Length; i += 2)
            Sequence(screen, pairs[i]).Category = pairs[i + 1];

        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);
    }

    static Sequence Sequence(Screen screen, string name) => screen.Editor.Program.Systems[0].Sequences.Single(sequence => sequence.Name == name);

    /// <summary>The sequences of the logic system by name, in the program's order.</summary>
    static string[] Order(Screen screen) => screen.Editor.Program.Systems[0].Sequences.Select(sequence => sequence.Name).ToArray();

    /// <summary>The names of the categories with no sequence under them.</summary>
    static string[] Empty(Screen screen) => screen.Editor.Program.Systems[0].EmptySequenceCategories.Select(empty => empty.Name).ToArray();

    /// <summary>The rows of the Sequences tab, a sequence by its name and a category by its name after "# ".</summary>
    static string[] Rows(Screen screen) => ((LogicSystemPage)screen.Editor.Current!).SequenceRows.Select(Said).ToArray();

    static string Said(Element row) => row is CategoryRow category ? "# " + category.Name : row is CategoryEndRow end ? "+ " + end.Category : ((StatusRow)row).Summary;

    static Element Row(Screen screen, string said) => ((LogicSystemPage)screen.Editor.Current!).SequenceRows.Single(row => Said(row) == said);

    static CategoryRow Category(Screen screen, string name) => (CategoryRow)Row(screen, "# " + name);

    static FieldSet Settling(Screen screen) =>
        ((LogicSystemPage)screen.Editor.Current!).CategorySettling ?? throw new Xunit.Sdk.XunitException("no panel is open");

    static Button Done(Screen screen) => Settling(screen).Parent!.Within().OfType<Button>().Single(button => button.Text == "Done");

    static void Open(Screen screen, string category)
    {
        Category(screen, category).Renames!.Execute();
        screen.Run(2);
    }

    /// <summary>Opens the category's panel, gives it <paramref name="name" /> where that is not null and one at a time as <paramref name="on" /> says, and presses Done.</summary>
    static void Settle(Screen screen, string category, string? name, bool on)
    {
        Open(screen, category);
        var form = Settling(screen);
        if (name != null)
            form.Fields[0].Text = name;

        form.Fields[1].Text = on ? "on" : "off";
        Done(screen).Command!.Execute();
        screen.Run(5);
    }

    static void AddCategory(Screen screen)
    {
        var page = (LogicSystemPage)screen.Editor.Current!;
        page.View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd) && button.IsVisible
            && button.Parent?.Parent?.IsVisible != false && Shown(button)).Command!.Execute();
        screen.Run(5);

        // A new category opens its panel on its name; Done keeps the name it was given.
        Assert.Equal("Category 1", Settling(screen).Fields[0].Text);
        Done(screen).Command!.Execute();
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

    /// <summary>
    /// Takes <paramref name="row" /> by its grip and lets it go over the top or bottom edge of <paramref name="onto" />, as
    /// <paramref name="below" /> says, carried <paramref name="across" /> to the right on the way.
    /// </summary>
    static void Carry(Screen screen, Element row, Element onto, bool below, float across = 0f)
    {
        var grip = (Element)((IReorderable)row).DragHandle;
        var from = grip.ToScreen(grip.Bounds.Center);
        var edge = onto.ToScreen(new Vector2(onto.Bounds.X, below ? onto.Bounds.Bottom - 2f : onto.Bounds.Y + 2f));
        var down = row == onto ? 0f : edge.Y - row.ToScreen(row.Bounds.Center).Y + (below ? row.Bounds.Height / 2f : -row.Bounds.Height / 2f);
        var to = new Vector2(from.X + across * Scale(row), from.Y + down);

        screen.Scene.MoveTo(from);
        screen.Scene.Down(from);
        screen.Run(1);
        screen.Scene.Move(new Vector2((from.X + to.X) / 2f, (from.Y + to.Y) / 2f));
        screen.Run(1);
        screen.Scene.Move(to);
        screen.Run(1);
        screen.Scene.Up();
        screen.Run(5);
    }

    static float Scale(Element row) => (row.ToScreen(new Vector2(1f, 0f)) - row.ToScreen(Vector2.Zero)).X;
}
