using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A block's toolbar, as a block on the rig carries one.
/// </summary>
/// <remarks>
/// The surface the whole trigger story rests on: a button panel raises nothing at all unless its
/// slot holds an item, and what Sprocket writes into a slot is the only record of which sequence
/// that button runs. So the harness keeps slots, their parameters, and nothing else.
/// <para>
/// Written out by hand rather than impersonated, because unlike a block this holds state and the
/// mod both reads and writes it.
/// </para>
/// </remarks>
public sealed class Toolbar : IMyToolbar
{
    readonly MyToolbarItemInfo[] _items;
    readonly bool[] _filled;
    readonly List<TerminalActionParameter>[] _parameters;

    readonly int _slots;
    readonly int _pages;

    /// <summary>A toolbar of one page of <paramref name="slots" /> slots, all of them empty, as a button panel's is.</summary>
    public Toolbar(int slots) : this(slots, 1)
    {
    }

    /// <summary>A toolbar of <paramref name="pages" /> pages of <paramref name="slots" /> slots each, all of them empty, as a cockpit's hotbar is nine of nine.</summary>
    public Toolbar(int slots, int pages)
    {
        _slots = slots;
        _pages = pages;
        _items = new MyToolbarItemInfo[slots * pages];
        _filled = new bool[slots * pages];
        _parameters = new List<TerminalActionParameter>[slots * pages];
    }

    /// <inheritdoc />
    public int SlotCount => _slots;

    /// <inheritdoc />
    public int PageCount => _pages;

    /// <summary>How many slots it has across its pages, filled or not, as the game counts them.</summary>
    public int ItemCount => SlotCount * PageCount;

    /// <inheritdoc />
    public MyToolbarType Type => MyToolbarType.ButtonPanel;

    /// <summary>The toolbar's own slots, standing for a gamepad's: a gamepad is not something the harness has.</summary>
    public IMyToolbarSlots GamepadSlots => this;

    /// <inheritdoc />
    public bool TryGetItem(int index, out MyToolbarItemInfo info)
    {
        if (index < 0 || index >= _items.Length || !_filled[index])
        {
            info = default(MyToolbarItemInfo);
            return false;
        }

        info = _items[index];
        return true;
    }

    /// <inheritdoc />
    public void GetItems(List<MyToolbarItemInfo> output)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            if (_filled[i])
                output.Add(_items[i]);
        }
    }

    /// <summary>Whether this slot holds anything, which is whether pressing it does anything.</summary>
    public bool Holds(int index)
    {
        return index >= 0 && index < _filled.Length && _filled[index];
    }

    /// <inheritdoc />
    public bool Trigger(int index)
    {
        return Holds(index);
    }

    /// <inheritdoc />
    public IMyToolbarChangeBuilder BeginChanges()
    {
        return Changing();
    }

    /// <summary>Begins a batch of changes to the toolbar, typed, so a write can carry what the item is to run with.</summary>
    public Changes Changing()
    {
        return new Changes(this);
    }

    /// <inheritdoc />
    public MyObjectBuilder_Toolbar GetObjectBuilder()
    {
        return null;
    }

    /// <summary>What the item in slot <paramref name="index" /> was given to run with, appended to <paramref name="output" />.</summary>
    public void Parameters(int index, List<TerminalActionParameter> output)
    {
        if (index < 0 || index >= _parameters.Length || _parameters[index] == null)
            return;

        for (var i = 0; i < _parameters[index].Count; i++)
            output.Add(_parameters[index][i]);
    }

    /// <summary>
    /// Changes waiting to be applied, the way the game takes them.
    /// </summary>
    /// <remarks>
    /// Nothing lands until <see cref="Apply" />, because that is the contract the mod is written
    /// against: it batches a slot's write and asks once whether the write took.
    /// </remarks>
    public sealed class Changes : IMyToolbarChangeBuilder
    {
        readonly Toolbar _toolbar;
        readonly List<int> _slots = new List<int>();
        readonly List<MyToolbarItemInfo> _items = new List<MyToolbarItemInfo>();
        readonly List<bool> _keeps = new List<bool>();
        readonly List<List<TerminalActionParameter>> _parameters =
            new List<List<TerminalActionParameter>>();

        /// <summary>No changes yet to <paramref name="toolbar" />.</summary>
        public Changes(Toolbar toolbar)
        {
            _toolbar = toolbar;
        }

        /// <inheritdoc />
        public IMyToolbarChangeBuilder Set(int index, MyToolbarItemInfo item)
        {
            return Set(index, item, null);
        }

        /// <summary>Puts <paramref name="item" /> in slot <paramref name="index" /> with what it is to run with, <paramref name="parameters" />, once applied.</summary>
        public IMyToolbarChangeBuilder Set(int index, MyToolbarItemInfo item,
            List<TerminalActionParameter>? parameters)
        {
            _slots.Add(index);
            _items.Add(item);
            _keeps.Add(true);
            _parameters.Add(parameters);
            return this;
        }

        /// <inheritdoc />
        public IMyToolbarChangeBuilder Clear(int index)
        {
            _slots.Add(index);
            _items.Add(default(MyToolbarItemInfo));
            _keeps.Add(false);
            _parameters.Add(null);
            return this;
        }

        /// <inheritdoc />
        public bool Apply()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot < 0 || slot >= _toolbar.ItemCount)
                    return false;

                _toolbar._filled[slot] = _keeps[i];
                _toolbar._items[slot] = _items[i];
                _toolbar._parameters[slot] = _parameters[i];
            }

            return true;
        }
    }
}
