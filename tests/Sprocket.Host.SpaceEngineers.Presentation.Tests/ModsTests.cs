using Sprocket.Host.SpaceEngineers.Features.Mods;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Controls;
using Lumel.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An if, or an otherwise if, that checks whether a mod is installed is made and changed from the selector's Mods source.</summary>
public class ModsTests : IDisposable
{
    static readonly string Head = $"sprocket {ScriptFormat.Version} alpha {ScriptFormat.Draft}\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n";

    const string Empty = "    sequence 1 {\n    }\n}\n";

    public ModsTests()
    {
        HostNamespaces.Clear();
        HostNamespaces.Register("gearbox.lifts", "Gearbox", "Gearbox Lifts", "Ada Gear");
        HostNamespaces.Register("northwind.pumps", "Northwind", "Pump Station", "Bo North");
    }

    public void Dispose() => HostNamespaces.Clear();

    /// <summary>The selector an if is made with offers Mods, listing each registered namespace by its mod's title and author, and not Sprocket's own.</summary>
    [Fact]
    public void AnIfOffersTheModsByTitleAndAuthor()
    {
        var screen = OnSequence(Head + Empty, out _);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.IfWord);
        screen.Run(2);
        Assert.Contains(editor.Texts.PickSourceMods, Offered(editor));

        editor.ChooseSource(editor.Texts.PickSourceMods);
        screen.Run(2);

