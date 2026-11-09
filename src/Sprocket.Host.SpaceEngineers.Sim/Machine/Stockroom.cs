using System;
using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A block holding items: a cargo container with one inventory, or a refinery with an input and an output.
/// </summary>
/// <remarks>
/// Each inventory answers how much it holds of the items written into it, by subtype, and tells whoever listens as
/// that changes, which is how the game says the same.
/// </remarks>
public sealed class Stockroom : Fitting
{
    // Which room's inventory a proxy is, since a move names the inventory it goes to and nothing else. Keyed weakly, so
    // a rig gone takes its inventories with it.
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IMyInventory, Stockroom> Rooms =
        new System.Runtime.CompilerServices.ConditionalWeakTable<IMyInventory, Stockroom>();

    readonly List<Dictionary<string, double>> _held = new List<Dictionary<string, double>>();
    double _room = double.MaxValue;
    readonly List<IMyInventory> _inventories = new List<IMyInventory>();
    readonly List<Action<IMyInventory, float, float>> _heard = new List<Action<IMyInventory, float, float>>();
    readonly List<VRage.Game.ModAPI.Ingame.MyItemType> _accepted = new List<VRage.Game.ModAPI.Ingame.MyItemType>();

    /// <summary>A block known by <paramref name="id" /> and called <paramref name="name" />, of the game's <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />, with <paramref name="inventories" /> inventories.</summary>
    public Stockroom(long id, string name, MyObjectBuilderType type, string subtype, string display, int inventories)
        : base(id, name, type, subtype, display)
    {
        for (var i = 0; i < inventories; i++)
        {
            var held = new Dictionary<string, double>();
            _held.Add(held);
            var inventory = Inventory(held);
            _inventories.Add(inventory);
            Rooms.Add(inventory, this);
        }

        if (inventories > 1)
            Fit(Pretend.To<IMyRefinery>()
                .Gives("IsProducing", () => false)
                .Gives("IsQueueEmpty", () => true)
                .Gives("InputInventory", () => _inventories[0])
                .Gives("OutputInventory", () => _inventories[1])
                .Does("add_StartedProducing", () => null)
                .Does("remove_StartedProducing", () => null)
                .Does("add_StoppedProducing", () => null)
                .Does("remove_StoppedProducing", () => null)
                .Does("GetInventory", args => _inventories[(int)args[0]]));
        else
            Fit(Pretend.To<IMyCargoContainer>().Does("GetInventory", args => _inventories[(int)args[0]]));
    }

    /// <inheritdoc />
    protected override int Inventories => _inventories.Count;

    /// <summary>Puts <paramref name="amount" /> of the item <paramref name="subtype" /> in the inventory at <paramref name="at" />, as a conveyor does.</summary>
    public void Holds(string subtype, double amount, int at = 0)
    {
        _held[at][subtype] = amount;
        foreach (var heard in _heard.ToArray())
            heard(_inventories[at], 0f, 0f);
    }

    /// <summary>How much of <paramref name="subtype" /> the inventory at <paramref name="at" /> holds.</summary>
    public double Holding(string subtype, int at = 0)
    {
        double amount;
        return _held[at].TryGetValue(subtype, out amount) ? amount : 0;
    }

    /// <summary>How much in all each of its inventories takes before it is full.</summary>
    public void Room(double amount)
    {
        _room = amount;
    }

    /// <summary>Whether a conveyor carries items from its inventories to anywhere; it does until told otherwise.</summary>
    public bool Connected { get; set; } = true;

    /// <summary>How full each of its inventories says it is, from 0 to 1.</summary>
    public float FillFactor { get; set; }

    double Free(Dictionary<string, double> held)
    {
        var used = 0d;
        foreach (var amount in held.Values)
            used += amount;

        return _room - used;
    }

    /// <summary>Takes up to <paramref name="amount" /> of the item into <paramref name="into" />, as much as fits, and answers how much; none of an item its list of accepted items lacks, where it has a list.</summary>
    double Takes(IMyInventory into, string subtype, double amount)
    {
        if (_accepted.Count > 0 && !_accepted.Contains(ItemType(subtype)))
            return 0;

        var held = _held[_inventories.IndexOf(into)];
        var taken = Math.Max(0, Math.Min(amount, Free(held)));
        Holds(subtype, Holding(subtype, _inventories.IndexOf(into)) + taken, _inventories.IndexOf(into));
        return taken;
    }

    /// <summary>Lets its inventories take the item <paramref name="item" />.</summary>
    public void Accepts(VRage.Game.MyDefinitionId item)
    {
        _accepted.Add(item);
    }

    IMyInventory Inventory(Dictionary<string, double> held)
    {
        return Pretend.To<IMyInventory>()
            .Gives("VolumeFillFactor", () => FillFactor)
            .Gives("CurrentMass", () => (MyFixedPoint)Total(held))
            .Gives("ItemCount", () => Stacks(held).Count)
            .Does("GetItemAt", args =>
            {
                var stacks = Stacks(held);
                var at = (int)args[0];
                return at < stacks.Count
                    ? new VRage.Game.ModAPI.Ingame.MyInventoryItem(ItemType(stacks[at]), (uint)at, (MyFixedPoint)held[stacks[at]])
                    : null;
            })
            .Does("CanTransferItemTo", _ => Connected)
            .Does("TransferItemTo", args =>
            {
                if (!Connected)
                    return false;

                var subtype = Stacks(held)[(int)args[1]];
                var target = (IMyInventory)args[0];
                Stockroom room;
                if (!Rooms.TryGetValue(target, out room))
                    return false;

                var taken = room.Takes(target, subtype, (double)(MyFixedPoint)args[4]);
                Holds(subtype, held[subtype] - taken, _held.IndexOf(held));
                return true;
            })
            .Does("GetAcceptedItems", args =>
            {
                ((List<VRage.Game.ModAPI.Ingame.MyItemType>)args[0]).AddRange(_accepted);
                return null;
            })
            .Does("GetItemAmount", args =>
            {
                double amount;
                return held.TryGetValue(((VRage.Game.ModAPI.Ingame.MyItemType)args[0]).SubtypeId, out amount)
                    ? (MyFixedPoint)amount
                    : 0;
            })
            .Does("add_OnVolumeChanged", args =>
            {
                _heard.Add((Action<IMyInventory, float, float>)args[0]);
                return null;
            })
            .Does("remove_OnVolumeChanged", args =>
            {
                _heard.Remove((Action<IMyInventory, float, float>)args[0]);
                return null;
            })
            .Done();
    }

    /// <summary>How much of everything an inventory holds, which stands for its mass.</summary>
    static double Total(Dictionary<string, double> held)
    {
        var total = 0d;
        foreach (var item in held)
            total += item.Value;

        return total;
    }

    /// <summary>The items an inventory holds any of, as the game lists its stacks.</summary>
    static List<string> Stacks(Dictionary<string, double> held)
    {
        var stacks = new List<string>();
        foreach (var item in held)
        {
            if (item.Value > 0)
                stacks.Add(item.Key);
        }

        return stacks;
    }

    static VRage.Game.ModAPI.Ingame.MyItemType ItemType(string subtype)
    {
        return new VRage.Game.ModAPI.Ingame.MyItemType("MyObjectBuilder_Ingot", subtype);
    }
}
