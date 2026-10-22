using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A logic system's slots, types and states share one set of names.</summary>
public class StateModelTests
{
    /// <summary>A name any of the three has is taken, and a new slot's name counts on past it.</summary>
    [Fact]
    public void SlotsTypesAndStatesShareTheirNames()
    {
        var system = new AutomationSystem(1, Guid.NewGuid());
        system.PartFor("Door");
        var side = new DeclaredType("side");
        side.Choices.Add("inside");
        side.Choices.Add("outside");
        system.Types.Add(side);
        system.States.Add(new State("where", "side", Literal.Name("inside")));

        Assert.True(system.Takes("Door"));
        Assert.True(system.Takes("side"));
        Assert.True(system.Takes("where"));
        Assert.False(system.Takes("inside"));

        Assert.Equal("side 2", system.Free("side"));
        Assert.Equal("where 2", system.Free("where"));
        Assert.Same(side, system.FindType("side"));
        Assert.True(system.FindState("where")!.Type == "side");
        Assert.True(new State("cycling", "", Literal.Of(false)).IsFlag);
    }
}
