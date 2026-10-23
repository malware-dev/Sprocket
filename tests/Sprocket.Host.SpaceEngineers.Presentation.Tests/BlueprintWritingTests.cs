using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
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

        Assert.Equal("slot 1", part.Id);
        Assert.Equal("slot 1", Assert.Single(screen.Editor.SystemSlots()).Name);
    }

    /// <summary>Pressing a slot's name opens its page, named for it.</summary>
    [Fact]
    public void PressingASlotsNameOpensIt()
    {
        var screen = OnTheSlot();

        Assert.Equal("slot 1", screen.Editor.Slot().Title);
    }

    /// <summary>A slot renamed on its heading is renamed wherever its system names it, and one step back takes it back.</summary>
    [Fact]
    public void ASlotRenamedIsOneStepBack()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        var was = part.Id;

        editor.Screen.Heading[0].Renames!("Call button");
        screen.Run(5);
        Assert.Equal("Call button", part.Id);
        Assert.NotNull(system.First!.FilledWith("Call button"));

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(was, part.Id);
        Assert.Equal(was, editor.Slot().Title);
    }

    /// <summary>A description typed into a slot's box is kept when the page is built again in the tick the box is left.</summary>
    [Fact]
    public void ADescriptionLeftAsThePageIsBuiltAgainIsKept()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        editor.Input.Focus(editor.Slot().About.Box);
        screen.Do("type", "The button that cycles it.").Run(1);

        editor.Input.Focus(null);
        editor.Invalidate();
        screen.Run(5);

        Assert.Equal("The button that cycles it.", part.Annotation);
        Assert.Equal("The button that cycles it.", editor.Slot().About.Text);
    }

    /// <summary>A short name typed into a signal slot's box is the slot's once the box is left, and undo takes it back.</summary>
    [Fact]
    public void AShortNameTypedIsKeptAndUndone()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        Assert.True(part.IsSignal);
        Assert.True(editor.Slot().Short.IsVisible);

        editor.Input.Focus(editor.Slot().Short);
        screen.Do("type", "Cycle").Run(1);
        editor.Input.Focus(null);
        screen.Run(5);
        Assert.Equal("Cycle", part.Short);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal("", part.Short);
        Assert.Equal("", editor.Slot().Short.Text);
    }

    /// <summary>A slot's name is refused where a slot nothing uses has it, in any case, and both slots stay as they were.</summary>
    [Fact]
    public void ANameASlotNothingUsesHasIsRefused()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        var spare = system.PartFor("Spare");

        Assert.False(editor.Slot().AcceptsName("spare"));
        editor.Screen.Heading[0].Renames!("Spare");
        screen.Run(5);

        Assert.Equal("slot 1", part.Id);
        Assert.Equal(new[] { part, spare }, system.Parts);
        Assert.Equal("Spare", spare.Id);
    }

    /// <summary>A slot's name is refused where another slot in use has it, and the slot keeps the name it had.</summary>
    [Fact]
    public void ANameAnotherSlotHasIsRefused()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var part = Assert.Single(system.Parts);
        system.PartFor("Other");
        var close = new Sequences.Act("close");
        close.Targets.Add("Other");
        editor.Program.AddSequence(system).Steps.Add(close);
        editor.Invalidate();
        screen.Run(5);

        editor.OpenSlot(part.Id);
        screen.Run(5);
        Assert.False(editor.Slot().AcceptsName("Other"));
        Assert.False(editor.Slot().AcceptsName("OTHER"));

        editor.Screen.Heading[0].Renames!("Other");
        screen.Run(5);
        Assert.Equal("slot 1", part.Id);
    }

    /// <summary>A slot's name is refused where a state or a type has it in any case, and the slot's own name in another case is not.</summary>
    [Fact]
    public void ANameAStateOrATypeHasIsRefused()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        system.States.Add(new Sequences.State("Cycling", "", World.Literal.Of(false)));
        system.Types.Add(new Sequences.DeclaredType("Side"));

        Assert.False(editor.Slot().AcceptsName("cycling"));
        Assert.False(editor.Slot().AcceptsName("SIDE"));
        Assert.True(editor.Slot().AcceptsName("Slot 1"));
    }

    /// <summary>A slot's name left empty is refused, and the slot keeps the name it had.</summary>
    [Fact]
    public void AnEmptyNameIsRefused()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        Assert.True(editor.Slot().NameRequired);
        editor.Screen.Heading[0].Renames!("");
        screen.Run(5);

        Assert.Equal("slot 1", part.Id);
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

    /// <summary>A step back taken while a box holds the keyboard takes back what was typed in it, and nothing before it.</summary>
    [Fact]
    public void AStepBackWhileTypingTakesBackWhatWasTyped()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        editor.Slot().Required.Command!.Execute();
        screen.Run(5);

        editor.Input.Focus(editor.Slot().About.Box);
        screen.Do("type", "The button that cycles it.").Run(1);

        editor.Screen.Undo!.Execute();
        screen.Run(5);

        Assert.Equal("", part.Annotation);
        Assert.Equal("", editor.Slot().About.Text);
        Assert.Null(editor.Input.Focused);
        Assert.True(part.Optional);
    }

    /// <summary>Making a slot optional is a step back of its own; every slot starts required, a signal slot too.</summary>
    [Fact]
    public void MakingASlotOptionalIsOneStepBack()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        Assert.False(part.Optional);

        editor.Slot().Required.Command!.Execute();
        screen.Run(5);
        Assert.True(part.Optional);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.False(part.Optional);
    }

    /// <summary>A blueprint's description is written at the top of its logic system's screen, as one step back.</summary>
    [Fact]
    public void TheDescriptionIsWrittenOnTheLogicSystemsScreen()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var page = (Features.LogicSystems.LogicSystemPage)editor.Current!;

        editor.Input.Focus(page.About.Box);
        screen.Do("type", "Cycles the airlock.").Run(1);
        editor.Input.Focus(null);
        screen.Run(5);
        Assert.Equal("Cycles the airlock.", system.Annotation);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal("", system.Annotation);
    }

    /// <summary>The guide button beside each description box opens the codex on writing text.</summary>
    [Fact]
    public void TheGuideButtonOpensTheCodexOnWriting()
    {
        var screen = OnTheSystem();
        var editor = screen.Editor;
        Guides(screen, editor.PressTab(editor.Texts.SystemInstallationPanel));

        editor.OpenSlot("slot 1");
        screen.Run(5);
        Guides(screen, editor.Slot().View);
    }

    /// <summary>The first paste says, under the box pasted into, where text from outside the game comes in, and later ones do not.</summary>
    [Fact]
    public void TheFirstPasteSaysWhereOutsideTextComesIn()
    {
        var screen = OnTheSlot();
        var editor = screen.Editor;
        editor.Input.Clipboard = new Storage.ClipboardFile(new Folder(), delegate { }, editor.Pasted);
        var box = editor.Slot().About.Box;

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

    /// <summary>Presses the guide button of every description box in <paramref name="view" />, each opening the codex on writing.</summary>
    static void Guides(Screen screen, Lumel.Element view)
    {
        var editor = screen.Editor;
        var composers = view.Within().OfType<Features.Writing.Composer>().ToList();
        Assert.NotEmpty(composers);
        foreach (var composer in composers)
        {
            if (editor.CodexOpen)
                editor.Windows!.Close(editor.CodexWindow!);

            composer.Guides!.Execute();
            screen.Run(2);

            Assert.True(editor.CodexOpen, "the codex did not open");
            Assert.Equal(Terms.Writing, editor.CodexWindow!.Term!.Id);
        }
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

    /// <summary>The page of the button's slot of a logic system switched off so it can be written.</summary>
    static Screen OnTheSlot()
    {
        var screen = OnTheSystem();
        screen.Editor.OpenSlot("slot 1");
        screen.Run(5);
        return screen;
    }
}
