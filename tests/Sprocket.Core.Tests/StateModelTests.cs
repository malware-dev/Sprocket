using Sprocket.Host.SpaceEngineers.Framework.Model;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A logic system's slots, types and states share one set of names.</summary>
public class StateModelTests
{
    /// <summary>A new slot's name counts on past another slot's, and may be a type's or a state's.</summary>
    [Fact]
    public void ANewSlotsNameCountsOnPastAnotherSlots()
    {
        var system = new AutomationSystem(1, Guid.NewGuid());
        system.PartFor("Door");
        var side = new DeclaredType("side");
        side.Choices.Add("inside");
        side.Choices.Add("outside");
        system.Types.Add(side);
        system.States.Add(new State("where", "side", Literal.Name("inside")));

        Assert.Equal("Door 2", system.Free("Door"));
        Assert.Equal("side", system.Free("side"));
        Assert.Equal("where", system.Free("where"));
        Assert.Same(side, system.FindType("side"));
        Assert.True(system.FindState("where")!.Type == "side");
        Assert.True(new State("cycling", "", Literal.Of(false)).IsFlag);
    }
}
