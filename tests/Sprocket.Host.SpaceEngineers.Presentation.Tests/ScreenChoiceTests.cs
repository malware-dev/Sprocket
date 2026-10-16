using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A member of a block's screen is chosen with a drop box of the screens under its line, the choice kept by the installation.</summary>
public class ScreenChoiceTests
{
    /// <summary>Show text on a billboard opens a drop box of its screens under its line, on the first, saying what that one shows.</summary>
    [Fact]
    public void ShowTextOpensTheScreensOnTheFirst()
    {
        var (screen, _) = Picking();

        var facets = screen.Editor.Facets()!;
        Assert.Equal(new[] { "Front", "Back" }, facets.Names);
        Assert.Equal("Front", facets.Name);
        Assert.Equal("nothing", facets.Line);
    }

    /// <summary>The line says what the chosen screen shows, and the app by its name where it runs one.</summary>
    [Fact]
    public void TheLineNamesTheAppAScreenRuns()
    {
        var (screen, board) = Picking();
        board.Faces[1].Content = ContentType.SCRIPT;
        board.Faces[1].App = "TSS_ClockDigital";

        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);

        Assert.Equal("Back", screen.Editor.Facets()!.Name);
        Assert.Equal("app TSS_ClockDigital", screen.Editor.Facets()!.Line);
    }

    /// <summary>A member that is not a screen's opens no drop box.</summary>
    [Fact]
    public void AnotherMemberOpensNone()
    {
        var (screen, _) = Picking();

        screen.Editor.PressChoice("power");
        screen.Run(2);

        Assert.Null(screen.Editor.Facets());
    }

    /// <summary>The step acts on a slot of screens of its own, which the installation fills with the screen chosen.</summary>
    [Fact]
    public void TheStepsSlotHoldsTheScreenChosen()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);

        Finish(screen, "Docked");

        var system = screen.Editor.Program.Systems[0];
        var act = Assert.IsType<Act>(system.Sequences[0].Steps[0]);
        var slot = Assert.Single(act.Targets);
        Assert.Equal(HostTypes.Returned("screen")!.Id, system.FindPart(slot)!.Holds);
        Assert.Equal(new[] { 2 }, system.First.Facets(slot));
    }

    /// <summary>Taking the step back takes its slot of screens with it, and changing the step to another screen and back again keeps it.</summary>
    [Fact]
    public void UndoTakesTheScreenSlotBack()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");
        var system = screen.Editor.Program.Systems[0];
        var slot = ((Act)system.Sequences[0].Steps[0]).Targets[0];

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);
        screen.Editor.Facets()!.Choose("Front");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);
        Assert.Equal(new[] { 1 }, system.First.Facets(slot));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(new[] { 2 }, system.First.Facets(slot));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Null(system.FindPart(slot));
        Assert.Null(system.First.Facets(slot));
    }

    /// <summary>Another installation fills the slot of screens on the installation panel: the step's member alone, the screen chosen under it, and no values asked.</summary>
    [Fact]
    public void AnotherInstallationChoosesItsOwnScreen()
    {
        var (screen, _) = Picking();
        Finish(screen, "Docked");
        var system = screen.Editor.Program.Systems[0];
        var slot = ((Act)system.Sequences[0].Steps[0]).Targets[0];
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.AddInstallation(0);
        screen.Run(2);

        screen.Editor.Parts().Single(row => row.Name == slot).Fills!.Execute();
        screen.Run(2);
        screen.Editor.PressBlock("Status Board");
        screen.Run(2);
        Assert.Equal(new[] { "show text" }, screen.Editor.Pick().Stages.Last().Entries().Where(row => !row.IsHeading).Select(row => row.Text));
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Assert.Null(screen.Editor.Pick().Arguments);
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(new[] { 1 }, system.Instances[0].Facets(slot));
        Assert.Equal(new[] { 2 }, system.Instances[1].Facets(slot));
    }

    /// <summary>A check of a screen's setting chooses its screen the same way, and checks it on a slot of screens of its own.</summary>
    [Fact]
    public void ACheckOfAScreensSettingHoldsItsScreen()
    {
        var rig = Rig.Demo();
        rig.Weld(new Billboard(40, "Status Board"));
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd(SentenceText.IfWord);
        screen.Run(2);
        screen.Editor.PressBlock("Status Board");
        screen.Run(2);
        screen.Editor.PressChoice("font size");
        screen.Run(2);
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);

        var form = screen.Editor.Pick().Arguments!.Form;
        form.Fields[form.Fields.Count - 1].Text = "2";
        screen.Editor.PressPrimary();
        screen.Run(2);

        var system = screen.Editor.Program.Systems[0];
        var slot = Assert.Single(Assert.IsType<If>(system.Sequences[0].Steps[0]).Test.Targets);
        Assert.Equal(HostTypes.Returned("screen")!.Id, system.FindPart(slot)!.Holds);
        Assert.Equal(new[] { 2 }, system.First.Facets(slot));
    }

    /// <summary>Changing the step opens on the screen it acts on.</summary>
    [Fact]
    public void ChangingTheStepOpensOnItsScreen()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);

        Assert.Equal("Back", screen.Editor.Facets()!.Name);
    }

    /// <summary>Setting a screen up asks for its content first, and then only for what that content takes, every color headed.</summary>
    [Fact]
    public void SettingUpAsksForWhatItsContentTakes()
    {
        var (screen, _) = Picking();
        screen.Editor.PressChoice("set up screen");
        screen.Run(2);
        var form = screen.Editor.Pick().Arguments!.Form;

        Assert.Equal(new[] { "content" }, Asked(screen));

        Press(form, "app");
        screen.Run(2);
        Assert.Equal(new[] { "content", "app", "app color", "app background color" }, Asked(screen));

        Press(form, "text and images");
        screen.Run(2);
        Assert.Contains("text color", Asked(screen));
        Assert.Contains("font size", Asked(screen));
        Assert.DoesNotContain("app color", Asked(screen));
    }

    /// <summary>What a setting hidden by the content holds is left out of the step, so it is left as it is.</summary>
    [Fact]
    public void ASettingTheContentHidesIsLeftOut()
    {
        var (screen, _) = Picking();
        screen.Editor.PressChoice("set up screen");
        screen.Run(2);
        var form = screen.Editor.Pick().Arguments!.Form;

        Press(form, "app");
        form.Fields[1].Text = "TSS_Weather";
        Press(form, "text and images");
        screen.Run(2);

        Assert.DoesNotContain("TSS_Weather", form.Answer);
    }

    /// <summary>Presses the word <paramref name="word" /> of the form's first value, its content, as a person does.</summary>
    static void Press(FieldSet form, string word) =>
        ((Lumel.Element)form.Fields[0]).Within().OfType<Lumel.Controls.Button>().First(button => button.Text == word).Command!.Execute();

    /// <summary>The labels the set up form shows, in the order they stand.</summary>
    static string[] Asked(Screen screen)
    {
        var form = screen.Editor.Pick().Arguments!.Form;
        return form.Rows.Cast<Lumel.Layout.FormRow>().Where(row => row.Field.IsVisible && row.Label is Lumel.Text.Label).Select(row => ((Lumel.Text.Label)row.Label!).Text).ToArray();
    }

    /// <summary>A text panel has one screen, so it opens no drop box, and its step's slot holds that screen.</summary>
    [Fact]
    public void ATextPanelsOneScreenIsChosenWithoutAsking()
    {
        var rig = Rig.Demo();
        rig.TextPanel(40, "Status Panel");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Status Panel");
        screen.Run(2);
        screen.Editor.PressChoice("show text");
        screen.Run(2);

        Assert.Null(screen.Editor.Facets());

        Finish(screen, "Docked");
        var system = screen.Editor.Program.Systems[0];
        var slot = Assert.Single(((Act)system.Sequences[0].Steps[0]).Targets);
        Assert.Equal(new[] { 1 }, system.First.Facets(slot));
    }

    static (Screen Screen, Billboard Board) Picking()
    {
        var rig = Rig.Demo();
        var board = rig.Weld(new Billboard(40, "Status Board"));
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Status Board");
        screen.Run(2);
        screen.Editor.PressChoice("show text");
        screen.Run(2);
        return (screen, board);
    }

    /// <summary>Writes <paramref name="text" /> as the text to show and finishes the pick.</summary>
    static void Finish(Screen screen, string text)
    {
        var form = screen.Editor.Pick().Arguments!.Form;
        form.Fields.First(field => field is LinesField).Text = text;
        screen.Editor.PressPrimary();
        screen.Run(2);
    }
}
