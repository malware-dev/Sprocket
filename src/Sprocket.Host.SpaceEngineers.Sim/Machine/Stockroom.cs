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
    readonly List<Dictionary<string, double>> _held = new List<Dictionary<string, double>>();
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
            _inventories.Add(Inventory(held));
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

    /// <summary>Lets its inventories take the item <paramref name="item" />.</summary>
    public void Accepts(VRage.Game.MyDefinitionId item)
    {
        _accepted.Add(item);
    }

    IMyInventory Inventory(Dictionary<string, double> held)
    {
        return Pretend.To<IMyInventory>()
            .Gives("VolumeFillFactor", () => 0f)
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
}
