using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Setting up a configuration panel: what is typed reaches the blueprint once the keyboard leaves the box, as one step back.</summary>
public class BlueprintWritingTests
{
    /// <summary>A slot renamed as the panel is set up takes the name as its annotation's label, and one step back takes it back.</summary>
    [Fact]
    public void ASlotRenamedIsOneStepBack()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        var was = part.Label;

        editor.Input.Focus(editor.Parts()[0].NameBox);
        screen.Do("clear", "type", "Call button").Run(1);
        Assert.Equal(was, part.Label);

        editor.Input.Focus(null);
        screen.Run(5);
        Assert.Equal("Call button", part.Label);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.Equal(was, part.Label);
        Assert.Equal(was, editor.Parts()[0].NameBox.Text);
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

        Assert.Equal("", part.About);
        Assert.Equal("", editor.Parts()[0].About.Text);
        Assert.Null(editor.Input.Focused);
        Assert.True(part.Optional);
    }

    /// <summary>Making a slot optional is a step back of its own.</summary>
    [Fact]
    public void MakingASlotOptionalIsOneStepBack()
    {
        var screen = Writing();
        var editor = screen.Editor;
        var part = Assert.Single(editor.Program.Systems[0].Parts);

        editor.Parts()[0].Required.Command!.Execute();
        screen.Run(5);
        Assert.True(part.Optional);

        editor.Screen.Undo!.Execute();
        screen.Run(5);
        Assert.False(part.Optional);
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
