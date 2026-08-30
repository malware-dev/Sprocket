using Reactor.Editing;
using Reactor.Sequences;
using Reactor.Runtime;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Machine;

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
    /// <summary>
    /// How often the program is written out, in ticks.
    /// </summary>
    /// <remarks>
    /// A second. Frequent enough that nothing anybody built is ever more than a moment from
    /// safety, rare enough that it is not a file write per frame - and the write is skipped
    /// entirely when the text has not changed, which is nearly always.
    /// </remarks>
    const int SaveEvery = 60;

    readonly Store _store;

    int _frame;

    public Simulation() : this(new Store("prototype"))
    {
    }

    /// <summary>A ship that remembers nothing, for tests.</summary>
    public static Simulation Fresh()
    {
        return new Simulation(Store.Nowhere());
    }

    public Simulation(Store store)
    {
        _store = store;

        World = DemoShip.Build();
        Program = new Automation();

        // Before the runner and the editor exist, so neither ever sees an empty program that
        // is about to be filled in underneath it.
        _store.Load(Program);

        Runner = new AutomationRunner(Program, World);
        Editor = new ReactorEditor(World, Program, Runner);
    }

    public SimWorld World { get; }

    public Automation Program { get; }

    public AutomationRunner Runner { get; }

    public ReactorEditor Editor { get; }

    /// <summary>Writes the program out now, whatever the clock says.</summary>
    public void Save() => _store.SaveIfChanged(Program);

    public string Where => _store.Path_;

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

        if (_frame % SaveEvery == 0)
            _store.SaveIfChanged(Program);
    }

    /// <summary>Entities the tester can poke, standing in for the world doing something.</summary>
    public IEnumerable<SimEntity> Pokeable()
    {
        foreach (var entity in World.SimEntities)
        {
            if (entity is SimSensor or SimButton or SimTank)
                yield return entity;
        }
    }
}
