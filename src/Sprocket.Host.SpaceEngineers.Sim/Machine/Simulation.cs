using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Sim.Content;

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
public sealed class Simulation
{
    const int SaveEvery = 60;

    /// <summary>The identity of the player at the harness's screen, which is the one its stand-in session answers with.</summary>
    public const long Viewer = 0;

    readonly Rig _rig;
    readonly Store _store;
    SprocketUnit _unit;

    Roster _roster;

    readonly List<Action> _later = new List<Action>();

    Rota _rota = new Rota();

    readonly Folder _folder = new Folder();

    readonly Folder _worldFolder = new Folder();

    readonly Backups _backups;

    readonly Memories _memories = new Memories(new ProtoBinary());

    readonly Preferences _preferences;

    readonly Shelf _shelf;

    readonly Texts _texts = new Texts();

    Settings _settings = new Settings(developerMode: false);

    readonly int _saidBefore;

    int _frame;

    /// <summary>The demo rig, saving as a construct named "prototype".</summary>
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
    public static Simulation On(Store store)
    {
        return new Simulation(store, Rig.Demo());
    }

    /// <summary>A rig somebody else fitted, for a test that is about particular blocks.</summary>
    public static Simulation On(Rig rig)
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
    public static Simulation Pasted(Store store)
    {
        return new Simulation(store, Rig.Demo(101));
    }

    Simulation(Store store, Rig rig)
    {
        _store = store;
        _rig = rig;
        _shelf = new Shelf(_folder, delegate { });
        _backups = new Backups(_worldFolder);
        _backups.Read();
        _preferences = new Preferences(_folder, delegate { });
        _preferences.Read();
        _saidBefore = Told.Lines.Count;
        _texts.Book.Pages = new CodexFiles(CodexFiles.Beside, "");

        // Before the unit, which reads the program out of this the moment it is built.
        _rig.Install();
        _rig.Sprocket.Storage = store;

        // And before that again, because the unit's first look at the toolbars decides whether the
        // program's buttons are still wired. In the game those slots are in the save beside the
        // program; here they are a key beside it.
        string wired;
        if (store.TryGetValue(Wiring.Key, out wired))
            Wiring.Load(wired, _rig);

        Adopt();

        // A slot used invokes what it holds, and what that means is the unit's to say - which is
        // the game's arrangement exactly: the slot carries a key and Sprocket resolves it.
        Connect();

        // One tick before anybody can press anything, because that is the order in the game: the
        // session adopts a block as it comes alive, long before a screen is opened on it. Without it
        // the first tick a script takes is also the block's first, and the unit treats a sequence
        // built a moment ago as one loaded from a save whose buttons somebody has since cleared.
        Tick();
    }

    /// <summary>Takes on the rig's programmable blocks, as each comes alive in the game.</summary>
    void Adopt()
    {
        // Said outright rather than read: the harness has no mod storage. A test that wants the
        // developer tools sets Editor.DeveloperMode on the way past.
        _roster = new Roster(() => _settings, _rota, _texts, _memories, _backups, () => Now, _later.Add) { Shelf = _shelf };
        _unit = _roster.Adopt(_rig.Sprocket.Block);
        Dress(_unit);

        foreach (var fitting in _rig.InWorld)
        {
            if (fitting is Programmable && fitting != _rig.Sprocket && Locker.HasProgram(fitting.Block))
                _roster.Adopt(fitting.Block);
        }
    }

    /// <summary>Gives <paramref name="unit" /> what the session gives one as its screen opens.</summary>
    void Dress(SprocketUnit unit)
    {
        unit.Showing = true;
        unit.Shelf = _shelf;
        unit.Preferences = _preferences;
        unit.Viewer = _memories.Player(Viewer);
        unit.Administers = () => Admin;
        unit.WorldSettings = () => _settings;
        unit.ChangeSettings = Settle;
    }

    /// <summary>
    /// The world saved and loaded again: the blocks stay as they are, and everything of Sprocket's is built again from
    /// what was saved.
    /// </summary>
    /// <remarks>
    /// What a reload does to the mod. The game brings the grid back itself, toolbars and all, so the rig stands still;
    /// the unit, its runner and the world's rota are made anew, and the frame counter starts again from nought, as the
    /// game's own does with every session.
    /// </remarks>
    public void Reload()
    {
        Save();
        _roster.Dispose();
        _later.Clear();
        _rota = new Rota();
        _frame = 0;
        _memories.Read(_worldFolder.Has(Memories.File) ? _worldFolder.Read(Memories.File) : null, _frame);
        Adopt();
    }