        var page = Assert.IsType<ModsPage>(editor.Current);
        var rows = page.List.Entries();
        Assert.Equal(new[] { "Gearbox Lifts", "Pump Station" }, rows.Select(row => row.Text));
        Assert.Equal(new[] { "Ada Gear", "Bo North" }, rows.Select(row => row.Secondary));
        Assert.Equal(editor.Texts.PickSourceMods, editor.Source());
    }

    /// <summary>A step that does something is not offered Mods.</summary>
    [Fact]
    public void ATaskIsNotOfferedMods()
    {
        var screen = OnSequence(Head + Empty, out _);
        var editor = screen.Editor;

        editor.PressAdd("task");
        screen.Run(2);

        Assert.DoesNotContain(editor.Texts.PickSourceMods, Offered(editor));
    }

    /// <summary>The list of mods is searched by title, author and namespace.</summary>
    [Fact]
    public void TheModsAreSearchable()
    {
        var screen = OnSequence(Head + Empty, out _);
        var editor = screen.Editor;
        OpenMods(screen);
        var list = Assert.IsType<ModsPage>(editor.Current).List;

        list.Search("pump");
        screen.Run(2);
        Assert.Equal(new[] { "Pump Station" }, list.Entries().Select(row => row.Text));

        list.Search("ada");
        screen.Run(2);
        Assert.Equal(new[] { "Gearbox Lifts" }, list.Entries().Select(row => row.Text));

        list.Search("northwind");
        screen.Run(2);
        Assert.Equal(new[] { "Pump Station" }, list.Entries().Select(row => row.Text));

        list.Search("nothing like it");
        screen.Run(2);
        Assert.Empty(list.Entries());
    }

    /// <summary>A mod picked with is installed, and Done, makes an if that tests it is installed.</summary>
    [Fact]
    public void AnIfIsMadeInstalled()
    {
        var screen = OnSequence(Head + Empty, out var sequence);
        var editor = screen.Editor;
        OpenMods(screen);

        Assert.Equal("If …", editor.Screen.Title());
        Assert.False(editor.Current.CanPrimary);
        Pick(screen, "Gearbox Lifts");
        Assert.Equal("If gearbox.lifts is installed", editor.Screen.Title());

        editor.PressPrimary();
        screen.Run(2);

        var test = Assert.IsType<HostTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("gearbox.lifts", test.Namespace);
        Assert.True(test.Installed);
        Assert.IsType<EndIf>(sequence.Steps[1]);
        Assert.Contains("if gearbox.lifts is installed then", ProgramWriter.Save(editor.Program, new Values()));
    }

    /// <summary>A mod picked with is not installed makes an if that tests it is not installed.</summary>
    [Fact]
    public void AnIfIsMadeNotInstalled()
    {
        var screen = OnSequence(Head + Empty, out var sequence);
        var editor = screen.Editor;
        OpenMods(screen);

        Pick(screen, "Pump Station");
        Choose(screen, SentenceText.NotInstalledWords);
        Assert.Equal("If northwind.pumps is not installed", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(2);

        var test = Assert.IsType<HostTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("northwind.pumps", test.Namespace);
        Assert.False(test.Installed);
        Assert.Contains("if northwind.pumps is not installed then", ProgramWriter.Save(editor.Program, new Values()));
    }

    /// <summary>An otherwise if is made from the same selector, and a double press on a mod finishes.</summary>
    [Fact]
    public void AnOtherwiseIfIsMadeFromMods()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if gearbox.lifts is installed then {\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        var end = editor.StepRows().Single(row => row.Kind == StepKind.End);
        Assert.Single(end.Adds).Choices!.Single(choice => choice.Text == SentenceText.OtherwiseIfWords).Adds!.Execute();
        screen.Run(2);
        editor.ChooseSource(editor.Texts.PickSourceMods);
        screen.Run(2);
        Assert.IsType<ModsPage>(editor.Current).List.Entries().Single(row => row.Text == "Pump Station").Pick(HeldModifier.Alone, 2);
        screen.Run(2);

        var test = Assert.IsType<HostTest>(Assert.IsType<Else>(sequence.Steps[1]).Test);
        Assert.Equal("northwind.pumps", test.Namespace);
        Assert.True(test.Installed);
    }

    /// <summary>The words of an existing test of the host are pressed to open the selector on Mods, its mod and choice chosen, and changing them replaces the test.</summary>
    [Fact]
    public void AnExistingTestOpensOnItsModAndChanges()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if gearbox.lifts is not installed then {\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        var slot = Assert.IsType<HostSlot>(Assert.Single(editor.StepRows()[0].Slots));
        slot.Opens!.Execute();
        screen.Run(2);

        var page = Assert.IsType<ModsPage>(editor.Current);
        Assert.Equal(editor.Texts.PickSourceMods, editor.Source());
        Assert.Equal("Gearbox Lifts", page.List.Entries().Single(row => row.IsChosen).Text);
        Assert.Equal(SentenceText.NotInstalledWords, page.Form.Fields[0].Text);
        Assert.Equal("If gearbox.lifts is not installed", editor.Screen.Title());

        Choose(screen, SentenceText.InstalledWords);
        Pick(screen, "Pump Station");
        editor.PressPrimary();
        screen.Run(2);

        var test = Assert.IsType<HostTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("northwind.pumps", test.Namespace);
        Assert.True(test.Installed);
        Assert.IsType<SequencePage>(editor.Current);
        Assert.Single(sequence.Steps.OfType<If>());
    }

    /// <summary>Choosing another source on an existing test of the host replaces it with a test of blocks.</summary>
    [Fact]
    public void AnExistingTestCanBecomeATestOfBlocks()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if gearbox.lifts is installed then {\n        }\n    }\n}\n", out _);
        var editor = screen.Editor;

        editor.StepRows()[0].Slots[0].Opens!.Execute();
        screen.Run(2);
        editor.ChooseSource(editor.Texts.PickSourceBlocks);
        screen.Run(2);

        Assert.IsType<PickPage>(editor.Current);
        Assert.NotEmpty(editor.Pick().Stages[0].Entries());
    }

    /// <summary>A namespace the program names that the host lacks is listed by the name it is written with, so its test can be opened and changed.</summary>
    [Fact]
    public void ANamespaceTheHostLacksIsListedAsWritten()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if absent.mod is installed then {\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        editor.StepRows()[0].Slots[0].Opens!.Execute();
        screen.Run(2);

        var page = Assert.IsType<ModsPage>(editor.Current);
        var rows = page.List.Entries();
        Assert.Equal(new[] { "Gearbox Lifts", "Pump Station", "absent.mod" }, rows.Select(row => row.Text));
        Assert.Equal("absent.mod", rows.Single(row => row.IsChosen).Text);

        Choose(screen, SentenceText.NotInstalledWords);
        editor.PressPrimary();
        screen.Run(2);

        var test = Assert.IsType<HostTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("absent.mod", test.Namespace);
        Assert.False(test.Installed);
    }

    /// <summary>With no mod registered, the list says there is nothing to choose from, and Done waits for a mod.</summary>
    [Fact]
    public void NoModsLeavesNothingToChoose()
    {
        HostNamespaces.Clear();
        var screen = OnSequence(Head + Empty, out _);
        var editor = screen.Editor;
        OpenMods(screen);

        var page = Assert.IsType<ModsPage>(editor.Current);
        Assert.Empty(page.List.Entries());
        Assert.False(editor.Current.CanPrimary);
    }

    static IEnumerable<string> Offered(SprocketEditor editor)
    {
        var texts = editor.Texts;
        var sources = new[] { texts.PickSourceBlocks, texts.PickSourceSlots, texts.PickSourceStates, texts.PickSourceMods };
        var box = Target.Within(editor.Pick().Stages[0]).OfType<ListField>().First(field => sources.Contains(field.Text));
        return box.Items.OfType<DropDownItem>().Select(item => item.Text).ToList();
    }

    static void OpenMods(Screen screen)
    {
        var editor = screen.Editor;
        editor.PressAdd(SentenceText.IfWord);
        screen.Run(2);
        editor.ChooseSource(editor.Texts.PickSourceMods);
        screen.Run(2);
    }

    static void Pick(Screen screen, string title)
    {
        screen.Editor.Pick().Stages[0].Entries().Single(row => row.Text == title).Pick(HeldModifier.Alone, 1);
        screen.Run(2);
    }

    static void Choose(Screen screen, string word)
    {
        var field = Assert.IsType<ModsPage>(screen.Editor.Current).Form.Fields[0];
        for (var i = 0; i < 4 && field.Text != word; i++)
        {
            ((IInputTarget)field).Text(word[0]);
            screen.Run(1);
        }

        Assert.Equal(word, field.Text);
    }

    static Screen OnSequence(string program, out Sequence sequence)
    {
        var screen = Screen.Holding(program);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new SequencePage(editor, sequence));
        screen.Run(1);
        return screen;
    }
}
