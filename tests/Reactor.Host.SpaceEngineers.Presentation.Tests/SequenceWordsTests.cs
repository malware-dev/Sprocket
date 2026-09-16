using Reactor.Host.SpaceEngineers.Words;
using Reactor.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Every sort of trigger and step a program can hold reads as something.
/// </summary>
/// <remarks>
/// The last arm of a switch over kinds answers for whatever core adds next, so a new sort of step
/// does not read as nothing - it reads as the wrong thing, with no test failing. A trigger the
/// construct answers came out blank, and one sequence running another said "wait for it to finish".
/// </remarks>
public class SequenceWordsTests
{
    [Fact]
    public void ATriggerTheShipAnswersSaysThatItIsOne()
    {
        Assert.Equal("When something on this ship says so", SequenceText.Trigger(null!, new RequiredTrigger()));
    }

    [Fact]
    public void ARunReadsAsTheSequenceItStarts()
    {
        var program = new Automation();
        var system = program.AddSystem();

        var called = program.AddSequence(system);
        called.Name = "Let me out";

        var caller = program.AddSequence(system);
        caller.Steps.Add(new Run(called.Number));

        Assert.Equal("run Let me out", SequenceText.Doing(null!, caller));
    }

    [Fact]
    public void ARunOfSomethingNamelessReadsAsItsNumber()
    {
        var program = new Automation();
        var system = program.AddSystem();

        var called = program.AddSequence(system);
        var caller = program.AddSequence(system);
        caller.Steps.Add(new Run(called.Number));

        Assert.Equal("run Sequence " + called.Number, SequenceText.Doing(null!, caller));
    }
}
