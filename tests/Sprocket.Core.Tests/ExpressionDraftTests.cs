using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Xunit;

namespace Sprocket.Tests;

/// <summary>An expression as the builder holds it while it is put together: its empty places, boxes, moves, removals and undo, and what each part can still come to.</summary>
public class ExpressionDraftTests
{
    /// <summary>What the settings of these tests hold: Count an integer, Ratio a real, Label a text, Delay a length of time, Armed a flag.</summary>
    sealed class Types : ITypeScope
    {
        public ValueKind? Setting(string name)
        {
            switch (name)
            {
                case "Count": return ValueKind.Integer;
                case "Ratio": return ValueKind.Real;
                case "Label": return ValueKind.Text;
                case "Delay": return ValueKind.Duration;
                case "Armed": return ValueKind.Flag;
            }

            return null;
        }

        public ValueKind? State(string name) => null;

        public ValueKind? Reading(BlockReading reading) => null;
    }

    static readonly ITypeScope Scope = new Types();

    static DraftValue Named(string name) => new DraftValue(new Operand(Literal.Setting(name)));

    static DraftValue Number(long number) => new DraftValue(new Operand(Literal.Of(number)));

    /// <summary>A box put on a value takes the value into its first place and leaves the rest empty, so nothing is built until they are filled.</summary>
    [Fact]
    public void ABoxPutOnAValueHoldsItFirstAndWaitsForTheRest()
    {
        var draft = new ExpressionDraft(null);
        draft.Fill((DraftHole)draft.Root, Named("Count"));
        draft.Wrap(draft.Root, JoinKind.Add);

        var join = Assert.IsType<DraftJoin>(draft.Root);
        Assert.IsType<DraftValue>(join.Parts[0]);
        Assert.IsType<DraftHole>(join.Parts[1]);
        Assert.Null(draft.Built());

        draft.Fill((DraftHole)join.Parts[1], Number(2));
        Assert.Equal("Count + 2", draft.Built()!.Written);
    }

