using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block's inventory, the type this host provides as <c>ext.inventory</c>: items are moved out of it into other inventories.</summary>
    /// <remarks>Each member receives its inventory first, counted from 1, ahead of the step's own values.</remarks>
    public sealed partial class InventoryType : HostType
    {
        /// <summary>The action that moves items out of an inventory, which is Sprocket's own.</summary>
        public const string MoveItems = "move_items";

        /// <summary>What a step stuck with nothing it can move does: it waits until it can.</summary>
        public const long Hold = 0;

        /// <summary>What a step stuck with nothing it can move does: it carries on with what it moved.</summary>
        public const long MoveOn = 1;

        // A move has no state of the block to arrive at: its step is settled by what it has moved.
        static readonly Reading Arrives = Reading.Of<IMyTerminalBlock>(block => false);

        static readonly OwnAction Moves = OwnAction.Of<IMyTerminalBlock, Move>(Move.Read, Started, Arrives, Finished, Stuck);

        static readonly OwnValue Fill = OwnValue.Number<IMyTerminalBlock, Held>(Held.Read, (block, held) => block.GetInventory(held.At).VolumeFillFactor * 100d);

        static readonly OwnValue Mass = OwnValue.Number<IMyTerminalBlock, Held>(Held.Read, (block, held) => (double)block.GetInventory(held.At).CurrentMass);

        /// <inheritdoc />
        public override string Id => $"{BlockKinds.Own}.{Function}";

        /// <inheritdoc />
        public override string Function => "inventory";

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block.InventoryCount > 0;
        }

        /// <inheritdoc />
        public override void Offered(IMyTerminalBlock block, List<HostTypeFacet> into)
        {
            var production = block is IMyProductionBlock;
            for (var i = 0; i < block.InventoryCount; i++)
                into.Add(new HostTypeFacet(i + 1, production && i < 2 ? (i == 0 ? "input" : "output") : $"inventory {i + 1}"));
        }

        /// <inheritdoc />
        public override string Showing(IMyTerminalBlock block, int number)
        {
            if (number < 1 || number > block.InventoryCount)
                return "";

            return $"{block.GetInventory(number - 1).VolumeFillFactor * 100f:0}%";
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(MoveItems, "move", Moves, (string)null).Listed("move items").Joining("from").Lasting().ThroughConveyors()
                .Asking(To(), TheItem(block), HowMuch(), WhenStuck()).In(Category.Stock).Of(this);

            // What the block reads of an inventory it names, for a slot that already holds one.
            kind.WithValue("amount_held", "amount", Inventory.AmountInSlot(block)).In(Category.Stock).Announced().Of(this).InSlotsOnly()
                .WithValue("fill_level", "fill", Fill).Percent().In(Category.Stock).Announced().Of(this).InSlotsOnly()
                .WithValue("mass_held", "mass", Mass).Measured(" kg").In(Category.Stock).Announced().Of(this).InSlotsOnly();
        }

        /// <summary>The slots the items go to.</summary>
        Field To()
        {
            var field = Field.TakingSlots("to", Id);
            field.Lead = "to";
            return field;
        }

        /// <summary>The word the list of items opens on, for moving every item.</summary>
        public const string AllItems = "All items";

        /// <summary><paramref name="item" />, a field choosing an item, with every item as the first of its words, which is what it opens on and which a step holds as nothing given.</summary>
        /// <remarks>A list that could not be read has no words to go before, and stays a box.</remarks>
        public static Field OrAllItems(Field item)
        {
            if (item.Words == null || item.Words.Count == 0)
                return item;

            var words = new List<string> { AllItems };
            words.AddRange(item.Words);
            var means = new List<string> { "" };
            means.AddRange(item.Means ?? item.Words);
            item.Words = words;
            item.Means = means;
            item.Default = AllItems;
            return item;
        }

        /// <summary>The item to move; every item where it is left empty.</summary>
        static Field TheItem(IMyTerminalBlock block)
        {
            var item = ItemIds.Asked("items", block);
            item.SaidAhead = 2;
            item.SaidEmpty = "items";
            item.Open = true;
            item.MayBeLeftOut = true;
            item.Needed = false;
            item.Default = "";
            return OrAllItems(item);
        }

        /// <summary>How much of the item to move; all of it where it is left empty.</summary>
        static Field HowMuch()
        {
            var field = Field.Between("how much", "0", null);
            field.SaidAhead = 1;
            field.SaidEmpty = "all";
            field.UnitFollows = "items";
            field.UnitOf = Inventory.UnitOf;
            field.Entry = FieldInput.Decimal;
            field.MayBeLeftOut = true;
            field.Default = "";
            return field;
        }

        /// <summary>What a step does where it is stuck, hold or move on.</summary>
        static Field WhenStuck()
        {
            var field = Field.Among("when stuck", new[] { "move on", "hold" }, new[] { Literal.Of(MoveOn).Said, Literal.Of(Hold).Said });
            field.Entry = FieldInput.Whole;
            field.Default = "hold";
            field.Quiet = "hold";
            field.Lead = "or";
            field.MayBeLeftOut = true;
            return field;
        }

        /// <summary>The inventory its slot holds, first among what a member is given.</summary>
        sealed class Held
        {
            /// <summary>Where the inventory is among the block's, counted from 0.</summary>
            public int At;

            /// <summary>The inventory its slot holds, first among <paramref name="arguments" />.</summary>
            public static Held Read(IList<Literal> arguments)
            {
                var given = arguments != null && arguments.Count > 0 ? (int)arguments[0].Number : 1;
                return new Held { At = given < 1 ? 0 : given - 1 };
            }
        }

        static void Started(IMyTerminalBlock block, Move move, ReaderMemory seen)
        {
            seen.Number = 0;
            move.Moved(block, seen);
        }

        /// <summary>Moves what can be moved and answers whether the step is settled.</summary>
        /// <remarks>Asked on the hold's pace while the step waits: this is where a step that holds picks up as room or stock turns up.</remarks>
        static bool Finished(IMyTerminalBlock block, Move move, ReaderMemory seen)
        {
            move.Moved(block, seen);
            return move.Reached(block, seen) || move.MovesOn;
        }

        static BlockTrouble Stuck(IMyTerminalBlock block, Move move, ReaderMemory seen)
        {
            return move.MovesOn || move.Reached(block, seen) ? BlockTrouble.None : BlockTrouble.Waiting(move.Why(block));
        }
    }
}
