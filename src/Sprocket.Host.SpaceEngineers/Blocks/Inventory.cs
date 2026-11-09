using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with an inventory, read for how full it is, how heavy its load and how much of an item it holds.</summary>
    /// <remarks>How full and how heavy are read only on a block with one inventory. How much of an item is read on any.</remarks>
    public sealed partial class Inventory : Capability
    {
        static readonly OwnValue Fill = OwnValue.Number<IMyTerminalBlock>(block => block.GetInventory(0).VolumeFillFactor * 100d);

        static readonly OwnValue Mass = OwnValue.Number<IMyTerminalBlock>(block => (double)block.GetInventory(0).CurrentMass);

        /// <summary>How much of an item one inventory of a block holds: in kilograms for ore and ingots, and in pieces for the rest.</summary>
        static OwnValue Amount(IMyTerminalBlock block)
        {
            return OwnValue.Number<IMyTerminalBlock, Asked>(Asked.Of, Held).AnsweredBy<IMyTerminalBlock>(Has)
                .Taking(WhichInventory(block), TheItem(block)).Joined("in", "of").MeasuredBy("item", UnitOf);
        }

        /// <summary>How much of an item the inventory a slot holds has, the inventory coming from the slot and only the item asked.</summary>
        public static OwnValue AmountInSlot(IMyTerminalBlock block)
        {
            return OwnValue.Number<IMyTerminalBlock, Asked>(Asked.Of, Held).AnsweredBy<IMyTerminalBlock>(Has)
                .Taking(TheItem(block)).Joined("in", "of").MeasuredBy("item", UnitOf);
        }

        /// <summary>How much of what is <paramref name="asked" /> the block holds, 0 of an item the game hasn't or an inventory it lacks.</summary>
        static double Held(IMyTerminalBlock block, Asked asked)
        {
            if (!asked.Item.Known || asked.Number < 1 || asked.Number > block.InventoryCount)
                return 0d;

            return (double)block.GetInventory(asked.Number - 1).GetItemAmount(asked.Item.Id);
        }

        static bool Has(IMyTerminalBlock block, IList<Literal> arguments)
        {
            var number = Asked.Of(arguments).Number;
            return number >= 1 && number <= block.InventoryCount;
        }

        /// <summary>The item, which a sentence says after "of": "amount of Iron Ingot".</summary>
        static Field TheItem(IMyTerminalBlock block)
        {
            var item = ItemIds.Asked("item", block);

            // An item off the list is still taken: blocks picked together list what the first of them holds.
            item.Open = true;
            item.Lead = "of";
            return item;
        }

        /// <summary>The unit an amount of the item <paramref name="said" /> is counted in, for a count of one where <paramref name="one" /> says so.</summary>
        public static string UnitOf(string said, bool one)
        {
            if (string.IsNullOrEmpty(said))
                return "";

            return ItemIds.IsWeighed(said) ? " kg" : one ? " pc" : " pcs";
        }

        /// <summary>Which inventory is read, a part of the block: never asked of a block with one, by name where the game names them, and by number on any other.</summary>
        static Field WhichInventory(IMyTerminalBlock block)
        {
            // Any number from 1 is taken: a block without that inventory is passed over rather than refused.
            var field = block is IMyProductionBlock
                ? Field.Among("inventory", new[] { "input", "output" }, new[] { "1", "2" })
                : Field.Between("inventory", "1", null);
            field.Least = "1";
            field.Open = true;
            field.Entry = FieldInput.Whole;
            field.Needed = true;
            field.IsPart = true;
            field.NumberedLead = "in inventory";
            field.Lead = block is IMyProductionBlock ? "in the" : field.NumberedLead;
            if (block.InventoryCount == 1)
            {
                field.Fixed = true;
                field.Default = "1";
                field.Quiet = "1";
            }

            return field;
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block.InventoryCount > 0;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("inventory_amount", "amount", Amount(block)).In(Category.Stock).Announced();
            if (block.InventoryCount != 1)
                return;

            kind.WithValue("inventory_fill", "inventory fill", Fill).Percent().In(Category.Stock).Announced()
                .WithValue("inventory_mass", "inventory mass", Mass).Measured(" kg").In(Category.Stock).Announced();
        }

        /// <summary>Told as anything goes into or out of the inventory.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return block.InventoryCount > 0 ? new Listening(block, told) : null;
        }
    }
}