    /// <summary>A box dropped on an empty place opens there with every place of its own empty.</summary>
    [Fact]
    public void ABoxOnAnEmptyPlaceOpensWithItsPlacesEmpty()
    {
        var draft = new ExpressionDraft(null);
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Lowest));

        var join = Assert.IsType<DraftJoin>(draft.Root);
        Assert.Equal(2, join.Parts.Count);
        Assert.All(join.Parts, part => Assert.IsType<DraftHole>(part));
    }

    /// <summary>Removing a value leaves its place empty, and undo and redo take the removal back and do it again.</summary>
    [Fact]
    public void ARemovalLeavesItsPlaceEmptyAndIsUndone()
    {
        var draft = new ExpressionDraft(new Arithmetic(Operator.Multiply, new Operand(Literal.Setting("Count")), new Operand(Literal.Of(3L))));
        var join = (DraftJoin)draft.Root;

        draft.Remove(join.Parts[1]);
        Assert.IsType<DraftHole>(((DraftJoin)draft.Root).Parts[1]);

        draft.Undo();
        Assert.Equal("Count * 3", draft.Built()!.Written);
        Assert.True(draft.CanRedo);

        draft.Redo();
        Assert.Null(draft.Built());
    }

    /// <summary>A value moved to an empty place leaves its own place empty, and a box cannot be moved into a place inside itself.</summary>
    [Fact]
    public void AMoveLeavesItsPlaceEmptyAndNeverGoesIntoItself()
    {
        var draft = new ExpressionDraft(null);
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Add));
        var join = (DraftJoin)draft.Root;
        draft.Fill((DraftHole)join.Parts[0], Number(5));

        draft.Move(join.Parts[0], (DraftHole)join.Parts[1]);
        Assert.IsType<DraftHole>(join.Parts[0]);
        Assert.IsType<DraftValue>(join.Parts[1]);

        draft.Wrap(join.Parts[1], JoinKind.Negate);
        var inner = (DraftJoin)join.Parts[1];
        Assert.Null(draft.TriedMove(join, (DraftHole)join.Parts[0]));
        Assert.NotNull(draft.TriedMove(inner, (DraftHole)join.Parts[0]));
    }

    /// <summary>What a box can come to follows the language: a text joined to a number is a text, a length times a number a length, and a flag fits no box.</summary>
    [Fact]
    public void ABoxComesToWhatTheLanguageSays()
    {
        Assert.Equal(KindClass.Text, DraftKinds.Of(new DraftJoin(JoinKind.Add, new DraftPiece[] { Named("Label"), Number(1) }), Scope));
        Assert.Equal(KindClass.Duration, DraftKinds.Of(new DraftJoin(JoinKind.Multiply, new DraftPiece[] { Named("Delay"), Number(2) }), Scope));
        Assert.Equal(KindClass.None, DraftKinds.Of(new DraftJoin(JoinKind.Add, new DraftPiece[] { Named("Armed"), Number(1) }), Scope));
        Assert.Equal(KindClass.None, DraftKinds.Of(new DraftJoin(JoinKind.Subtract, new DraftPiece[] { Named("Label"), Number(1) }), Scope));
    }

    /// <summary>An empty place can still be anything, so a box with one is lit where any filling would work, and a whole expression suits a field only where it can come to what the field takes.</summary>
    [Fact]
    public void AnEmptyPlaceCanStillBeAnything()
    {
        var open = new DraftJoin(JoinKind.Add, new DraftPiece[] { Named("Delay"), new DraftHole() });
        Assert.Equal(KindClass.Duration | KindClass.Text, DraftKinds.Of(open, Scope));
        Assert.True(DraftKinds.Suits(open, DraftKinds.Wanted("duration"), Scope));
        Assert.False(DraftKinds.Suits(open, DraftKinds.Wanted("integer"), Scope));
    }

    /// <summary>An expression read into the builder and built again is the same expression.</summary>
    [Fact]
    public void AnExpressionComesBackAsItWent()
    {
        var expression = new Arithmetic(Operator.Multiply,
            new Function(FunctionKind.Min, new List<Expression> { new Operand(Literal.Setting("Count")), new Operand(Literal.Of(4L)) }),
            new Negation(new Operand(Literal.Setting("Ratio"))));

        Assert.Equal(expression.Written, ExpressionDraft.Built(ExpressionDraft.From(expression))!.Written);
    }

    /// <summary>Fixed values are worked out, and the setting beside them is kept.</summary>
    [Fact]
    public void FixedValuesAreWorkedOut()
    {
        var expression = new Arithmetic(Operator.Add,
            new Arithmetic(Operator.Multiply, new Operand(Literal.Of(2L)), new Operand(Literal.Of(3L))),
            new Operand(Literal.Setting("Count")));

        Assert.Equal("6 + Count", Folding.Fold(expression, Scope).Written);
    }

    /// <summary>A number times 0 is 0, and times 1, plus 0 or minus 0 is the number, whatever it reads.</summary>
    [Fact]
    public void WhatMakesNoDifferenceIsDropped()
    {
        var count = new Operand(Literal.Setting("Count"));
        Assert.Equal("0", Folding.Fold(new Arithmetic(Operator.Multiply, count, new Operand(Literal.Of(0L))), Scope).Written);
        Assert.Equal("Count", Folding.Fold(new Arithmetic(Operator.Multiply, new Operand(Literal.Of(1L)), count), Scope).Written);
        Assert.Equal("Count", Folding.Fold(new Arithmetic(Operator.Add, count, new Operand(Literal.Of(0L))), Scope).Written);
        Assert.Equal("Count", Folding.Fold(new Arithmetic(Operator.Subtract, count, new Operand(Literal.Of(0L))), Scope).Written);
    }

    /// <summary>
    /// What only looks like no difference is kept: a text plus 0 joins a 0 on, an integer divided by 1 becomes a real, and
    /// a division by zero fails where it runs rather than before.
    /// </summary>
    [Fact]
    public void WhatChangesTheValueIsKept()
    {
        Assert.Equal("Label + 0", Folding.Fold(new Arithmetic(Operator.Add, new Operand(Literal.Setting("Label")), new Operand(Literal.Of(0L))), Scope).Written);
        Assert.Equal("Count / 1", Folding.Fold(new Arithmetic(Operator.Divide, new Operand(Literal.Setting("Count")), new Operand(Literal.Of(1L))), Scope).Written);
        Assert.Equal("Ratio", Folding.Fold(new Arithmetic(Operator.Divide, new Operand(Literal.Setting("Ratio")), new Operand(Literal.Of(1L))), Scope).Written);
        Assert.Equal("1 / 0", Folding.Fold(new Arithmetic(Operator.Divide, new Operand(Literal.Of(1L)), new Operand(Literal.Of(0L))), Scope).Written);
    }
}
