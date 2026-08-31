using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.World;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// What happens to a reference when the thing it named changes underneath it.
/// </summary>
/// <remarks>
/// The middle state is the reason this exists. A reference used to be an id and a yes-or-no; the
/// interesting case is the one in between, where the id is gone and something else answers to the
/// name - because binding to it silently is how the wrong door gets cycled on somebody's airlock.
/// </remarks>
public class RefTests
{
    readonly TestWorld _world = new();

    [Fact]
    public void TheStrongestKeyFindingItIsABinding()
    {
        var door = Add(1, "Inner Door");
        var declared = Declare("inner", door);

        var bindings = Resolve(declared);

        Assert.Equal(RefState.Bound, bindings.State("inner"));
        Assert.Same(door, bindings.Entity("inner"));
        Assert.True(bindings.Holds("inner"));
    }

    /// <summary>
    /// The id is gone and something else carries the name, so there is a candidate and no binding.
    /// </summary>
    /// <remarks>
    /// This is a rebuilt door, or a blueprint pasted next to the original. The candidate is worth
    /// showing and is not worth acting on, and the difference between those two is the whole design.
    /// </remarks>
    [Fact]
    public void AWeakerKeyFindingItIsAProposalAndNotABinding()
    {
        var door = Add(1, "Inner Door");
        var declared = Declare("inner", door);

        _world.Remove(1);
        var rebuilt = Add(2, "Inner Door");

        var bindings = Resolve(declared);

        Assert.Equal(RefState.Adrift, bindings.State("inner"));
        Assert.Same(rebuilt, bindings.Entity("inner"));
        Assert.False(bindings.Holds("inner"));
    }

    [Fact]
    public void NothingAnsweringAtAllIsLost()
    {
        var door = Add(1, "Inner Door");
        var declared = Declare("inner", door);

        _world.Remove(1);

        var bindings = Resolve(declared);

        Assert.Equal(RefState.Lost, bindings.State("inner"));
        Assert.Null(bindings.Entity("inner"));
    }

    /// <summary>
    /// A kind never finds anything. It refuses what a key found.
    /// </summary>
    /// <remarks>
    /// The case it exists for: somebody renames a lamp to what the door used to be called. The name
    /// matches, so without the constraint the sequence would propose opening a lamp.
    /// </remarks>
    [Fact]
    public void AKeyLandingOnTheWrongKindOfThingIsRefused()
    {
        var door = Add(1, "Inner Door");
        var declared = Declare("inner", door);

        _world.Remove(1);
        _world.Add(new TestLamp(2, "Inner Door"));

        var bindings = Resolve(declared);

        Assert.Equal(RefState.Lost, bindings.State("inner"));
        Assert.Null(bindings.Entity("inner"));
    }

    /// <summary>A declaration with nothing to bind it is legal, and is simply lost.</summary>
    [Fact]
    public void ADeclarationWithNoKeysIsLostRatherThanBroken()
    {
        var declared = new EntityRef("spare", "");

        var bindings = Resolve(declared);

        Assert.Equal(RefState.Lost, bindings.State("spare"));
    }

    /// <summary>A name nobody declared answers rather than throwing.</summary>
    [Fact]
    public void AnUndeclaredNameIsLost()
    {
        var bindings = Resolve();

        Assert.Equal(RefState.Lost, bindings.State("nothing-like-this"));
        Assert.Null(bindings.Entity("nothing-like-this"));
    }

    TestDoor Add(long id, string name)
    {
        var door = new TestDoor(id, name);
        _world.Add(door);
        return door;
    }

    EntityRef Declare(string name, IEntity entity)
    {
        var declared = new EntityRef(name, entity.Kind.Id);
        _world.Identify(entity, declared.Keys);
        return declared;
    }

    Bindings Resolve(params EntityRef[] refs)
    {
        var bindings = new Bindings();
        bindings.Resolve(_world, refs);
        return bindings;
    }
}
