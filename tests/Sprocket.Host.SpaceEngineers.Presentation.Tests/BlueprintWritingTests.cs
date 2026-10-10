using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Setting up a configuration panel: what is typed reaches the blueprint once the keyboard leaves the box, as one step back.</summary>
public class BlueprintWritingTests
{
    /// <summary>A slot is called what its block is called when the blueprint is made.</summary>
    [Fact]
    public void ASlotTakesItsBlocksName()
    {
        var screen = Writing();
        var part = Assert.Single(screen.Editor.Program.Systems[0].Parts);

        Assert.Equal("Airlock Button", part.Id);
        Assert.Equal("Airlock Button", screen.Editor.Parts()[0].NameBox.Text);
    }

    /// <summary>A slot renamed as the panel is set up is renamed wherever its system names it, and one step back takes it back.</summary>
    [Fact]
    public void ASlotRenamedIsOneStepBack()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        var was = part.Id;

        editor.Input.Focus(editor.Parts()[0].NameBox);
        screen.Do("clear", "type", "Call button").Run(1);
        Assert.Equal(was, part.Id);

        editor.Input.Focus(null);
        screen.Run(5);
        Assert.Equal("Call button", part.Id);
        Assert.NotNull(system.First!.FilledWith("Call button"));

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(was, part.Id);
        Assert.Equal(was, editor.Parts()[0].NameBox.Text);
    }

    /// <summary>A name typed into a slot's box is kept when the page is built again in the tick the box is left.</summary>
    [Fact]
    public void ANameLeftAsThePageIsBuiltAgainIsKept()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        editor.Input.Focus(editor.Parts()[0].NameBox);
        screen.Do("clear", "type", "Call button").Run(1);

        editor.Input.Focus(null);
        editor.Invalidate();
        screen.Run(5);

        Assert.Equal("Call button", part.Id);
        Assert.Equal("Call button", editor.Parts()[0].NameBox.Text);
    }

    /// <summary>A slot may take the name of one nothing uses, which is not saved and goes; the name of one in use is refused.</summary>
    [Fact]
    public void ANameHeldByASlotNothingUsesIsFree()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        system.PartFor("Spare");

        editor.Input.Focus(editor.Parts()[0].NameBox);
        screen.Do("clear", "type", "Spare").Run(1);
        editor.Input.Focus(null);
        screen.Run(5);

        Assert.Equal("Spare", part.Id);
        Assert.Same(part, Assert.Single(system.Parts));
        Assert.NotNull(system.First!.FilledWith("Spare"));
    }

    /// <summary>
    /// A slot's name is refused where another slot in use has it, said under the box while it is typed and again once the
    /// box is left, and the slot keeps the name it had.
    /// </summary>
    [Fact]
    public void ANameAnotherSlotHasIsRefusedAndSaysSo()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        system.PartFor("Other");
        var close = new Sequences.Act("close");
        close.Targets.Add("Other");
        editor.Program.AddSequence(system).Steps.Add(close);
        editor.Invalidate();
        screen.Run(5);

        var row = editor.Parts().Single(r => r.NameBox.Text == part.Id);
        editor.Input.Focus(row.NameBox);
        screen.Do("clear", "type", "Other").Run(1);
        Assert.Equal("Another slot is already called Other.", row.Refusal);

        editor.Input.Focus(null);
        screen.Run(5);

        Assert.Equal("Airlock Button", part.Id);
        row = editor.Parts().Single(r => r.NameBox.Text == part.Id);
        Assert.Equal("Not renamed: another slot is already called Other.", row.Refusal);
    }

    /// <summary>A slot's name left empty is refused and says so, and the slot keeps the name it had.</summary>
    [Fact]
    public void AnEmptyNameIsRefusedAndSaysSo()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        editor.Input.Focus(editor.Parts()[0].NameBox);
        screen.Do("clear", "type", "x").Run(1);
        Assert.Null(editor.Parts()[0].Refusal);
        screen.Do("clear").Run(1);
        Assert.Equal("A slot needs a name.", editor.Parts()[0].Refusal);

        editor.Input.Focus(null);
        screen.Run(5);

        Assert.Equal("Airlock Button", part.Id);
        Assert.Equal("Not renamed: a slot needs a name.", editor.Parts()[0].Refusal);
    }

    /// <summary>A signal slot being filled in names the trigger on its block that answers it, not only the block.</summary>
    [Fact]
    public void ATriggerSlotNamesTheTriggerOnItsBlock()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];

        editor.GoToRoot();
        screen.Run(5);
        editor.Choose(system, system.Instances[0]);
        editor.Push(new BlueprintPage(editor, system, false));
        screen.Run(5);

        var row = Assert.Single(editor.Parts());
        Assert.Single(row.Blocks);
        Assert.Equal("button 1 is pressed", Assert.Single(row.Signals).Text);

        // The block and the signal are two parts, as a trigger's row shows them: the signal carries its mark.
        var chips = row.Within().OfType<Slot>().Where(chip => chip.IsVisible).ToList();
        Assert.Equal(2, chips.Count);
        Assert.Equal("Airlock Button", chips[0].Text);
        Assert.Contains("button 1 is pressed", chips[1].Text);
    }

    /// <summary>A step back taken while a box holds the keyboard takes back what was typed in it, and nothing before it.</summary>
    [Fact]
    public void AStepBackWhileTypingTakesBackWhatWasTyped()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        editor.Parts()[0].Required.Command!.Execute();
        screen.Run(5);

        editor.Input.Focus(editor.Parts()[0].About.Box);
        screen.Do("type", "The button that cycles it.").Run(1);

        editor.Screen.Undo!.Execute();
        screen.Run(5);

        Assert.Equal("", part.Annotation);
        Assert.Equal("", editor.Parts()[0].About.Text);
        Assert.Null(editor.Input.Focused);
        Assert.True(part.Optional);
    }

    /// <summary>Making a slot optional is a step back of its own; every slot starts required, a signal slot too.</summary>
    [Fact]
    public void MakingASlotOptionalIsOneStepBack()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        Assert.False(part.Optional);

        editor.Parts()[0].Required.Command!.Execute();
        screen.Run(5);
        Assert.True(part.Optional);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.False(part.Optional);
    }

    /// <summary>The guide button beside each description box opens the codex on writing text.</summary>
    [Fact]
    public void TheGuideButtonOpensTheCodexOnWriting()
    {
        var screen = Writing();
        var editor = screen.Editor;

        var composers = editor.Blueprint().View.Within().OfType<Features.Writing.Composer>().ToList();
        Assert.NotEmpty(composers);
        foreach (var composer in composers)
        {
            if (editor.CodexOpen)
                editor.Windows!.Close(editor.CodexWindow!);

            composer.Guides!.Execute();
            screen.Run(2);

            Assert.True(editor.CodexOpen, "the codex did not open");
            Assert.Equal("writing", editor.CodexWindow!.Term!.Id);
        }
    }

    /// <summary>The first paste says, under the box pasted into, where text from outside the game comes in, and later ones do not.</summary>
    [Fact]
    public void TheFirstPasteSaysWhereOutsideTextComesIn()
    {
        var screen = Writing();
        var editor = screen.Editor;
        editor.Input.Clipboard = new Storage.ClipboardFile(new Folder(), delegate { }, editor.Pasted);
        var box = editor.Blueprint().View.Within().OfType<Features.Writing.Composer>().First().Box;

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

    /// <summary>How many windows are open saying where outside text comes in.</summary>
    static int Notes(Features.Shell.SprocketEditor editor)
    {
        var count = 0;
        for (var i = 0; i < editor.Windows!.Count; i++)
        {
            if (editor.Windows[i].Within().OfType<Lumel.Text.Label>().Any(label => label.Text == editor.Texts.WritingPasteNote))
                count++;
        }

        return count;
    }

    /// <summary>A logic system with a button, its configuration panel open to be set up.</summary>
    static Screen Writing()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.GoToRoot();
        screen.Run(5);

        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        editor.EditPanel();
        screen.Run(5);
        Assert.True(editor.Blueprint().IsEditing);
        return screen;
    }
}
