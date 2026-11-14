using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

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
    public void ARunReadsAsTheSequenceItStarts()
    {
        var program = new Automation();
        var system = program.AddSystem();

        var called = program.AddSequence(system);
        called.Name = "Let me out";

        var caller = program.AddSequence(system);
        caller.Steps.Add(new Run(called.Number));

        Assert.Equal("run Let me out", SequenceText.Step(null!, caller, caller.Steps[0], null));
    }

    [Fact]
    public void ARunOfSomethingNamelessReadsAsItsNumber()
    {
        var program = new Automation();
        var system = program.AddSystem();

        var called = program.AddSequence(system);
        var caller = program.AddSequence(system);
        caller.Steps.Add(new Run(called.Number));

        Assert.Equal("run Sequence " + called.Number, SequenceText.Step(null!, caller, caller.Steps[0], null));
    }
}
