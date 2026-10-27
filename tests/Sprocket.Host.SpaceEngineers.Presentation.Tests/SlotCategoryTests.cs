using Lumel;
using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The installation panel stands a logic system's slots in the program's order, each run of slots under one category headed
/// by its name; slots and categories are carried by their grips, a slot into or out of a category by how far across it goes,
/// each change one step back.
/// </summary>
public class SlotCategoryTests
{
    /// <summary>A new category is named by how many there are, stands under the slots with none under it, and goes again as it is undone.</summary>
    [Fact]
    public void ANewCategoryStandsUnderTheSlots()
    {
        var screen = Slots();
        var editor = screen.Editor;

        AddCategory(screen);
        AddCategory(screen);

        Assert.Equal(new[] { "slot 1", "slot 2", "slot 3", "# Category 1", "# Category 2" }, Rows(screen));
        Assert.Equal(new[] { "Category 1", "Category 2" }, Empty(screen));

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "Category 1" }, Empty(screen));
    }

    /// <summary>A slot carried right under a category's name goes into it, stands there in the program's order, and one step back takes it out again.</summary>
    [Fact]
    public void ASlotCarriedRightUnderACategoryIsInIt()
    {
        var screen = Slots();
        var editor = screen.Editor;
        AddCategory(screen);

        Carry(screen, Row(screen, "slot 1"), Row(screen, "# Category 1"), true, Sizes.Indent);

        Assert.Equal(new[] { "slot 2", "slot 3", "# Category 1", "slot 1" }, Rows(screen));
        Assert.Equal("Category 1", Part(screen, "slot 1").Category);
        Assert.Equal(new[] { "slot 2", "slot 3", "slot 1" }, Order(screen));
        Assert.Empty(Empty(screen));

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "slot 1", "slot 2", "slot 3", "# Category 1" }, Rows(screen));
        Assert.Equal("", Part(screen, "slot 1").Category);
    }

    /// <summary>A slot carried straight down under a category with nothing in it stays out of it, under it.</summary>
    [Fact]
    public void ASlotCarriedStraightUnderAnEmptyCategoryStaysOut()
    {
        var screen = Slots();
        AddCategory(screen);

        Carry(screen, Row(screen, "slot 1"), Row(screen, "# Category 1"), true);

        Assert.Equal(new[] { "slot 2", "slot 3", "# Category 1", "slot 1" }, Rows(screen));
        Assert.Equal("", Part(screen, "slot 1").Category);
        Assert.Equal(new[] { "Category 1" }, Empty(screen));
    }

    /// <summary>A slot dropped among the slots under no category changes its place among them.</summary>
    [Fact]
    public void ASlotDroppedAmongTheOthersChangesItsPlace()
    {
        var screen = Slots();

        Carry(screen, Row(screen, "slot 3"), Row(screen, "slot 1"), false);

        Assert.Equal(new[] { "slot 3", "slot 1", "slot 2" }, Rows(screen));
    }

    /// <summary>A category stands where its first slot stands in the program, with slots under no category above and below it.</summary>
    [Fact]
    public void ACategoryStandsAmongTheSlotsWithNone()
    {
        var screen = Slots();
        Put(screen, "slot 2", "Doors");

        Assert.Equal(new[] { "slot 1", "# Doors", "slot 2", "slot 3" }, Rows(screen));
    }

    /// <summary>Two runs of slots under the same name are two categories, each with its own name over it.</summary>
    [Fact]
    public void TwoRunsUnderOneNameAreTwoCategories()
    {
        var screen = Slots();
        Put(screen, "slot 1", "Doors", "slot 3", "Doors");

        Assert.Equal(new[] { "# Doors", "slot 1", "slot 2", "# Doors", "slot 3" }, Rows(screen));
    }

    /// <summary>The last slot of a category carried left in its place leaves the category, standing under it.</summary>
    [Fact]
    public void ALastSlotCarriedLeftLeavesItsCategory()
    {
        var screen = Slots();
        Put(screen, "slot 2", "Doors", "slot 3", "Doors");

        Carry(screen, Row(screen, "slot 3"), Row(screen, "slot 3"), false, -Sizes.Indent);

        Assert.Equal(new[] { "slot 1", "# Doors", "slot 2", "slot 3" }, Rows(screen));
        Assert.Equal("", Part(screen, "slot 3").Category);
    }

    /// <summary>A slot under a category carried right in its place joins the category above it.</summary>
    [Fact]
    public void ASlotCarriedRightJoinsTheCategoryAboveIt()
    {
        var screen = Slots();
        Put(screen, "slot 2", "Doors");

        Carry(screen, Row(screen, "slot 3"), Row(screen, "slot 3"), false, Sizes.Indent);

        Assert.Equal(new[] { "slot 1", "# Doors", "slot 2", "slot 3" }, Rows(screen));
        Assert.Equal("Doors", Part(screen, "slot 3").Category);
    }

    /// <summary>A slot dropped between two slots of a category is in it, however far across it is carried.</summary>
    [Fact]
    public void ASlotDroppedInsideACategoryIsInIt()
    {
        var screen = Slots();
        Put(screen, "slot 1", "Doors", "slot 2", "Doors");

        Carry(screen, Row(screen, "slot 3"), Row(screen, "slot 2"), false, -Sizes.Indent);

        Assert.Equal(new[] { "# Doors", "slot 1", "slot 3", "slot 2" }, Rows(screen));
        Assert.Equal("Doors", Part(screen, "slot 3").Category);
    }

    /// <summary>The only slot of a category carried out of it leaves the category standing empty where it was.</summary>
    [Fact]
    public void ACategoryLeftWithNoSlotsStandsEmpty()
    {
        var screen = Slots();
        Put(screen, "slot 2", "Doors");

        Carry(screen, Row(screen, "slot 2"), Row(screen, "slot 1"), false);

        Assert.Equal(new[] { "slot 2", "slot 1", "# Doors", "slot 3" }, Rows(screen));
        Assert.Equal(new[] { "Doors" }, Empty(screen));
    }

    /// <summary>A category carried takes its slots with it, and lands only where it stands whole: never inside another.</summary>
    [Fact]
    public void ACategoryCarriesItsSlots()
    {
        var screen = Slots();
        Put(screen, "slot 1", "Calls", "slot 2", "Doors");

        Assert.Equal(new[] { "# Calls", "slot 1", "# Doors", "slot 2", "slot 3" }, Rows(screen));

        Carry(screen, Row(screen, "# Calls"), Row(screen, "slot 3"), true);

        Assert.Equal(new[] { "# Doors", "slot 2", "slot 3", "# Calls", "slot 1" }, Rows(screen));
        Assert.Equal(new[] { "slot 2", "slot 3", "slot 1" }, Order(screen));
    }

    /// <summary>A category removed leaves its slots where they stand, under no category, and one step back puts them under it again.</summary>
    [Fact]
    public void RemovingACategoryKeepsItsSlots()
    {
        var screen = Slots();
        var editor = screen.Editor;
        Put(screen, "slot 2", "Doors");

        Category(screen, "Doors").Removes!.Execute();
        screen.Run(5);

        Assert.Equal(new[] { "slot 1", "slot 2", "slot 3" }, Rows(screen));
        Assert.Equal("", Part(screen, "slot 2").Category);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(new[] { "slot 1", "# Doors", "slot 2", "slot 3" }, Rows(screen));
    }

    /// <summary>Pressing a category's name lets it be typed; Enter renames it, and its slots stay under it.</summary>
    [Fact]
    public void PressingTheNameRenamesTheCategory()
    {
        var screen = Slots();
        Put(screen, "slot 2", "Doors");

        Type(screen, "Doors", "Calls", MyKeys.Enter);

        Assert.Equal(new[] { "slot 1", "# Calls", "slot 2", "slot 3" }, Rows(screen));
        Assert.Equal("Calls", Part(screen, "slot 2").Category);
    }

    /// <summary>A category may take another's name, and the two stay two.</summary>
    [Fact]
    public void ACategoryMayTakeAnothersName()
    {
        var screen = Slots();
        AddCategory(screen);
        AddCategory(screen);

        Type(screen, "Category 1", "Category 2", MyKeys.Enter);

        Assert.Equal(new[] { "Category 2", "Category 2" }, Empty(screen));
    }

    /// <summary>An empty name is refused as a heading's is: Enter keeps the box, Escape lets it go, and the category keeps its name.</summary>
    [Fact]
    public void AnEmptyNameIsRefused()
    {
        var screen = Slots();
        AddCategory(screen);

        Type(screen, "Category 1", "   ", MyKeys.Enter);
        Assert.True(Category(screen, "Category 1").Plate.IsRenaming, "the box was put away");

        screen.Editor.Input!.Key(MyKeys.Escape, false, false);
        screen.Run(5);

        Assert.Equal(new[] { "Category 1" }, Empty(screen));
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

        Assert.Equal(new[] { "Category 1", "Category 2" }, Empty(screen));
        Assert.False(Category(screen, "Category 1").Plate.IsRenaming, "the box is still showing");
    }

    /// <summary>A category with no slot under it is not written into the program, and one with slots is written on them.</summary>
    [Fact]
    public void OnlyTheSlotsSayTheirCategories()
    {
        var screen = Slots();
        Put(screen, "slot 2", "Doors");
        AddCategory(screen);

        var written = ProgramWriter.Save(screen.Editor.Program, screen.Editor.World.Values);

        Assert.Contains("#@ category: \"Doors\"", written);
        Assert.DoesNotContain("Category 1", written);
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
        Assert.Equal(screen.Editor.Texts.Codex.SystemTurnOffToEdit, category.Grip.Tip?.Text);
    }

    /// <summary>An installation's own panel stands its slots the same way, under the names of the categories that hold any, with nothing to carry.</summary>
    [Fact]
    public void AnInstallationsPanelShowsTheCategories()
    {
        var screen = Slots();
        var system = screen.Editor.Program.Systems[0];
        system.EmptyCategories.Add(new EmptyCategory("Empty", null));
        Put(screen, "slot 2", "Doors");
        screen.Editor.GoToRoot();
        screen.Run(5);
        screen.Editor.RootRows()[0].Opens!.Execute();
        screen.Run(5);

        var rows = screen.Editor.Blueprint().Parts.Select(Said).ToArray();
        Assert.Equal(new[] { "slot 1", "# Doors", "slot 2", "slot 3" }, rows);
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

    /// <summary>Puts each slot named in <paramref name="pairs" /> under the category after it, as a program would say it, and shows the panel again.</summary>
    static void Put(Screen screen, params string[] pairs)
    {
        for (var i = 0; i + 1 < pairs.Length; i += 2)
            Part(screen, pairs[i]).Category = pairs[i + 1];

        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(5);
    }

    static Part Part(Screen screen, string name) => screen.Editor.Program.Systems[0].FindPart(name)!;

    /// <summary>The slots of the logic system by name, in the program's order.</summary>
    static string[] Order(Screen screen) => screen.Editor.Program.Systems[0].Parts.Select(part => part.Id).ToArray();

    /// <summary>The names of the categories with no slot under them.</summary>
    static string[] Empty(Screen screen) => screen.Editor.Program.Systems[0].EmptyCategories.Select(empty => empty.Name).ToArray();

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

    /// <summary>
    /// Takes <paramref name="row" /> by its grip and lets it go over the top or bottom edge of <paramref name="onto" />, as
    /// <paramref name="below" /> says, carried <paramref name="across" /> to the right on the way.
    /// </summary>
    static void Carry(Screen screen, Element row, Element onto, bool below, float across = 0f)
    {
        var grip = (Element)((ICarried)row).Grip;
        var from = grip.ToScreen(grip.Bounds.Center);
        var edge = onto.ToScreen(new Vector2(onto.Bounds.X, below ? onto.Bounds.Bottom - 2f : onto.Bounds.Y + 2f));

        // The grip's middle lands where the carried row's middle should cross the edge; a row dropped on itself stays put.
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

    /// <summary>How many screen units one of <paramref name="row" />'s own units is.</summary>
    static float Scale(Element row) => (row.ToScreen(new Vector2(1f, 0f)) - row.ToScreen(Vector2.Zero)).X;

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
