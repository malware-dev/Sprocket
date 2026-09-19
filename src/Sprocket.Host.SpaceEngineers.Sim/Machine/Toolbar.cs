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
sealed class Toolbar : IMyToolbar
{
    readonly MyToolbarItemInfo[] _items;
    readonly bool[] _filled;
    readonly List<TerminalActionParameter>[] _parameters;

    public Toolbar(int slots)
    {
        _items = new MyToolbarItemInfo[slots];
        _filled = new bool[slots];
        _parameters = new List<TerminalActionParameter>[slots];
    }

    public int SlotCount => _items.Length;

    public int PageCount => 1;

    public int ItemCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < _filled.Length; i++)
            {
                if (_filled[i])
                    count++;
            }

            return count;
        }
    }

    public MyToolbarType Type => MyToolbarType.ButtonPanel;

    /// <summary>The same slots. A gamepad is not something the harness has.</summary>
    public IMyToolbarSlots GamepadSlots => this;

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

    public bool Trigger(int index)
    {
        return Holds(index);
    }

    public IMyToolbarChangeBuilder BeginChanges()
    {
        return Changing();
    }

    /// <summary>The same, typed, so a write can carry what the item is to run with.</summary>
    public Changes Changing()
    {
        return new Changes(this);
    }

    public MyObjectBuilder_Toolbar GetObjectBuilder()
    {
        return null;
    }

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

        public Changes(Toolbar toolbar)
        {
            _toolbar = toolbar;
        }

        public IMyToolbarChangeBuilder Set(int index, MyToolbarItemInfo item)
        {
            return Set(index, item, null);
        }

        public IMyToolbarChangeBuilder Set(int index, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters)
        {
            _slots.Add(index);
            _items.Add(item);
            _keeps.Add(true);
            _parameters.Add(parameters);
            return this;
        }

        public IMyToolbarChangeBuilder Clear(int index)
        {
            _slots.Add(index);
            _items.Add(default(MyToolbarItemInfo));
            _keeps.Add(false);
            _parameters.Add(null);
            return this;
        }

        public bool Apply()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot < 0 || slot >= _toolbar.SlotCount)
                    return false;

                _toolbar._filled[slot] = _keeps[i];
                _toolbar._items[slot] = _items[i];
                _toolbar._parameters[slot] = _parameters[i];
            }

            return true;
        }
    }
}
