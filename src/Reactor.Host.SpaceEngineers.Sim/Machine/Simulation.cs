using System.Collections.Generic;
using Reactor.Editing;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Sim.Machine;

/// <summary>
/// The rig with Reactor running on it.
/// </summary>
/// <remarks>
/// <b>The mod's own unit, not a stand-in for it.</b> Everything below the window - the world, the
/// program, the runner, the editor, the toolbars, the storage - is the code the game runs, driven
/// over blocks the rig makes up. What this supplies is what the game supplies: a construct, and a
/// clock.
/// <para>
/// Deliberately separate from any window, so the whole editor can be driven from a test without one.
/// </para>
/// </remarks>
sealed class Simulation
{
    /// <summary>
    /// How often the program is written out, in ticks.
    /// </summary>
    /// <remarks>
    /// A second. Frequent enough that nothing anybody built is ever more than a moment from safety,
    /// rare enough that it is not a file write per frame - and the write is skipped entirely when
    /// the text has not changed, which is nearly always.
    /// </remarks>
    const int SaveEvery = 60;

    readonly Rig _rig = new Rig();
    readonly Store _store;
    readonly ReactorUnit _unit;

    int _frame;

    public Simulation() : this(new Store("prototype"))
    {
    }

    /// <summary>A rig that remembers nothing, for tests and for captured frames.</summary>
    public static Simulation Fresh()
    {
        return new Simulation(Store.Nowhere());
    }

    Simulation(Store store)
    {
        _store = store;

        // Before the unit, which reads the program out of this the moment it is built.
        _rig.Install();
        _rig.Reactor.Storage = store;

        _unit = new ReactorUnit(_rig.Reactor.Block);

        // One tick before anybody can press anything, because that is the order in the game: the
        // session adopts a block as it comes alive, long before a screen is opened on it. Without it
        // the first tick a script takes is also the block's first, and the unit treats a sequence
        // built a moment ago as one loaded from a save whose buttons somebody has since cleared.
        Tick();
    }

    public ReactorEditor Editor => _unit.Editor;

    public GridWorld World => _unit.World;

    /// <summary>Where the program is being kept, for the line at the bottom of the window.</summary>
    public string Where => _store.Where;

    /// <summary>Writes the program out now, whatever the clock says.</summary>
    public void Save()
    {
        _unit.Persist();
    }

    /// <summary>
    /// One tick. The rig moves first so the sequences always sample signals that are current for
    /// this tick rather than the last one.
    /// </summary>
    public void Tick()
    {
        // Standing in for the game's frame counter. This host is woken every frame so its own count
        // happens to be exact; the mod reads Space Engineers' own counter instead, because its
        // slower update rates do not land on exact multiples and a self-counted tick would drift
        // with server load.
        _frame++;

        _rig.Tick();
        _unit.Tick(_frame);

        if (_frame % SaveEvery == 0)
            _unit.Persist();
    }

    /// <summary>The buttons on the rig, for the row of pokes along the bottom of the window.</summary>
    public IEnumerable<Panel> Panels()
    {
        foreach (var fitting in _rig.Fitted)
        {
            var panel = fitting as Panel;
            if (panel != null)
                yield return panel;
        }
    }
}
