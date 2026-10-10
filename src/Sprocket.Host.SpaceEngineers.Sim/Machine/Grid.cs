using System;
using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sprocket.Sim.Machine;

/// <summary>
/// One of the rig's grids: what it is called, what is welded to it, and when that changed.
/// </summary>
/// <remarks>
/// The mod walks the grid group for blocks and listens to the grid for blocks appearing and
/// disappearing, so those are what this answers. Adding and removing here stands in for welding
/// something on and grinding it off, which is how a program comes to refer to a block that is no
/// longer there.
/// </remarks>
public sealed class Grid
{
    readonly List<Fitting> _fitted = new List<Fitting>();
    readonly Dictionary<Fitting, IMySlimBlock> _slim = new Dictionary<Fitting, IMySlimBlock>();

    /// <summary>The tops of mechanical joints standing on this grid, which are blocks but not terminal ones.</summary>
    readonly List<IMySlimBlock> _tops = new List<IMySlimBlock>();

    /// <summary>The next cell handed to a block welded without one, along one row.</summary>
    int _next;

    bool _closed;

    Action<IMySlimBlock> _added;
    Action<IMySlimBlock> _removed;

    /// <summary>A grid called <paramref name="name" />, <paramref name="id" /> its entity id, with a journal of its own.</summary>
    public Grid(string name, long id = 9000) : this(name, new List<string>(), id)
    {
    }

    /// <summary>A grid, <paramref name="id" /> its entity id, writing what is done to its blocks into <paramref name="journal" />, which several grids can share.</summary>
    public Grid(string name, List<string> journal, long id)
    {
        Journal = journal;

        // Held by the block that answers for it rather than by this, because renaming a construct is
        // the mod writing to the grid: what it wrote is what the next read has to say.
        var called = name;

        Block = Pretend.To<IMyCubeGrid>()
            .Gives("EntityId", () => id)
            .Gives("CustomName", () => called)
            .Takes("CustomName", value => called = (string)value)
            .Gives("DisplayName", () => called)
            .Gives("GridSizeEnum", () => MyCubeSize.Large)
            .Gives("Closed", () => _closed)
            .Gives("MarkedForClose", () => _closed)
            .Does("GetBlocks", Blocks)
            .Answers("GetCubeBlock", invocation => At((Vector3I)invocation.Arguments[0]))
            .Answers("add_OnBlockAdded", invocation => Listen(ref _added, invocation, true))
            .Answers("remove_OnBlockAdded", invocation => Listen(ref _added, invocation, false))
            .Answers("add_OnBlockRemoved", invocation => Listen(ref _removed, invocation, true))
            .Answers("remove_OnBlockRemoved", invocation => Listen(ref _removed, invocation, false))
            .Does("add_OnBlockIntegrityChanged", () => null)
            .Does("remove_OnBlockIntegrityChanged", () => null)
            .Does("add_OnGridBlockDamaged", () => null)
            .Does("remove_OnGridBlockDamaged", () => null)
            .Done();
    }

    /// <summary>What the game would hand the mod for this grid, a large one as every grid on the rig is.</summary>
    public IMyCubeGrid Block { get; private set; }

    /// <summary>The blocks welded to this grid, in the order they were welded.</summary>
    public IReadOnlyList<Fitting> Fitted => _fitted;

    /// <summary>What every block on this grid writes down, in the order it happened.</summary>
    public List<string> Journal { get; private set; }

    /// <summary>Welds <paramref name="fitting" /> to this grid, and hands it back.</summary>
    public T Weld<T>(T fitting) where T : Fitting
    {
        fitting.Grid = Block;
        fitting.Journal = Journal;
        if (fitting.Cell == null)
            fitting.Cell = new Vector3I(_next++, 0, 0);

        _fitted.Add(fitting);
        _slim[fitting] = Pretend.To<IMySlimBlock>()
            .Gives("FatBlock", () => fitting.Block)
            .Gives("CubeGrid", () => Block)
            .Done();
        fitting.Slim = _slim[fitting];

        if (_added != null)
            _added(_slim[fitting]);

        return fitting;
    }

    /// <summary>Grinding one off, which leaves whatever named it pointing at nothing.</summary>
    /// <remarks>The game reports the block removed from the grid, then closes it.</remarks>
    public void Grind(Fitting fitting)
    {
        if (!_fitted.Remove(fitting))
            return;

        IMySlimBlock slim = _slim[fitting];
        _slim.Remove(fitting);

        if (_removed != null)
            _removed(slim);

        fitting.Close();
    }

    /// <summary>Deleting the grid whole, as a player in creative or the server's cleanup does.</summary>
    /// <remarks>The game closes the grid and every block on it, and reports none of them removed.</remarks>
    public void Delete()
    {
        _closed = true;
        for (var i = 0; i < _fitted.Count; i++)
            _fitted[i].Close();
    }

    /// <summary>A moment of every block on this grid running.</summary>
    public void Tick()
    {
        for (var i = 0; i < _fitted.Count; i++)
            _fitted[i].Tick();
    }

    /// <summary>
    /// Stands the top of <paramref name="under" /> on this grid, which is what makes this grid the
    /// joint's other side.
    /// </summary>
    public void Top(IMyMechanicalConnectionBlock under)
    {
        var cell = new Vector3I(_next++, 0, 0);
        IMyCubeGrid grid = Block;
        IMyMotorRotor top = Pretend.To<IMyMotorRotor>()
            .Gives("Base", () => under)
            .Gives("Position", () => cell)
            .Gives("CubeGrid", () => grid)
            .Done();

        _tops.Add(Pretend.To<IMySlimBlock>()
            .Gives("FatBlock", () => top)
            .Gives("CubeGrid", () => grid)
            .Done());
    }

    object Blocks(object[] arguments)
    {
        var into = (List<IMySlimBlock>)arguments[0];
        for (var i = 0; i < _fitted.Count; i++)
            into.Add(_slim[_fitted[i]]);

        for (var i = 0; i < _tops.Count; i++)
            into.Add(_tops[i]);

        return null;
    }

    /// <summary>The block standing at <paramref name="cell" />, or null.</summary>
    IMySlimBlock At(Vector3I cell)
    {
        for (var i = 0; i < _fitted.Count; i++)
        {
            if (_fitted[i].Cell == cell)
                return _slim[_fitted[i]];
        }

        for (var i = 0; i < _tops.Count; i++)
        {
            if (_tops[i].FatBlock.Position == cell)
                return _tops[i];
        }

        return null;
    }

    static object Listen(ref Action<IMySlimBlock> held, Castle.DynamicProxy.IInvocation invocation,
        bool adding)
    {
        var handler = (Delegate)invocation.Arguments[0];
        held = (Action<IMySlimBlock>)(adding
            ? Delegate.Combine(held, handler)
            : Delegate.Remove(held, handler));

        return null;
    }
}
