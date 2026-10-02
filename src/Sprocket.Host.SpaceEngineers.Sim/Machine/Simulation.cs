using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Sim.Machine;

/// <summary>
/// The rig with Sprocket running on it.
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
    const int SaveEvery = 60;

    readonly Rig _rig;
    readonly Store _store;
    readonly SprocketUnit _unit;

    readonly Rota _rota = new Rota();

    readonly Backups _backups = new Backups();

    readonly Shelf _shelf = new Shelf();

    bool _rehoming;

    readonly Texts _texts = new Texts();

    readonly int _saidBefore;

    int _frame;

    public Simulation() : this(new Store("prototype"), Rig.Demo())
    {
    }

    /// <summary>A rig that remembers nothing, for tests and for captured frames.</summary>
    public static Simulation Fresh()
    {
        return new Simulation(Store.Nowhere(), Rig.Demo());
    }

    /// <summary>
    /// A rig on storage somebody else holds.
    /// </summary>
    /// <remarks>
    /// So a test can close a construct and open it again: the same storage, a new rig, a new unit -
    /// which is what a restart is, and the only way to catch the harness losing something the game
    /// would have kept.
    /// </remarks>
    internal static Simulation On(Store store)
    {
        return new Simulation(store, Rig.Demo());
    }

    /// <summary>A rig somebody else fitted, for a test that is about particular blocks.</summary>
    internal static Simulation On(Rig rig)
    {
        return new Simulation(Store.Nowhere(), rig);
    }

    /// <summary>
    /// The same storage and the same toolbars, and the block Sprocket runs in is a different block.
    /// </summary>
    /// <remarks>
    /// What cut and paste does: the program travels in the block's own storage, the construct keeps
    /// its slots, and the arrival is given a new id - so every slot names a block that is gone.
    /// </remarks>
    internal static Simulation Pasted(Store store)
    {
        return new Simulation(store, Rig.Demo(101));
    }

    Simulation(Store store, Rig rig)
    {
        _store = store;
        _rig = rig;
        _saidBefore = Told.Lines.Count;

        // Before the unit, which reads the program out of this the moment it is built.
        _rig.Install();
        _rig.Sprocket.Storage = store;

        // And before that again, because the unit's first look at the toolbars decides whether the
        // program's buttons are still wired. In the game those slots are in the save beside the
        // program; here they are a key beside it.
        string wired;
        if (store.TryGetValue(Wiring.Key, out wired))
            Wiring.Load(wired, _rig);

        // Said outright rather than read: the harness has no mod storage. A test that wants the
        // developer tools sets Editor.DeveloperMode on the way past.
        _unit = new SprocketUnit(_rig.Sprocket.Block, new Settings(developerMode: false), _rota, _texts, _backups);

        // The harness is always drawing the screen, which is what the unit ticks its editor for.
        _unit.Showing = true;
        _unit.Shelf = _shelf;

        // Answered on the next tick, which is where the game's invoke on the game thread lands: after the grid that
        // removed the hero has finished changing.
        _unit.HeroRemoved += delegate { _rehoming = true; };

        // A slot used invokes what it holds, and what that means is the unit's to say - which is
        // the game's arrangement exactly: the slot carries a key and Sprocket resolves it.
        Connect();

        // One tick before anybody can press anything, because that is the order in the game: the
        // session adopts a block as it comes alive, long before a screen is opened on it. Without it
        // the first tick a script takes is also the block's first, and the unit treats a sequence
        // built a moment ago as one loaded from a save whose buttons somebody has since cleared.
        Tick();
    }

    /// <summary>The Sprocket the construct carries, as the machine keeping it holds it.</summary>
    public SprocketUnit Unit => _unit;

    /// <summary>The world's backups, which the library lists.</summary>
    public Backups Backups => _backups;

    /// <summary>The player's library, kept in memory for as long as the harness runs.</summary>
    public Shelf Shelf => _shelf;

    /// <summary>What the editor says; loading a language into its book says it in that language.</summary>
    public Texts Texts => _texts;

    public SprocketEditor Editor => _unit.Editor;

    public GridWorld World => _unit.World;

    public AutomationRunner Runner => _unit.Runner;

    /// <summary>What the mod has written to the game's log since this began.</summary>
    public List<string> Log => Told.Lines.GetRange(_saidBefore, Told.Lines.Count - _saidBefore);

    /// <summary>The construct itself, for a test that has to weld, grind or cut the power.</summary>
    internal Rig Machine => _rig;

    /// <summary>Where the program is being kept, for the line at the bottom of the window.</summary>
    public string Where => _store.Where;

    /// <summary>Takes the construct's backup, as the world saving does, saying it was taken at <paramref name="saved" />.</summary>
    public void BackUp(DateTime saved)
    {
        _unit.Persist();
        _unit.BackUp(_backups, saved, false);
    }

    /// <summary>Writes the program and the wiring out now, whatever the clock says, and takes the backup.</summary>
    public void Save()
    {
        _unit.Persist();
        _store.SetValue(Wiring.Key, Wiring.Save(_rig));

        // And the backup, which the world saving takes too.
        if (!_unit.Lost)
            _unit.BackUp(_backups, DateTime.UtcNow, false);
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

        // A block with a toolbar welded after the rig was made is wired here rather than only in the constructor,
        // because the game wires one as it is built. Without this a button added later is a button
        // nobody hears, which looks exactly like Sprocket failing to claim the slot.
        Connect();

        if (_rehoming)
        {
            _rehoming = false;
            _unit.Rehome();

            // As the session does: nothing holds the automation after this, so its backup is taken from it now.
            if (_unit.Lost)
                _unit.BackUp(_backups, DateTime.UtcNow, true);
        }

        _rig.Tick();
        if (!_unit.Lost)
            _unit.Tick(_frame);

        // After the unit, as the session does it, so its bindings are current when its sequences run.
        _rota.Run(_frame);

        if (_frame % SaveEvery == 0)
            Save();
    }

    /// <summary>
    /// A slot of Sprocket's being invoked, on the block it names.
    /// </summary>
    /// <remarks>
    /// The game runs a toolbar action on a block, so a slot naming a block that is not here reaches
    /// nobody - and one naming a different Sprocket reaches that one's program, not this one. Both
    /// are what a construct looks like after somebody cut and pasted the block Sprocket runs in.
    /// </remarks>
    void Fire(long block, string key)
    {
        if (block == _unit.Block.EntityId)
            _unit.Fire(key);
    }

    /// <summary>
    /// Somebody pressing the first button of a named panel.
    /// </summary>
    /// <remarks>
    /// By name, because that is what a person looking at the ship has. A panel nobody wired raises
    /// nothing, which is the game's rule and worth testing rather than working around.
    /// </remarks>
    public void Press(string panel)
    {
        foreach (Panel found in Panels())
        {
            if (found.Name == panel)
            {
                found.Press(0);
                return;
            }
        }
    }

    /// <summary>Has every block with a toolbar that is not yet connected run what its slots hold through this simulation.</summary>
    void Connect()
    {
        foreach (var fitting in _rig.Fitted)
        {
            if (fitting.Toolbar == null || fitting.Invokes != null)
                continue;

            fitting.Invokes = Fire;
            fitting.Performs = _rig.Perform;
        }
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
