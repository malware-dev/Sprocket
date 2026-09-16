using Reactor.Host.SpaceEngineers.Presentation.Words;
using Reactor.Sequences;
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
    readonly SequenceText _words = new SequenceText();

    [Fact]
    public void ATriggerTheShipAnswersSaysThatItIsOne()
    {
        Assert.Equal("When something on this ship says so", _words.Trigger(null!, new RequiredTrigger()));
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

        Assert.Equal("run Let me out", _words.Doing(null!, caller));
    }

    [Fact]
    public void ARunOfSomethingNamelessReadsAsItsNumber()
    {
        var program = new Automation();
        var system = program.AddSystem();

        var called = program.AddSequence(system);
        var caller = program.AddSequence(system);
        caller.Steps.Add(new Run(called.Number));

        Assert.Equal("run Sequence " + called.Number, _words.Doing(null!, caller));
    }
}
