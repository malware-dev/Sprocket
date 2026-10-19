using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

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
    readonly Rigged _ship = new();

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

        _ship.Grind(1);
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

        _ship.Grind(1);

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

        _ship.Grind(1);
        _ship.Light(2, "Inner Door");

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

        var first = program.Declare(_ship.World, door);
        var again = program.Declare(_ship.World, door);

        Assert.Same(first, again);
        Assert.Single(program.Refs);
    }

    [Fact]
    public void TwoBlocksAreTwoDeclarations()
    {
        var program = new Automation();

        program.Declare(_ship.World, Add(1, "Inner Door"));
        program.Declare(_ship.World, Add(2, "Outer Door"));

        Assert.Equal(2, program.Refs.Count);
        Assert.NotEqual(program.Refs[0].Name, program.Refs[1].Name);
    }

    /// <summary>
    /// A name read back from storage is never handed out again to something else.
    /// </summary>
    /// <remarks>
    /// A variable is named after its block, so a program loaded with a variable called Inner Door must not name the next
    /// Inner Door picked the same - two declarations sharing a name is two claims about one door.
    /// </remarks>
    [Fact]
    public void ARestoredNameIsNotMintedAgain()
    {
        var program = new Automation();
        program.Restore(new EntityRef("Inner Door", ""));

        var minted = program.Declare(_ship.World, Add(1, "Inner Door"));

        Assert.Equal("Inner Door 2", minted.Name);
        Assert.Same(program.FindRef("Inner Door"), program.Refs[0]);
    }

    /// <summary>A block picked is named after itself.</summary>
    [Fact]
    public void ABlockIsNamedAfterItself()
    {
        var program = new Automation();
        program.Restore(new EntityRef("inner", ""));

        Assert.Equal("Inner Door", program.Declare(_ship.World, Add(1, "Inner Door")).Name);
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
        program.Declare(_ship.World, Add(1, "Inner Door"));
        program.Declare(_ship.World, Add(2, "Outer Door"));

        var bindings = new Bindings();
        bindings.Refresh(_ship.World, program);

        var settled = _ship.World.Keys.Matched;

        for (var tick = 0; tick < 600; tick++)
            bindings.Refresh(_ship.World, program);

        Assert.Equal(settled, _ship.World.Keys.Matched);
    }

    /// <summary>Welding something on is what makes it look again, once.</summary>
    [Fact]
    public void ABlockArrivingMakesItResolveOnce()
    {
        var program = new Automation();
        program.Declare(_ship.World, Add(1, "Inner Door"));

        var bindings = new Bindings();
        bindings.Refresh(_ship.World, program);
        var settled = _ship.World.Keys.Matched;

        Add(2, "Outer Door");

        bindings.Refresh(_ship.World, program);
        Assert.Equal(settled + 1, _ship.World.Keys.Matched);

        bindings.Refresh(_ship.World, program);
        Assert.Equal(settled + 1, _ship.World.Keys.Matched);
    }

    /// <summary>A block no name reaches, welded on, leaves what the names point at as it was, so nothing worked out against them is worked out again.</summary>
    [Fact]
    public void ABlockNoNameReachesLeavesTheRevision()
    {
        var program = new Automation();
        program.Declare(_ship.World, Add(1, "Inner Door"));

        var bindings = new Bindings();
        bindings.Refresh(_ship.World, program);
        var settled = bindings.Revision;

        Add(2, "Outer Door");

        Assert.False(bindings.Refresh(_ship.World, program));
        Assert.Equal(settled, bindings.Revision);
    }

    /// <summary>A block a lost name finds again, welded on, moves the revision.</summary>
    [Fact]
    public void ABlockALostNameFindsMovesTheRevision()
    {
        var program = new Automation();
        var name = program.Declare(_ship.World, Add(1, "Inner Door")).Name;
        _ship.Grind(1);

        var bindings = new Bindings();
        bindings.Refresh(_ship.World, program);
        Assert.Null(bindings.Entity(name));
        var settled = bindings.Revision;

        Add(3, "Inner Door");

        Assert.True(bindings.Refresh(_ship.World, program));
        Assert.NotEqual(settled, bindings.Revision);
        Assert.NotNull(bindings.Entity(name));
    }

    /// <summary>And so is declaring something, which is the other half of the chain.</summary>
    [Fact]
    public void ANewDeclarationMakesItResolveOnce()
    {
        var program = new Automation();
        program.Declare(_ship.World, Add(1, "Inner Door"));

        var bindings = new Bindings();
        bindings.Refresh(_ship.World, program);
        var settled = bindings.Revision;

        program.Declare(_ship.World, Add(2, "Outer Door"));

        bindings.Refresh(_ship.World, program);
        Assert.NotEqual(settled, bindings.Revision);
    }

    /// <summary>A door welded on, as what Sprocket sees of it.</summary>
    GridEntity Add(long id, string name)
    {
        _ship.Door(id, name);
        return _ship.Entity(id);
    }

    EntityRef Declare(string name, GridEntity entity)
    {
        var declared = new EntityRef(name, entity.Kind.Id);
        _ship.World.Keys.Identify(entity, declared.Keys);
        return declared;
    }

    Bindings Resolve(params EntityRef[] refs)
    {
        var bindings = new Bindings();
        bindings.Resolve(_ship.World, refs);
        return bindings;
    }
}
