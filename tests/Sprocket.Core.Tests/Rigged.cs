using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;

namespace Sprocket.Tests;

/// <summary>
/// A construct to test against: real blocks, and the mod's own world over them.
/// </summary>
/// <remarks>
/// There used to be a second world here - a hand-written one implementing the same interface - and
/// with it a second vocabulary, a second set of blocks, and a standing question about which of the
/// two a passing test was really about. The harness builds a construct properly now, so this is the
/// same rig the frames and the window run on, fitted with whatever a test is about.
/// </remarks>
sealed class Rigged
{
    public Rigged()
    {
        Rig = new Rig();

        // The gateway is the game's, and a static, so a rig has to be the one standing behind it
        // while a test runs. That is why these tests do not run in parallel - see Serially.cs.
        Rig.Install();

        World = new GridWorld(Rig.Sprocket.Block);
    }

    public Rig Rig { get; private set; }

    public GridWorld World { get; private set; }

    public Values Values => World.Values;

    /// <summary>A door on the construct, and the block a test pokes at.</summary>
    public Door Door(long id, string name)
    {
        return Rig.Door(id, name);
    }

    /// <summary>A hangar door on the construct.</summary>
    public Door HangarDoor(long id, string name)
    {
        return Rig.HangarDoor(id, name);
    }

    public Vent Vent(long id, string name)
    {
        return Rig.Vent(id, name);
    }

    public Rotor Rotor(long id, string name)
    {
        return Rig.Rotor(id, name);
    }

    public Panel Panel(long id, string name, int buttons = 1)
    {
        return Rig.Panel(id, name, buttons);
    }

    public Lamp Light(long id, string name)
    {
        return Rig.Light(id, name);
    }

    public Beacon Beacon(long id, string name)
    {
        return Rig.Beacon(id, name);
    }

    public Rotor Hinge(long id, string name)
    {
        return Rig.Hinge(id, name);
    }

    public Piston Piston(long id, string name)
    {
        return Rig.Piston(id, name);
    }

    public Battery Battery(long id, string name)
    {
        return Rig.Battery(id, name);
    }

    public Tank Tank(long id, string name)
    {
        return Rig.Tank(id, name);
    }

    public Speaker Speaker(long id, string name)
    {
        return Rig.Speaker(id, name);
    }

    /// <summary>What Sprocket sees of a block, which is what a program refers to.</summary>
    public GridEntity Entity(long id)
    {
        return World.FindEntity(id);
    }

    /// <summary>
    /// Grinds a block off, leaving whatever named it pointing at nothing.
    /// </summary>
    /// <remarks>
    /// The world is read once afterwards, because that is what noticing costs: the grid says a block
    /// went, and the world rebuilds its list the next time anything asks for it rather than there and
    /// then. In the game the unit asks every tick. A test that skipped this would be testing a world
    /// that had not looked yet.
    /// </remarks>
    public void Grind(Fitting fitting)
    {
        Rig.Grind(fitting);
        Looked();
    }

    /// <summary>What a tick does: ask the world what is on the construct.</summary>
    public void Looked()
    {
        // The reading is the point - asking is what makes the world notice the grid changed.
        Xunit.Assert.NotNull(World.Entities);
    }

    /// <summary>The same, for a test that only kept the block's id.</summary>
    public void Grind(long id)
    {
        foreach (Fitting fitting in Rig.Fitted)
        {
            if (fitting.Id == id)
            {
                Grind(fitting);
                return;
            }
        }
    }

    /// <summary>A moment of the construct running: doors move, vents fill.</summary>
    public void Tick()
    {
        Rig.Tick();
    }

    /// <summary>
    /// Says that nothing here can start this sequence, which is what the unit says by looking.
    /// </summary>
    /// <remarks>
    /// Through the world's own surface rather than a set a test can poke: the real one is told the
    /// whole picture at once, because that is how it is worked out - one pass over the toolbars.
    /// </remarks>
    public void CannotStart(Sequence sequence)
    {
        var start = Start(sequence);
        if (!_unstartable.Contains(start))
            _unstartable.Add(start);

        World.Unstartable(_unstartable, _unarranged);
    }

    public void CanStart(Sequence sequence)
    {
        _unstartable.Remove(Start(sequence));
        World.Unstartable(_unstartable, _unarranged);
    }

    /// <summary>The sequence on the one of its system the editor shows first.</summary>
    static long Start(Sequence sequence) => GridWorld.Start(sequence.Id, sequence.System?.First);

    readonly List<long> _unstartable = new List<long>();

    readonly Dictionary<long, Unanswered> _unarranged = new Dictionary<long, Unanswered>();
}
