using Sprocket.Host.SpaceEngineers.Features.Installations;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Writing a blueprint's slots and description: what is typed reaches the blueprint once the keyboard leaves the box, as one step back.</summary>
public class BlueprintWritingTests
{
    /// <summary>A slot made by a pick takes the next numbered slot name, and is listed so on its logic system's screen.</summary>
    [Fact]
    public void ASlotTakesTheNextNumberedName()
    {
        var screen = OnTheSystem();
        var part = Assert.Single(screen.Editor.Program.Systems[0].Parts);

        Assert.Equal("signal slot 1", part.Id);
        Assert.Equal("signal slot 1", Assert.Single(screen.Editor.SystemSlots()).Name);
    }

    /// <summary>A slot renamed in its panel is renamed wherever its system names it, and one step back takes it back.</summary>
    [Fact]
    public void ASlotRenamedIsOneStepBack()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        var was = part.Id;

        var panel = editor.Asked();
        panel.Form.Fields[0].Text = "Call button";
        panel.Done();
        screen.Run(5);
        Assert.Equal("Call button", part.Id);
        Assert.NotNull(system.First!.FilledWith("Call button"));

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(was, part.Id);
    }

    /// <summary>A description typed into a slot's panel is the slot's once the panel is done, and one step back takes it back.</summary>
    [Fact]
    public void ADescriptionTypedIsTheSlotsOnceThePanelIsDone()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        var panel = editor.Asked();
        editor.Input.Focus(panel.About().Box);
        screen.Do("type", "The button that cycles it.").Run(1);
        panel.Done();
        screen.Run(5);
        Assert.Equal("The button that cycles it.", part.Annotation);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal("", part.Annotation);
    }

    /// <summary>A description typed into a panel that is then put away is not the slot's.</summary>
    [Fact]
    public void APanelPutAwayKeepsNothingTypedInIt()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        var panel = editor.Asked();
        editor.Input.Focus(panel.About().Box);
        screen.Do("type", "The button that cycles it.").Run(1);
        panel.Finish(true);
        screen.Run(5);
        Assert.Equal("", part.Annotation);
    }

    /// <summary>A short name typed into a signal slot's panel is the slot's once the panel is done, and undo takes it back.</summary>
    [Fact]
    public void AShortNameTypedIsKeptAndUndone()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        Assert.True(part.IsSignal);

        var panel = editor.Asked();
        Assert.Equal(2, panel.Form.Count);
        panel.Form.Fields[1].Text = "Cycle";
        panel.Done();
        screen.Run(5);
        Assert.Equal("Cycle", part.Short);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal("", part.Short);
    }

    /// <summary>A slot's name is refused where a slot nothing uses has it, in any case: the panel says why and stays, and both slots stay as they were.</summary>
    [Fact]
    public void ANameASlotNothingUsesHasIsRefused()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        var spare = system.PartFor("Spare");
        editor.Invalidate();
        screen.Run(5);
        editor.OpenSlot(part.Id);
        screen.Run(5);

        var panel = editor.Asked();
        panel.Form.Fields[0].Text = "spare";
        panel.Done();
        screen.Run(5);

        Assert.True(panel.IsOpen);
        Assert.Equal(editor.Texts.RenameTaken, panel.Why);
        Assert.Equal("signal slot 1", part.Id);
        Assert.Equal(new[] { part, spare }, system.Parts);
        Assert.Equal("Spare", spare.Id);
    }

    /// <summary>A slot's name is refused where another slot in use has it, in any case, and the slot keeps the name it had.</summary>
    [Fact]
    public void ANameAnotherSlotHasIsRefused()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        system.PartFor("Other");
        var close = new Framework.Model.Act("close");
        close.Targets.Add("Other");
        editor.Program.AddSequence(system).Steps.Add(close);
        editor.Invalidate();
        screen.Run(5);

        editor.OpenSlot(part.Id);
        screen.Run(5);
        var panel = editor.Asked();
        panel.Form.Fields[0].Text = "OTHER";
        panel.Done();
        screen.Run(5);

        Assert.True(panel.IsOpen);
        Assert.Equal(editor.Texts.RenameTaken, panel.Why);
        Assert.Equal("signal slot 1", part.Id);
    }

    /// <summary>A slot may take a state's or a type's name, and its own in another case.</summary>
    [Fact]
    public void ASlotMayShareAStatesOrATypesName()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        system.States.Add(new Framework.Model.State("Cycling", "", Framework.Model.Literal.Of(false)));
        system.Types.Add(new Framework.Model.DeclaredType("Side"));

        foreach (var name in new[] { "SIGNAL SLOT 1", "cycling", "SIDE" })
        {
            editor.OpenSlot(part.Id);
            screen.Run(5);
            var panel = editor.Asked();
            panel.Form.Fields[0].Text = name;
            panel.Done();
            screen.Run(5);

            Assert.False(panel.IsOpen, $"{name} was refused: {panel.Why}");
            Assert.Equal(name, part.Id);
        }
    }

    /// <summary>A slot's name left empty is refused, and the slot keeps the name it had.</summary>
    [Fact]
    public void AnEmptyNameIsRefused()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        var panel = editor.Asked();
        panel.Form.Fields[0].Text = "";
        panel.Done();
        screen.Run(5);

        Assert.True(panel.IsOpen);
        Assert.Equal("signal slot 1", part.Id);
    }

    /// <summary>A signal slot being filled in names the trigger on its block that answers it, not only the block.</summary>
    [Fact]
    public void ATriggerSlotNamesTheTriggerOnItsBlock()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];

        editor.GoToRoot();
        screen.Run(5);
        editor.Choose(system, system.Instances[0]);
        editor.Push(new BlueprintPage(editor, system));
        screen.Run(5);

        var row = Assert.Single(editor.Parts());
        Assert.Single(row.Blocks);
        Assert.Equal("button 1 is pressed", Assert.Single(row.Signals).Text);

        // The block and the signal are two parts, as a trigger's row shows them: the signal carries its mark.
        var chips = row.Within().OfType<SlotChip>().Where(chip => chip.IsVisible).ToList();
        Assert.Equal(2, chips.Count);
        Assert.Equal("Airlock Button", chips[0].Text);
        Assert.Contains("button 1 is pressed", chips[1].Text);
    }

    /// <summary>A blueprint's description is written on its logic system's installation panel, as one step back.</summary>
    [Fact]
    public void TheDescriptionIsWrittenOnTheLogicSystemsScreen()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var page = (Features.LogicSystems.LogicSystemPage)editor.Current!;
        editor.PressTab(editor.Texts.SystemInstallationPanel);
        screen.Run(2);

        editor.Input.Focus(page.About.Box);
        screen.Do("type", "Cycles the airlock.").Run(1);
        editor.Input.Focus(null);
        screen.Run(5);
        Assert.Equal("Cycles the airlock.", system.Annotation);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal("", system.Annotation);
    }

    /// <summary>The guide button beside each description box opens the guide to rich text in a window of its own.</summary>
    [Fact]
    public void TheGuideButtonOpensTheGuideToRichText()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        Guides(screen, editor.PressTab(editor.Texts.SystemInstallationPanel));

        editor.OpenSlot("signal slot 1");
        screen.Run(5);
        Guides(screen, editor.Asked().Window);
    }

    /// <summary>The first paste says, under the box pasted into, where text from outside the game comes in, and later ones do not.</summary>
    [Fact]
    public void TheFirstPasteSaysWhereOutsideTextComesIn()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        editor.Input.Clipboard = new Framework.Storage.ClipboardFile(new Folder(), delegate { }, editor.Pasted);
        var box = editor.Asked().About().Box;

        editor.Input.Focus(box);
        editor.Input.Key(VRage.Input.MyKeys.V, false, true);
        screen.Run(2);
        Assert.Equal(1, Notes(editor));

        editor.Windows!.Close(editor.Windows.Top);
        editor.Input.Focus(box);
        editor.Input.Key(VRage.Input.MyKeys.V, false, true);
        screen.Run(2);
        Assert.Equal(0, Notes(editor));
    }

    /// <summary>Presses the guide button of every description box in <paramref name="view" />, each opening the guide to rich text.</summary>
    static void Guides(Screen screen, Lumel.Element view)
    {
        var editor = screen.Editor;
        var composers = view.Within().OfType<Framework.Ui.Writing.Composer>().ToList();
        Assert.NotEmpty(composers);
        foreach (var composer in composers)
        {
            if (editor.Guide != null)
                editor.Windows!.Close(editor.Guide);

            composer.Guides!.Execute();
            screen.Run(2);

            Assert.True(editor.Guide != null && editor.Windows!.IsOpen(editor.Guide), "the guide did not open");
            Assert.True(screen.Shows("Rich text"), "the guide is not titled with its page's heading");
        }
    }

    /// <summary>How many windows are open saying where outside text comes in.</summary>
    static int Notes(SprocketEditor editor)
    {
        var count = 0;
        for (var i = 0; i < editor.Windows!.Count; i++)
        {
            if (editor.Windows[i].Within().OfType<Lumel.Text.Label>().Any(label => label.Text == editor.Texts.WritingPasteNote))
                count++;
        }

        return count;
    }

    /// <summary>The screen of a logic system with a button, switched off so it can be written.</summary>
    static Screen OnTheSystem()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.GoToRoot();
        screen.Run(5);

        editor.Program.Systems[0].Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        return screen;
    }

    /// <summary>The panel of the button's slot of a logic system switched off so it can be written, open.</summary>
    static Screen OnTheSlot()
    {
        var screen = OnTheSystem();
        screen.Editor.OpenSlot("signal slot 1");
        screen.Run(5);
        return screen;
    }
}
