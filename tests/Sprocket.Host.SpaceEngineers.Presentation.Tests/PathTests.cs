using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block is found again by where it stands, so a copy of a grid - pasted, or built from a blueprint -
/// finds its own blocks.
/// </summary>
/// <remarks>
/// A copy is stood in for by grinding a block off and welding another in its cell with a new id, which
/// is what a paste looks like from the program's side: every id new, every block where it was.
/// </remarks>
public class PathTests
{
    /// <summary>A declared block is kept by its id, then its path, then its name, strongest first.</summary>
    [Fact]
    public void ADeclaredBlockIsKeyedByItsIdThenItsPathThenItsName()
    {
        var rig = new Rig();
        Door door = rig.Door(2, "Airlock Inner Door");
        var sim = Simulation.On(rig);
        sim.Tick();

        var keys = Declared(sim, door).Keys;

        Assert.Equal(3, keys.Count);
        Assert.Equal(ValueKind.Integer, keys[0].Kind);
        Assert.Equal(new[] { door.Cell!.Value }, keys[1].Cells);
        Assert.Null(keys[2].Cells);
        Assert.Equal(ValueKind.Text, keys[2].Kind);
    }

    /// <summary>A copy of a block is found by where it stands, counts as found, and takes its new id.</summary>
    [Fact]
    public void ACopyIsFoundByWhereItStandsAndTakesItsNewId()
    {
        var rig = new Rig();
        Door door = rig.Door(2, "Airlock Inner Door");
        var sim = Simulation.On(rig);
        sim.Tick();
        var declared = Declared(sim, door);

        Door copy = Copied(rig, door, 7);
        sim.Tick();

        Assert.Equal(7, sim.Runner.Bindings.Entity(declared.Name)!.Id);
        Assert.Equal(RefState.Bound, sim.Runner.Bindings.State(declared.Name));

        long id;
        sim.World.Values.TryInteger(declared.Keys[0].Kind, declared.Keys[0].Value, out id);
        Assert.Equal(copy.Id, id);
    }

    /// <summary>A block on a rotor's head is found through the rotor, and so is a copy of it.</summary>
    [Fact]
    public void ABlockOnARotorsHeadIsFoundThroughTheRotor()
    {
        var rig = new Rig();
        Rotor rotor = rig.Rotor(3, "Arm Rotor");
        rig.OnHeadOf(rotor);
        Door door = rig.Door(4, "Arm Door");
        rig.OnBase();

        var sim = Simulation.On(rig);
        sim.Tick();
        var declared = Declared(sim, door);

        Assert.Equal(new[] { rotor.Cell!.Value, door.Cell!.Value }, declared.Keys[1].Cells);

        rig.OnHeadOf(rotor);
        Copied(rig, door, 8);
        rig.OnBase();
        sim.Tick();

        Assert.Equal(8, sim.Runner.Bindings.Entity(declared.Name)!.Id);
        Assert.Equal(RefState.Bound, sim.Runner.Bindings.State(declared.Name));
    }

    /// <summary>A block found by its name alone is a suggestion, not a match: two blocks can share one.</summary>
    [Fact]
    public void ABlockFoundByItsNameAloneIsOnlySuggested()
    {
        var rig = new Rig();
        Door door = rig.Door(2, "Airlock Inner Door");
        var sim = Simulation.On(rig);
        sim.Tick();
        var declared = Declared(sim, door);

        // Somewhere else on the grid, so neither the id nor the path finds it.
        Door moved = rig.Door(9, "Airlock Inner Door");
        rig.Grind(door);
        sim.Tick();

        Assert.Equal(moved.Id, sim.Runner.Bindings.Entity(declared.Name)!.Id);
        Assert.Equal(RefState.Adrift, sim.Runner.Bindings.State(declared.Name));
    }

    /// <summary>
    /// The keys are tried in the order the program lists them: a name written first is the author's
    /// first choice, and a block it finds is found.
    /// </summary>
    [Fact]
    public void ANameWrittenFirstIsTrusted()
    {
        var rig = new Rig();
        rig.Door(2, "Airlock Inner Door");
        var sim = Simulation.On(rig);
        sim.Tick();

        Load(sim, "[ \"Airlock Inner Door\", 999 ]");
        sim.Tick();

        Assert.Equal(2, sim.Runner.Bindings.Entity("door1")!.Id);
        Assert.Equal(RefState.Bound, sim.Runner.Bindings.State("door1"));
    }

    /// <summary>A copy's new id goes where its id key already stands, and no key moves.</summary>
    [Fact]
    public void ACopysIdIsReplacedWhereItStands()
    {
        var rig = new Rig();
        Door door = rig.Door(2, "Airlock Inner Door");
        var sim = Simulation.On(rig);
        sim.Tick();

        Vector3I cell = door.Cell!.Value;
        Load(sim, "[ \"Somebody Else's Door\", 2, [ (" + cell.X + " " + cell.Y + " " + cell.Z + ") ] ]");
        Copied(rig, door, 7);
        sim.Tick();

        var keys = sim.Editor.Program.FindRef("door1")!.Keys;
        Assert.Equal(ValueKind.Text, keys[0].Kind);
        Assert.Null(keys[0].Cells);

        long id;
        sim.World.Values.TryInteger(keys[1].Kind, keys[1].Value, out id);
        Assert.Equal(7, id);
        Assert.Equal(new[] { cell }, keys[2].Cells);
    }

    /// <summary>Loads a program declaring one door, <c>door1</c>, found by <paramref name="keys" />.</summary>
    static void Load(Simulation sim, string keys)
    {
        var program = "sprocket 1 alpha 10\n\nlet door1 = block 'Door/LargeBlockDoor' " + keys + "\n";
        Assert.Equal("", Storage.ProgramText.Load(program, sim.Editor.Program, sim.World.Values));
    }

    static EntityRef Declared(Simulation sim, Fitting fitting)
    {
        return sim.Editor.Program.Declare(sim.World, sim.World.FindEntity(fitting.Id));
    }

    /// <summary>Grinds <paramref name="door" /> off and welds a door with a new id into its cell.</summary>
    static Door Copied(Rig rig, Door door, long id)
    {
        Vector3I cell = door.Cell!.Value;
        rig.Grind(door);
        Door copy = rig.Door(id, door.Name);
        copy.Cell = cell;
        return copy;
    }
}