    /// <summary>Sprocket's clock: the harness's frame carried on from where the world was saved, as the session keeps it.</summary>
    public int Now => _memories.Now(_frame);

    /// <summary>The Sprocket the construct carries.</summary>
    public SprocketUnit Unit
    {
        get
        {
            var now = _roster.Kept(_unit.Block);
            if (now != null && now != _unit)
            {
                _unit = now;
                Dress(_unit);
            }

            return _unit;
        }
    }

    /// <summary>Every Sprocket there is.</summary>
    public Roster Roster => _roster;

    /// <summary>The world's backups, which the library lists.</summary>
    public Backups Backups => _backups;

    /// <summary>How the player likes the screens to look, kept in <see cref="Folder" /> as the game keeps it.</summary>
    public Preferences Preferences => _preferences;

    /// <summary>The player's library, kept in memory for as long as the harness runs.</summary>
    public Shelf Shelf => _shelf;

    /// <summary>The storage folder the library is kept in.</summary>
    public Folder Folder => _folder;

    /// <summary>The world's storage folder, which the backups are kept in.</summary>
    public Folder WorldFolder => _worldFolder;

    /// <summary>What the editor says; loading a language into its book says it in that language.</summary>
    public Texts Texts => _texts;

    /// <summary>The editor on the rig's programmable block.</summary>
    public SprocketEditor Editor => Unit.Editor;

    /// <summary>Whether the player at the harness's screen is an admin with creative tools on, who sees the world's settings.</summary>
    public bool Admin { get; set; }

    /// <summary>The settings the harness's world runs by, as an admin last left them.</summary>
    public Settings Settings => _settings;

    /// <summary>The blocks the automation sees.</summary>
    public GridWorld World => Unit.World;

    /// <summary>Runs the harness's world by <paramref name="next" /> from now on, as the server does when an admin changes them.</summary>
    public void Settle(Settings next)
    {
        _settings = next;
        foreach (var unit in _roster.All)
            unit.Apply(next);
    }

    /// <summary>What runs the automation's sequences.</summary>
    public AutomationRunner Runner => Unit.Runner;

    /// <summary>What the mod has written to the game's log since this began.</summary>
    public List<string> Log => Told.Lines.GetRange(_saidBefore, Told.Lines.Count - _saidBefore);

    /// <summary>The construct itself, for a test that has to weld, grind or cut the power.</summary>
    public Rig Machine => _rig;

    /// <summary>Where the program is being kept, for the line at the bottom of the window.</summary>
    public string Where => _store.Where;

    /// <summary>Takes the construct's backup, as losing its automation does, saying it was taken at <paramref name="saved" />.</summary>
    public void BackUp(DateTime saved)
    {
        Unit.Persist();
        Unit.BackUp(_backups, saved, _shelf);
    }

    /// <summary>Writes the program, the wiring and what the world remembers out now, whatever the clock says.</summary>
    public void Save()
    {
        _roster.Persist();
        _store.SetValue(Wiring.Key, Wiring.Save(_rig));
        var remembered = new List<ConstructMemory>();
        foreach (var unit in _roster.All)
        {
            if (!unit.Lost)
                remembered.Add(unit.Memory);
        }

        _worldFolder.Write(Memories.File, _memories.Write(_frame, remembered, new List<DoorMemory>()));
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

        // Where the game's invoke on its own thread lands.
        var later = new List<Action>(_later);
        _later.Clear();
        foreach (var action in later)
            action();

        _rig.Tick();
        _roster.Tick(Now);

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
        foreach (var unit in _roster.All)
        {
            if (unit.Block.EntityId == block)
            {
                unit.Fire(key);
                return;
            }
        }
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
        foreach (var fitting in _rig.InWorld)
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
        foreach (var fitting in _rig.InWorld)
        {
            var panel = fitting as Panel;
            if (panel != null)
                yield return panel;
        }
    }
}
