using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Driving;
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
