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
        Assert.False(part.Optional);
    }

    /// <summary>Making a slot required is a step back of its own; a trigger slot is made optional.</summary>
    [Fact]
    public void MakingASlotRequiredIsOneStepBack()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        Assert.True(part.Optional);

        editor.Parts()[0].Required.Command!.Execute();
        screen.Run(5);
        Assert.False(part.Optional);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.True(part.Optional);
    }


    /// <summary>A blueprint just made from a logic system with a button, its configuration panel open to be set up.</summary>
    static Screen Writing()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.GoToRoot();
        screen.Run(5);

        editor.RootRows()[0].Offers!.Execute();
        screen.Run(5);
        Assert.True(editor.Blueprint().IsEditing);
        return screen;
    }
}
