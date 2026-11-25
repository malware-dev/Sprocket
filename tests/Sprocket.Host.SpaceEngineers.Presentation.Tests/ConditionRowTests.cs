using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An if and a when whose condition a script writes as an expression read on their rows as the condition does.</summary>
public class ConditionRowTests
{
    const string Program = "sprocket 3 alpha 3\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
        + "    let cycling = off\n"
        + "    let count = 0\n\n"
        + "    when count >= 3 and not cycling then run 1\n\n"
        + "    sequence 1 {\n"
        + "        if cycling or count > 2 then {\n"
        + "        }\n"
        + "    }\n}\n";

    /// <summary>The if's row says its condition as one part.</summary>
    [Fact]
    public void AnIfOnAnExpressionReadsAsItsCondition()
    {
        var screen = StateEditingTests.OnSequence(Program, out var sequence);
        Assert.IsType<ExpressionTest>(((If)sequence.Steps[0]).Test);

        var part = Assert.IsType<ArgumentsSlot>(Assert.Single(screen.Editor.StepRows()[0].Slots));
        var said = Assert.Single(part.Arguments).Text;
        Assert.Contains("cycling", said);
        Assert.Contains("count", said);
    }

    /// <summary>The if's condition opens its builder holding it; Done changes the if, and undo puts the condition back.</summary>
    [Fact]
    public void AnIfsConditionIsChangedInItsBuilderAndUndone()
    {
        var screen = StateEditingTests.OnSequence(Program, out var sequence);
        var editor = screen.Editor;
        editor.PressStepSlot(0, 0);
        screen.Run(2);
        var draft = Assert.IsType<ExpressionPage>(editor.Current).Panel.Draft;
        Assert.Equal("cycling or count > 2", draft.Built()!.Written);

        var or = (DraftJoin)draft.Root;
        draft.Remove(or.Parts[1]);
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[1],
            new DraftJoin(JoinKind.AtMost, new DraftPiece[] { new DraftValue(new StateOperand("count")), new DraftValue(new Operand(Literal.Of(1L))) }));
        screen.Run(2);
        editor.Current.Primary();
        screen.Run(2);

        Assert.IsType<SequencePage>(editor.Current);
        Assert.Equal("cycling or count <= 1", Assert.IsType<ExpressionTest>(((If)sequence.Steps[0]).Test).Condition.Written);
        editor.Current.Undo();
        screen.Run(2);
        Assert.Equal("cycling or count > 2", Assert.IsType<ExpressionTest>(((If)sequence.Steps[0]).Test).Condition.Written);
    }

    /// <summary>The when's condition opens its builder holding it, and Done changes the when, which still runs its sequence.</summary>
    [Fact]
    public void AWhensConditionIsChangedInItsBuilder()
    {
        var screen = StateEditingTests.OnSystem(Program, out var system);
        var editor = screen.Editor;
        var runs = system.Triggers[0].Does.Number;
        editor.PressWhen(0);
        screen.Run(2);
        var draft = Assert.IsType<ExpressionPage>(editor.Current).Panel.Draft;
        Assert.Equal("count >= 3 and not cycling", draft.Built()!.Written);

        var and = (DraftJoin)draft.Root;
        draft.Remove(and.Parts[0]);
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[0],
            new DraftJoin(JoinKind.Above, new DraftPiece[] { new DraftValue(new StateOperand("count")), new DraftValue(new Operand(Literal.Of(5L))) }));
        screen.Run(2);
        editor.Current.Primary();
        screen.Run(2);

        var changed = Assert.IsType<ExpressionTrigger>(Assert.Single(system.Triggers));
        Assert.Equal("count > 5 and not cycling", changed.Condition.Written);
        Assert.Equal(runs, changed.Does.Number);
    }

    /// <summary>The when's row says its condition as one part, as a when on blocks or a state is worded.</summary>
    [Fact]
    public void AWhenOnAnExpressionReadsAsItsCondition()
    {
        var screen = StateEditingTests.OnSystem(Program, out var system);
        Assert.IsType<ExpressionTrigger>(system.Triggers[0]);

        var row = Assert.Single(screen.Editor.Triggers());
        var part = Assert.IsType<ArgumentsSlot>(Assert.Single(row.Starts));
        var said = Assert.Single(part.Arguments).Text;
        Assert.Contains("count", said);
        Assert.Contains("cycling", said);
    }
}
