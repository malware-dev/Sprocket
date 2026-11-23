using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>States holding a number, a text or a length, made, started, set and compared from the editor.</summary>
public class StateTypeEditingTests
{
    const string Head = "sprocket 3 alpha 3\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Counter\n{\n"
        + "    let count = 0\n"
        + "    let most = 9\n"
        + "    let cycling = off\n\n";

    /// <summary>Choosing a whole number for a state's type makes it an integer state starting at nought.</summary>
    [Fact]
    public void AStateIsMadeToHoldAWholeNumber()
    {
        var screen = StateEditingTests.OnSystem(Head + "    sequence 1 {\n    }\n}\n", out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        Target.Within(view).OfType<StateRow>().Last().TypeButton.Command!.Execute();
        screen.Run(1);
        editor.PressAnswer(editor.Texts.SettingKindInteger);
        screen.Run(1);

        var cycling = system.FindState("cycling")!;
        Assert.Equal(ValueKind.Integer, cycling.Kind);
        Assert.Equal(0, cycling.Start.Integer);
        Assert.Equal(ValueKind.Integer, cycling.Start.Kind);
    }

    /// <summary>A number state's start is given in a field of its type, with no mark, as a setting's default is, and changes as one edit.</summary>
    [Fact]
    public void ANumberStatesStartIsTypedInAField()
    {
        var screen = StateEditingTests.OnSystem(Head + "    sequence 1 {\n    }\n}\n", out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        Target.Within(view).OfType<StateRow>().First().Start.Command!.Execute();
        screen.Run(1);
        var page = Assert.IsType<LogicSystemPage>(editor.Current);
        var panel = page.Stating!;
        Assert.Null(panel.Form.MarkOf(0));
        panel.Form.Fields[0].Text = "7";
        panel.Done();
        screen.Run(1);

        Assert.Equal(7, system.FindState("count")!.Start.Integer);
        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Equal(0, system.FindState("count")!.Start.Integer);
    }

    /// <summary>What a step sets a number state to is given in the values panel, which also offers another state's value.</summary>
    [Fact]
    public void ANumberStateIsSetInTheValuesPanel()
    {
        var screen = StateEditingTests.OnSequence(Head + "    sequence 1 {\n        set count to 3\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        Assert.Single(editor.StepRows()).Slots[1].Opens!.Execute();
        screen.Run(1);
        var page = Assert.IsType<SequencePage>(editor.Current);
        var panel = page.Giving!;
        Assert.Equal("3", panel.Form.Fields[0].Text);
        panel.Form.Fields[0].Text = "5";
        panel.Done();
        screen.Run(1);

        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.Equal(ValueKind.Integer, set.Value.Kind);
        Assert.Equal(5, set.Value.Integer);
    }

    /// <summary>A test of a number state is offered the orderings beside is and is not, in the words a block's condition uses.</summary>
    [Fact]
    public void ANumberStatesTestOffersTheOrderings()
    {
        var screen = StateEditingTests.OnSequence(Head + "    sequence 1 {\n        if count > 3 then {\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        var row = editor.StepRows()[0];
        Assert.Contains(ComparisonQuestion.Said(Relation.Above), editor.Reads(0));
        row.Slots.OfType<ArgumentsSlot>().First(slot => slot.Holds == ArgumentRole.Compared).Opens!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { Relation.Is, Relation.IsNot, Relation.Above, Relation.AtLeast, Relation.Below, Relation.AtMost }.Select(ComparisonQuestion.Said).ToArray(),
            editor.Screen.Answers.Select(answer => answer.Text).ToArray());

        editor.PressAnswer(ComparisonQuestion.Said(Relation.AtMost));
        screen.Run(1);
        Assert.Equal(Relation.AtMost, Assert.IsType<StateTest>(((If)sequence.Steps[0]).Test).Relation);
    }
}
