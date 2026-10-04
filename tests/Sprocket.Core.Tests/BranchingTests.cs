using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A sequence answers the questions about the shape of its ifs: where a branch ends, which if a line belongs to, its otherwise, and where a run goes next.</summary>
public class BranchingTests
{
    /// <summary>
    /// 0 if, 1 wait, 2 if, 3 wait, 4 end, 5 else if, 6 wait, 7 else, 8 wait, 9 end, 10 wait: an if holding another in its first
    /// branch, with an else if and an else.
    /// </summary>
    static Sequence Nested()
    {
        var sequence = new Sequence(1);
        sequence.Steps.Add(new If(new Comparison()));
        sequence.Steps.Add(new Wait(10));
        sequence.Steps.Add(new If(new Comparison()));
        sequence.Steps.Add(new Wait(20));
        sequence.Steps.Add(new EndIf());
        sequence.Steps.Add(new Else(new Comparison()));
        sequence.Steps.Add(new Wait(30));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new Wait(40));
        sequence.Steps.Add(new EndIf());
        sequence.Steps.Add(new Wait(50));
        return sequence;
    }

    /// <summary>An if's branches are its own line and each else of it, past any if nested inside.</summary>
    [Fact]
    public void AnIfsBranchesStepOverTheIfsInside()
    {
        var sequence = Nested();

        Assert.Equal(new[] { 0, 5, 7 }, sequence.Branches(0));
        Assert.Equal(new[] { 2 }, sequence.Branches(2));
        Assert.Equal(9, sequence.EndOf(0));
        Assert.Equal(4, sequence.EndOf(2));
    }

    /// <summary>An else, and an end, belong to the if they close, however deep.</summary>
    [Fact]
    public void ALineBelongsToTheIfItCloses()
    {
        var sequence = Nested();

        Assert.Equal(0, sequence.OpenOf(5));
        Assert.Equal(0, sequence.OpenOf(9));
        Assert.Equal(2, sequence.OpenOf(4));
    }

    /// <summary>The otherwise is the last branch where it tests nothing, and an if without one has none.</summary>
    [Fact]
    public void AnOtherwiseIsTheLastBranchThatTestsNothing()
    {
        var sequence = Nested();

        Assert.Equal(7, sequence.Otherwise(0));
        Assert.Equal(-1, sequence.Otherwise(2));
    }

    /// <summary>A run goes on from the end of a branch past the end of its if, and past what is switched off.</summary>
    [Fact]
    public void ARunGoesOnPastTheRestOfItsIf()
    {
        var sequence = Nested();

        // The end of the nested if's branch, then on past the outer if's other branches.
        Assert.Equal(10, sequence.After(3));
        Assert.Equal(10, sequence.After(6));
        Assert.Equal(2, sequence.After(1));

        sequence.Steps[2].IsOff = true;
        Assert.Equal(10, sequence.After(1));
    }
}
