using Reactor.Editing;
using Reactor.Sequences;
using Reactor.Runtime;

namespace Reactor.Host.Machine;

/// <summary>
/// A simulated ship with Reactor running on it.
/// </summary>
/// <remarks>
/// Everything a target has to supply: a world, somewhere to keep the program, and a
/// clock. Space Engineers supplies the first from the grid and the last from its
/// update loop; here both are made up.
///
/// Deliberately separate from any window, so the whole editor can be driven from a
/// test without one.
/// </remarks>
sealed class Simulation
{
    int _frame;

    public Simulation()
    {
        World = DemoShip.Build();
        Program = new Automation();
        Runner = new AutomationRunner(Program, World);
        Editor = new ReactorEditor(World, Program, Runner);
    }

    public SimWorld World { get; }

    public Automation Program { get; }

    public AutomationRunner Runner { get; }

    public ReactorEditor Editor { get; }

    /// <summary>
    /// One tick. The ship moves first so the sequences always sample signals that are
    /// current for this tick rather than the last one.
    /// </summary>
    public void Tick()
    {
        // Standing in for the game's frame counter. This host is woken every frame so
        // its own count happens to be exact; the mod must read Space Engineers' own
        // counter instead, because its slower update rates do not land on exact
        // multiples and a self-counted tick would drift with server load.
        _frame++;

        World.Tick();
        Editor.Tick(_frame);
    }

    /// <summary>Devices the tester can poke, standing in for the world doing something.</summary>
    public IEnumerable<SimDevice> Pokeable()
    {
        foreach (var device in World.SimDevices)
        {
            if (device is SimSensor or SimButton or SimTank)
                yield return device;
        }
    }
}
