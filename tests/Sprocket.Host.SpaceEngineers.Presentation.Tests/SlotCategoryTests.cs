using Lumel;
using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The installation panel stands a logic system's slots in an order somebody chose, the ones under no category first and
/// then each category's name over its own; slots and categories are carried by their grips, each change one step back.
/// </summary>
public class SlotCategoryTests
{
    /// <summary>A new category is named by how many there are, stands under the slots, and goes again as it is undone.</summary>
    [Fact]
    public void ANewCategoryStandsUnderTheSlots()
    {
        var screen = Slots();
        var editor = screen.Editor;

        AddCategory(screen);
        AddCategory(screen);

        Assert.Equal(new[] { "slot 1", "slot 2", "slot 3", "# Category 1", "# Category 2" }, Rows(screen));
        Assert.Equal(new[] { "Category 1", "Category 2" }, editor.Program.Systems[0].Categories);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "Category 1" }, editor.Program.Systems[0].Categories);
    }

    /// <summary>A slot dropped under a category's name stands under it, and one step back takes it out again.</summary>
    [Fact]
    public void ASlotDroppedUnderACategoryIsInIt()
    {
        var screen = Slots();
        var editor = screen.Editor;
        AddCategory(screen);

        Carry(screen, Row(screen, "slot 1"), Row(screen, "# Category 1"), below: true);

        Assert.Equal(new[] { "slot 2", "slot 3", "# Category 1", "slot 1" }, Rows(screen));
        Assert.Equal("Category 1", editor.Program.Systems[0].FindPart("slot 1")!.Category);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "slot 1", "slot 2", "slot 3", "# Category 1" }, Rows(screen));
        Assert.Equal("", editor.Program.Systems[0].FindPart("slot 1")!.Category);
    }

    /// <summary>A slot dropped among the slots under no category changes its place among them.</summary>
    [Fact]
    public void ASlotDroppedAmongTheOthersChangesItsPlace()
    {
        var screen = Slots();

        Carry(screen, Row(screen, "slot 3"), Row(screen, "slot 1"), below: false);

        Assert.Equal(new[] { "slot 3", "slot 1", "slot 2" }, Rows(screen));
    }

    /// <summary>A category carried takes its slots with it, and lands only where it stands whole: never among the slots under none.</summary>
    [Fact]
    public void ACategoryCarriesItsSlots()
    {
        var screen = Slots();
        var system = screen.Editor.Program.Systems[0];
        system.Categories.Add("Doors");
        system.Categories.Add("Calls");
        system.FindPart("slot 2")!.Category = "Doors";
        system.FindPart("slot 1")!.Category = "Calls";
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);

        Assert.Equal(new[] { "slot 3", "# Doors", "slot 2", "# Calls", "slot 1" }, Rows(screen));

        Carry(screen, Row(screen, "# Calls"), Row(screen, "slot 3"), below: false);

        Assert.Equal(new[] { "slot 3", "# Calls", "slot 1", "# Doors", "slot 2" }, Rows(screen));
        Assert.Equal(new[] { "Calls", "Doors" }, system.Categories);
    }

    /// <summary>A category removed leaves its slots, under no category, and one step back puts them under it again.</summary>
    [Fact]
    public void RemovingACategoryKeepsItsSlots()
    {
        var screen = Slots();
        var editor = screen.Editor;
        AddCategory(screen);
        Carry(screen, Row(screen, "slot 1"), Row(screen, "# Category 1"), below: true);

        Category(screen, "Category 1").Removes!.Execute();
        screen.Run(5);

        Assert.Equal(new[] { "slot 2", "slot 3", "slot 1" }, Rows(screen));
        Assert.Empty(editor.Program.Systems[0].Categories);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "slot 2", "slot 3", "# Category 1", "slot 1" }, Rows(screen));
    }

    /// <summary>Pressing a category's name lets it be typed; Enter renames it, and its slots stay under it.</summary>
    [Fact]
    public void PressingTheNameRenamesTheCategory()
    {
        var screen = Slots();
        var editor = screen.Editor;
        AddCategory(screen);
        Carry(screen, Row(screen, "slot 1"), Row(screen, "# Category 1"), below: true);

        Type(screen, "Category 1", "Calls", MyKeys.Enter);

        Assert.Equal(new[] { "slot 2", "slot 3", "# Calls", "slot 1" }, Rows(screen));
        Assert.Equal("Calls", editor.Program.Systems[0].FindPart("slot 1")!.Category);
    }

    /// <summary>An empty name or another category's is refused as a heading's is: Enter keeps the box, Escape lets it go, and the category keeps its name.</summary>
    [Theory]
    [InlineData("   ")]
    [InlineData("Category 2")]
    public void ANameItCannotTakeIsRefused(string typed)
    {
        var screen = Slots();
        AddCategory(screen);
        AddCategory(screen);

        Type(screen, "Category 1", typed, MyKeys.Enter);
        Assert.True(Category(screen, "Category 1").Plate.IsRenaming, "the box was put away");

        screen.Editor.Input!.Key(MyKeys.Escape, false, false);
        screen.Run(5);

        Assert.Equal(new[] { "Category 1", "Category 2" }, screen.Editor.Program.Systems[0].Categories);
        Assert.False(Category(screen, "Category 1").Plate.IsRenaming, "the box is still showing");
    }

    /// <summary>A name let go of with Escape changes nothing.</summary>
    [Fact]
    public void EscapeChangesNothing()
    {
        var screen = Slots();
        AddCategory(screen);
        AddCategory(screen);

        Type(screen, "Category 1", "Calls", MyKeys.Escape);

        Assert.Equal(new[] { "Category 1", "Category 2" }, screen.Editor.Program.Systems[0].Categories);
        Assert.False(Category(screen, "Category 1").Plate.IsRenaming, "the box is still showing");
    }

    /// <summary>While the logic system is on, nothing is carried, added, renamed or removed, and each says why.</summary>
    [Fact]
    public void NothingMovesWhileTheSystemIsOn()
    {
        var screen = Slots();
        AddCategory(screen);
        screen.Editor.Program.Systems[0].Enabled = true;
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);

        var category = Category(screen, "Category 1");
        Assert.False(category.Grip.CanCarry);
        Assert.Null(category.Renames);
        Assert.Null(category.Removes);
        Assert.All(screen.Editor.SystemSlots(), row => Assert.False(row.Grip.CanCarry));

        var page = (LogicSystemPage)screen.Editor.Current!;
        var add = page.View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd));
        Assert.Null(add.Command);
        Assert.False(Shown(add), "the button to add a category shows");
        Assert.Equal(screen.Editor.Texts.SystemTurnOffToEdit, category.Grip.Tip?.Text);
    }

    /// <summary>An installation's own panel stands its slots the same way, under the names of the categories that hold any, with nothing to carry.</summary>
    [Fact]
    public void AnInstallationsPanelShowsTheCategories()
    {
        var screen = Slots();
        var system = screen.Editor.Program.Systems[0];
        system.Categories.Add("Doors");
        system.Categories.Add("Empty");
        system.FindPart("slot 2")!.Category = "Doors";
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Editor.GoToRoot();
        screen.Run(5);
        screen.Editor.RootRows()[0].Opens!.Execute();
        screen.Run(5);

        var rows = screen.Editor.Blueprint().Parts.Select(Said).ToArray();
        Assert.Equal(new[] { "slot 1", "slot 3", "# Doors", "slot 2" }, rows);
        Assert.All(screen.Editor.Blueprint().Parts.OfType<ICarried>(), row => Assert.False(row.Grip.IsVisible));
    }

    /// <summary>The screen, switched off to be written, of a button system whose sequence closes both airlock doors, the outer one first, on its installation panel.</summary>
    static Screen Slots()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        foreach (var door in new[] { "Airlock Outer Door", "Airlock Inner Door" })
        {
            editor.PressAdd("task");
            editor.PressBlock(door);
            editor.PressChoice("close");
            editor.PressPrimary();
            screen.Run(2);
        }

        editor.GoToRoot();
        screen.Run(5);
        editor.Program.Systems[0].Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        editor.PressTab(editor.Texts.SystemInstallationPanel);
        return screen.Run(5);
    }

    /// <summary>The rows of the installation panel, a slot by its name and a category by its name after "# ".</summary>
    static string[] Rows(Screen screen) => ((LogicSystemPage)screen.Editor.Current!).Slots.Select(Said).ToArray();

    static string Said(Element row) => row is CategoryRow category ? "# " + category.Name : ((PartRow)row).Name;

    /// <summary>The row of the installation panel that <see cref="Rows" /> says as <paramref name="said" />.</summary>
    static Element Row(Screen screen, string said) => ((LogicSystemPage)screen.Editor.Current!).Slots.Single(row => Said(row) == said);

    static CategoryRow Category(Screen screen, string name) => (CategoryRow)Row(screen, "# " + name);

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

    static void AddCategory(Screen screen)
    {
        var page = (LogicSystemPage)screen.Editor.Current!;
        page.View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd)).Command!.Execute();
        screen.Run(5);
    }

    /// <summary>Takes <paramref name="row" /> by its grip and lets it go over the top or bottom edge of <paramref name="onto" />, as <paramref name="below" /> says.</summary>
    static void Carry(Screen screen, Element row, Element onto, bool below)
    {
        var grip = (Element)((ICarried)row).Grip;
        var from = grip.ToScreen(grip.Bounds.Center);
        var edge = onto.ToScreen(new Vector2(onto.Bounds.X, below ? onto.Bounds.Bottom - 2f : onto.Bounds.Y + 2f));

        // The grip's middle lands where the carried row's middle should cross the edge.
        var to = new Vector2(from.X, from.Y + edge.Y - row.ToScreen(row.Bounds.Center).Y + (below ? row.Bounds.Height / 2f : -row.Bounds.Height / 2f));

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

    /// <summary>Presses the name of the category <paramref name="name" />, types <paramref name="typed" /> over it, and presses <paramref name="key" />.</summary>
    static void Type(Screen screen, string name, string typed, MyKeys key)
    {
        var row = Category(screen, name);
        row.Renames!.Execute();
        screen.Run(1);
        Assert.Same(row.Plate.Box, screen.Editor.Input!.Focused);

        row.Plate.Box.Insert(typed);
        screen.Editor.Input.Key(key, false, false);
        screen.Run(5);
    }
}
