using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Reactor.Host.SpaceEngineers.Core.World;
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

    /// <summary>The same block picked twice is one declaration, so re-pointing it is one act.</summary>
    [Fact]
    public void DeclaringTheSameBlockTwiceNamesItOnce()
    {
        var program = new Automation();
        var door = Add(1, "Inner Door");

        var first = program.Declare(_world, door);
        var again = program.Declare(_world, door);

        Assert.Same(first, again);
        Assert.Single(program.Refs);
    }

    [Fact]
    public void TwoBlocksAreTwoDeclarations()
    {
        var program = new Automation();

        program.Declare(_world, Add(1, "Inner Door"));
        program.Declare(_world, Add(2, "Outer Door"));

        Assert.Equal(2, program.Refs.Count);
        Assert.NotEqual(program.Refs[0].Name, program.Refs[1].Name);
    }

    /// <summary>
    /// A name read back from storage is never handed out again to something else.
    /// </summary>
    /// <remarks>
    /// Generated names count up, so a program loaded with entity1 and entity2 must not mint entity2
    /// for the next block picked - two declarations sharing a name is two claims about one door.
    /// </remarks>
    [Fact]
    public void ARestoredNameIsNotMintedAgain()
    {
        var program = new Automation();
        program.Restore(new EntityRef("entity7", ""));

        var minted = program.Declare(_world, Add(1, "Inner Door"));

        Assert.NotEqual("entity7", minted.Name);
        Assert.Same(program.FindRef("entity7"), program.Refs[0]);
    }

    /// <summary>A name somebody wrote is left alone by the counting.</summary>
    [Fact]
    public void AWrittenNameDoesNotDisturbTheNumbering()
    {
        var program = new Automation();
        program.Restore(new EntityRef("inner", ""));

        Assert.Equal("entity1", program.Declare(_world, Add(1, "Inner Door")).Name);
    }

    /// <summary>
    /// An ordinary tick looks at nothing, which is the number that matters.
    /// </summary>
    /// <remarks>
    /// Matching walks every key against every block. Doing it per reference per tick, inside the
    /// game's update loop, is the failure this design is shaped to avoid - and the only honest proof
    /// is a count, because the wrong version works perfectly and is invisible in every screenshot.
    /// </remarks>
    [Fact]
    public void TenSecondsOfIdleTicksMatchNothing()
    {
        var program = new Automation();
        program.Declare(_world, Add(1, "Inner Door"));
        program.Declare(_world, Add(2, "Outer Door"));

        var bindings = new Bindings();
        bindings.Refresh(_world, program);

        var settled = _world.Matches;

        for (var tick = 0; tick < 600; tick++)
            bindings.Refresh(_world, program);

        Assert.Equal(settled, _world.Matches);
    }

    /// <summary>Welding something on is what makes it look again.</summary>
    [Fact]
    public void ABlockArrivingMakesItResolveOnce()
    {
        var program = new Automation();
        program.Declare(_world, Add(1, "Inner Door"));

        var bindings = new Bindings();
        bindings.Refresh(_world, program);
        var settled = bindings.Revision;

        Add(2, "Outer Door");

        bindings.Refresh(_world, program);
        Assert.NotEqual(settled, bindings.Revision);

        var after = bindings.Revision;
        bindings.Refresh(_world, program);
        Assert.Equal(after, bindings.Revision);
    }

    /// <summary>And so is declaring something, which is the other half of the chain.</summary>
    [Fact]
    public void ANewDeclarationMakesItResolveOnce()
    {
        var program = new Automation();
        program.Declare(_world, Add(1, "Inner Door"));

        var bindings = new Bindings();
        bindings.Refresh(_world, program);
        var settled = bindings.Revision;

        program.Declare(_world, Add(2, "Outer Door"));

        bindings.Refresh(_world, program);
        Assert.NotEqual(settled, bindings.Revision);
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
