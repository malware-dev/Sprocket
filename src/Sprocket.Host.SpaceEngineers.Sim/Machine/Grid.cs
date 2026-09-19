using System;
using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using VRage.Game.ModAPI;

namespace Sprocket.Sim.Machine;

/// <summary>
/// The rig's one grid: what it is called, what is welded to it, and when that changed.
/// </summary>
/// <remarks>
/// The mod walks the grid group for blocks and listens to the grid for blocks appearing and
/// disappearing, so those are what this answers. Adding and removing here stands in for welding
/// something on and grinding it off, which is how a program comes to refer to a block that is no
/// longer there.
/// </remarks>
sealed class Grid
{
    readonly List<Fitting> _fitted = new List<Fitting>();
    readonly Dictionary<Fitting, IMySlimBlock> _slim = new Dictionary<Fitting, IMySlimBlock>();

    Action<IMySlimBlock> _added;
    Action<IMySlimBlock> _removed;

    public Grid(string name)
    {
        // Held by the block that answers for it rather than by this, because renaming a construct is
        // the mod writing to the grid: what it wrote is what the next read has to say.
        var called = name;

        Block = Pretend.To<IMyCubeGrid>()
            .Gives("CustomName", () => called)
            .Takes("CustomName", value => called = (string)value)
            .Gives("DisplayName", () => called)
            .Does("GetBlocks", Blocks)
            .Answers("add_OnBlockAdded", invocation => Listen(ref _added, invocation, true))
            .Answers("remove_OnBlockAdded", invocation => Listen(ref _added, invocation, false))
            .Answers("add_OnBlockRemoved", invocation => Listen(ref _removed, invocation, true))
            .Answers("remove_OnBlockRemoved", invocation => Listen(ref _removed, invocation, false))
            .Done();
    }

    public IMyCubeGrid Block { get; private set; }

    public IReadOnlyList<Fitting> Fitted => _fitted;

    /// <summary>What every block on this grid writes down, in the order it happened.</summary>
    public List<string> Journal { get; } = new List<string>();

    public T Weld<T>(T fitting) where T : Fitting
    {
        fitting.Grid = Block;
        fitting.Journal = Journal;
        _fitted.Add(fitting);
        _slim[fitting] = Pretend.To<IMySlimBlock>()
            .Gives("FatBlock", () => fitting.Block)
            .Gives("CubeGrid", () => Block)
            .Done();

        if (_added != null)
            _added(_slim[fitting]);

        return fitting;
    }

    /// <summary>Grinding one off, which leaves whatever named it pointing at nothing.</summary>
    public void Grind(Fitting fitting)
    {
        if (!_fitted.Remove(fitting))
            return;

        IMySlimBlock slim = _slim[fitting];
        _slim.Remove(fitting);

        if (_removed != null)
            _removed(slim);
    }

    public void Tick()
    {
        for (var i = 0; i < _fitted.Count; i++)
            _fitted[i].Tick();
    }

    object Blocks(object[] arguments)
    {
        var into = (List<IMySlimBlock>)arguments[0];
        for (var i = 0; i < _fitted.Count; i++)
            into.Add(_slim[_fitted[i]]);

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
