using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using VRageMath;
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

    /// <summary>
    /// The drop box of screens heads the screen's members, chosen before any of them, and each stands in under it; the
    /// block's own members stand as they are, and a step on one acts on no screen.
    /// </summary>
    [Fact]
    public void TheScreensBoxHeadsTheScreensMembers()
    {
        var (screen, _) = Picking();
        var rows = screen.Editor.Pick().Stages.Last().Entries().Where(row => !row.IsHeading).ToList();

        Assert.True(rows.Single(row => row.Text == "show text").Indented);
        Assert.True(rows.Single(row => row.Text == "set up screen").Indented);
        Assert.False(rows.Single(row => row.Text == "power").Indented);

        screen.Editor.PressChoice("power");
        screen.Run(2);
        Assert.NotNull(screen.Editor.Facets());

        screen.Editor.PressPrimary();
        screen.Run(2);
        var act = Assert.IsType<Act>(screen.Editor.Program.Systems[0].Sequences[0].Steps[0]);
        Assert.Null(screen.Editor.Program.Systems[0].First.Facets(Assert.Single(act.Targets)));
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
        var slot = Assert.Single(Assert.IsType<If>(system.Sequences[0].Steps[0]).Compared.Targets);
        Assert.Equal(HostTypes.Returned("screen")!.Id, system.FindPart(slot)!.Holds);
        Assert.Equal(new[] { 2 }, system.First.Facets(slot));
    }

    /// <summary>Changing a screen step opens on the screen it acts on, with its own slot named in the heading as it is and nothing to name anew.</summary>
    [Fact]
    public void ChangingTheStepOpensOnItsScreenAndNamesItsOwnSlot()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");
        var slot = Assert.Single(Assert.IsType<Act>(screen.Editor.Program.Systems[0].Sequences[0].Steps[0]).Targets);

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);

        Assert.Equal("Back", screen.Editor.Facets()!.Name);
        var heading = screen.Editor.Screen.Heading[0];
        Assert.Equal(slot, heading.Text);
        Assert.True(heading.CanRename);
    }

    /// <summary>
    /// A screen step on a block slot, as an older program has it, that only this step uses, opens on that slot and makes it a
    /// screen slot in place, keeping its name; undo makes it a block slot again.
    /// </summary>
    [Fact]
    public void AScreenStepsOwnBlockSlotBecomesAScreenSlot()
    {
        var (screen, system, old) = OnABlockSlot(Second.None);

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);
        Assert.Equal($"{old} holds blocks, but this step uses its screen. Picking the screen makes {old} a screen slot, for every step that uses it.",
            screen.Editor.Question());
        screen.Editor.PressAnswer("Pick the screen");
        screen.Run(2);

        var heading = screen.Editor.Screen.Heading[0];
        Assert.Equal(old, heading.Text);
        Assert.Equal("", heading.After);

        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");
        Assert.Equal(old, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[0]).Targets));
        Assert.NotEqual("", system.FindPart(old)!.Holds);
        Assert.Equal(2, system.First.Facets(old)!.Single());

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal("", system.FindPart(old)!.Holds);
    }

    /// <summary>
    /// A block slot every step uses the screen of is one slot: picking for one step makes it a screen slot in place, asking
    /// nothing, and mends the other step with it.
    /// </summary>
    [Fact]
    public void ABlockSlotOnlyScreenStepsUseBecomesOneScreenSlot()
    {
        var (screen, system, old) = OnABlockSlot(Second.Screen);

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);
        screen.Editor.PressAnswer("Pick the screen");
        screen.Run(2);
        Assert.Equal(old, screen.Editor.Screen.Heading[0].Text);

        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");

        Assert.Equal(old, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[0]).Targets));
        Assert.Equal(old, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[1]).Targets));
        Assert.NotEqual("", system.FindPart(old)!.Holds);
        Assert.NotEqual(SequenceProblem.ScreenOnBlockSlot,
            SequenceSummary.Fault(screen.Editor.Runner.Get(system.Sequences[0], system.First)).Problem);
    }

    /// <summary>
    /// A screen step on a block slot a step uses for the block itself opens on a new screen slot, saying which slot it is
    /// instead of; every step using the screen moves onto it, and the block slot is left to the block's step.
    /// </summary>
    [Fact]
    public void AScreenStepOnABlockSlotABlockStepUsesIsGivenANewScreenSlot()
    {
        var (screen, system, old) = OnABlockSlot(Second.Both);

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);
        Assert.Equal($"{old} holds blocks, but this step uses its screen. Other steps use {old} for the block itself, so picking the screen gives every step that uses the screen a new screen slot, and {old} stays for the block.",
            screen.Editor.Question());
        screen.Editor.PressAnswer("Pick the screen");
        screen.Run(2);

        var heading = screen.Editor.Screen.Heading[0];
        Assert.NotEqual(old, heading.Text);
        Assert.Equal($"instead of {old}", heading.After);

        Finish(screen, "Docked");
        var made = Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[0]).Targets);
        Assert.NotEqual(old, made);
        Assert.NotEqual("", system.FindPart(made)!.Holds);
        Assert.Equal("", system.FindPart(old)!.Holds);
        Assert.Equal(1, system.Mentions(old));
        Assert.Equal(made, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[1]).Targets));
        Assert.Equal(old, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[2]).Targets));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(old, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[1]).Targets));
    }

    /// <summary>Cancelling the question asked before mending a screen step on a block slot leaves the step and its slot as they were.</summary>
    [Fact]
    public void CancellingTheRepairLeavesTheStep()
    {
        var (screen, system, old) = OnABlockSlot(Second.None);

        screen.Editor.PressStepSlot(0, 0);
        screen.Run(2);
        screen.Editor.PressAnswer("Cancel");
        screen.Run(2);

        Assert.Equal(old, Assert.Single(Assert.IsType<Act>(system.Sequences[0].Steps[0]).Targets));
        Assert.Equal("", system.FindPart(old)!.Holds);
        Assert.IsNotType<PickPage>(screen.Editor.Current);
    }

    /// <summary>The warning on a screen step whose slot holds blocks is said on the home screen too, once, by its logic system, and not by its installations.</summary>
    [Fact]
    public void AScreenStepOnABlockSlotIsWarnedOfFromHome()
    {
        var (screen, _, _) = OnABlockSlot(Second.None);
        screen.Editor.GoToRoot();
        screen.Run(2);

        var heading = screen.Editor.RootHeadings()[0];
        Assert.Contains("This step is outdated and now needs a screen slot.", heading.Problem);
        Assert.Equal(NoticeLevel.Warning, heading.Level);
        Assert.Equal(1, heading.Remarks);
        Assert.Equal(0, heading.Problems);

        var row = screen.Editor.RootRows()[0];
        Assert.Equal("", row.Problem);
        Assert.Equal(0, row.Remarks);
    }

    /// <summary>What else uses the block slot of <see cref="OnABlockSlot" />.</summary>
    enum Second
    {
        /// <summary>Nothing else.</summary>
        None,

        /// <summary>A second show text step.</summary>
        Screen,

        /// <summary>A step switching the block itself on and off.</summary>
        Block,

        /// <summary>A second show text step, and then a step switching the block itself on and off.</summary>
        Both
    }

    /// <summary>A show text step whose slot holds blocks, as an older program has it, with <paramref name="second" /> on that slot after it.</summary>
    static (Screen Screen, AutomationSystem System, string Slot) OnABlockSlot(Second second)
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");
        var system = screen.Editor.Program.Systems[0];
        var first = Assert.IsType<Act>(system.Sequences[0].Steps[0]);
        var old = Assert.Single(first.Targets);
        system.FindPart(old)!.Holds = "";
        if (second == Second.Screen || second == Second.Both)
        {
            var other = new Act(first.ActionId) { Form = first.Form, Provider = first.Provider };
            other.Targets.Add(old);
            foreach (var argument in first.Arguments)
                other.Arguments.Add(argument);
            system.Sequences[0].Steps.Add(other);
        }

        if (second == Second.Block || second == Second.Both)
        {
            var toggle = new Act("OnOff");
            toggle.Targets.Add(old);
            system.Sequences[0].Steps.Add(toggle);
        }

        screen.Editor.Program.Edited();
        screen.Run(2);
        return (screen, system, old);
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

    /// <summary>
    /// Done with only some settings given makes the step, each value in its own place, and the step lacks nothing: the rest
    /// are left as they are.
    /// </summary>
    [Fact]
    public void SettingUpWithSettingsLeftEmptyIsDone()
    {
        var (screen, _) = Picking();
        screen.Editor.PressChoice("set up screen");
        screen.Run(2);
        var form = screen.Editor.Pick().Arguments!.Form;
        Press(form, "text and images");
        form.Fields[2].Text = "#FF0000";
        form.Fields[4].Text = "";
        screen.Run(2);

        screen.Editor.PressPrimary();
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Systems[0].Sequences[0].Steps[0]);
        Assert.Equal(ScreenType.SetUp, act.ActionId);
        Assert.Equal("", act.Arguments[1].Text);
        Assert.Equal(Literal.Color(255, 0, 0, 255).Said, act.Arguments[2].Said);
        Assert.Equal("", screen.Editor.StepRows()[0].Problem);
    }

    /// <summary>A new step setting a screen up opens on what the screen chosen holds, its content alone left to choose.</summary>
    [Fact]
    public void SettingUpOpensOnWhatTheScreenHolds()
    {
        var (screen, board) = Picking();
        var back = board.Faces[1];
        back.FontColor = new Color(0, 255, 0, 255);
        back.BackgroundColor = new Color(0, 0, 255, 255);
        back.Font = "Monospace";
        back.FontSize = 2.5f;
        back.Alignment = TextAlignment.CENTER;
        back.Padding = 10f;
        back.Foreground = new Color(255, 0, 0, 255);
        back.App = "TSS_Weather";
        screen.Editor.PressChoice("set up screen");
        screen.Run(2);
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);

        var form = screen.Editor.Pick().Arguments!.Form;
        Assert.Equal("", form.Fields[0].Text);
        Assert.Equal("TSS_Weather", form.Fields[1].Text);
        Assert.Equal(new Color(0, 255, 0, 255), Shade(form.Fields[2].Text));
        Assert.Equal(new Color(0, 0, 255, 255), Shade(form.Fields[3].Text));
        Assert.Equal("Monospace", form.Fields[4].Text);
        Assert.Equal("2.5", form.Fields[5].Text);
        Assert.Equal("center", form.Fields[6].Text);
        Assert.Equal("10.0", form.Fields[7].Text);
        Assert.Equal(new Color(255, 0, 0, 255), Shade(form.Fields[8].Text));
    }

    /// <summary>A screen running no app opens with none chosen, which is a choice of its own.</summary>
    [Fact]
    public void AScreenRunningNoAppOpensOnNone()
    {
        var (screen, _) = Picking();
        screen.Editor.PressChoice("set up screen");
        screen.Run(2);

        Assert.Equal("none", screen.Editor.Pick().Arguments!.Form.Fields[1].Text);
    }

    /// <summary>The color <paramref name="said" /> spells.</summary>
    static Color Shade(string said)
    {
        Assert.True(ColorChannels.TryRead(said, out var color, out _), $"'{said}' is no color");
        return color;
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

    /// <summary>A trigger's task on a screen, opened on what it does and left with nothing changed, goes back without asking.</summary>
    [Fact]
    public void LeavingATriggersScreenTaskUnchangedAsksNothing()
    {
        var rig = Rig.Demo();
        rig.TextPanel(40, "Status Panel");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Stock");
        screen.Editor.Back();
        screen.Run(2);
        screen.Editor.PressStart("every");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Status Panel");
        screen.Run(2);
        screen.Editor.PressChoice("show text");
        screen.Run(2);
        screen.Editor.Pick().Arguments!.Form.Fields.First(field => field is LinesField).Text = "Docked";
        screen.Editor.PressPrimary();
        screen.Run(4);

        screen.Editor.Triggers().Last().Does.OfType<EntitiesSlot>().First().Opens!.Execute();
        screen.Run(3);
        screen.Editor.Back();
        screen.Run(4);

        Assert.Equal("Stock", screen.Editor.Screen.Title());
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

    /// <summary>A slot of screens picked by name asks for no screen: what is done is that type's alone, on the screen the slot holds.</summary>
    [Fact]
    public void AScreenSlotPickedByNameAsksNoScreen()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");
        var system = screen.Editor.Program.Systems[0];
        var slot = ((Act)system.Sequences[0].Steps[0]).Targets[0];

        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.ChooseSource(screen.Editor.Texts.PickSourceSlots);
        screen.Run(1);
        screen.Editor.PressBlock(slot);
        screen.Run(2);

        Assert.Null(screen.Editor.Facets());
        Assert.False(screen.Editor.HasChoice("power"), "a member of the blocks was offered for a slot of screens");
        var held = Assert.Single(Target.Within(screen.Editor.Pick().Stages[0]).OfType<SlotHeldRow>());
        var says = Target.Within(held).OfType<SlotChip>().Select(chip => chip.Text).ToList();
        Assert.Contains("Status Board", says);
        Assert.Contains("Back", says);
        screen.Editor.PressChoice("show text");
        screen.Run(2);
        Finish(screen, "Undocked");

        var made = system.Sequences[0].Steps.OfType<Act>().Last();
        Assert.Equal(slot, Assert.Single(made.Targets));
        Assert.Equal(new[] { 2 }, system.First.Facets(slot));
    }

    /// <summary>
    /// A trigger's own action on a screen, named as a trigger names it, takes the slot of screens the picker's heading shows for
    /// the same pick, rather than making a slot of the blocks.
    /// </summary>
    [Fact]
    public void ATriggersScreenActionNamesTheSlotItsHeadingShows()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var act = (Act)system.Sequences[0].Steps[0];
        var slot = act.Targets[0];
        var board = editor.World.Entities.First(e => e.Name == "Status Board").Id;

        var forecast = editor.Picks.Forecast(system, new[] { board }, act.ActionId, "Back", false, null, false, null, "");
        List<string>? named = null;
        editor.Picks.Name(system, new PickAnswer(new[] { board }, act.ActionId, Quantifier.Any, null, act.Form, "Back"), false, null,
            names => named = names);

        Assert.Equal(new[] { slot }, forecast.Matched);
        Assert.Equal(new[] { slot }, named);
    }

    /// <summary>The tooltip of a slot of screens lists each block with the screen of it the installation shown holds.</summary>
    [Fact]
    public void AScreenSlotsTipNamesEachBlockAndItsScreen()
    {
        var (screen, _) = Picking();
        screen.Editor.Facets()!.Choose("Back");
        screen.Run(2);
        Finish(screen, "Docked");

        var row = screen.Editor.StepRows()[0];
        var tip = row.ChipOf(row.Slots.OfType<EntitiesSlot>().Single())!.Tip!.Text;
        Assert.Contains("- Status Board: Back\n", tip);
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
