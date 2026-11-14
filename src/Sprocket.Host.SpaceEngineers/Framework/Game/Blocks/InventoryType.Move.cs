using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class InventoryType
    {
        /// <summary>What a step told an inventory to move and where, read once when it is compiled.</summary>
        sealed class Move
        {
            static readonly IList<HeldThing> Nowhere = new HeldThing[0];

            /// <summary>The number of the inventory the items leave, from 1.</summary>
            public int Facet;

            /// <summary>Where the items go, filled in order.</summary>
            public IList<HeldThing> To;

            /// <summary>The item, or null for every item.</summary>
            public Item Item;

            /// <summary>How much to move, or a negative number for all there is.</summary>
            public double Amount;

            /// <summary>Whether the step carries on where nothing more can move, rather than holding.</summary>
            public bool MovesOn;

            /// <summary>What a step's options come to, the inventory's number first.</summary>
            public static Move Read(IList<Literal> arguments)
            {
                var move = new Move { Facet = arguments.Count > 0 ? (int)arguments[0].Number : 1, To = Nowhere, Amount = -1 };
                if (arguments.Count > 1 && arguments[1].Held != null)
                    move.To = arguments[1].Held;

                if (arguments.Count > 2 && !string.IsNullOrEmpty(arguments[2].Text))
                    move.Item = Item.At(arguments, 2);

                if (arguments.Count > 3 && (arguments[3].Kind == ValueKind.Integer || arguments[3].Kind == ValueKind.Real))
                    move.Amount = arguments[3].Number;

                // Only ore and ingots come in fractions, so a count of anything else is a whole number.
                if (move.Amount >= 0 && (move.Item == null || !ItemIds.IsWeighed(arguments[2].Text)))
                    move.Amount = System.Math.Floor(move.Amount);

                move.MovesOn = arguments.Count > 4 && arguments[4].Kind == ValueKind.Integer && arguments[4].Whole == MoveOn;
                return move;
            }

            IMyInventory Source(IMyTerminalBlock block)
            {
                return Facet >= 1 && Facet <= block.InventoryCount ? block.GetInventory(Facet - 1) : null;
            }

            static IMyInventory Inventory(HeldThing thing)
            {
                var number = thing.Facet < 1 ? 1 : thing.Facet;
                return thing.Block != null && !thing.Block.Closed && number <= thing.Block.InventoryCount ? thing.Block.GetInventory(number - 1) : null;
            }

            /// <summary>Whether <paramref name="item" /> is one this moves.</summary>
            bool Takes(VRage.Game.ModAPI.Ingame.MyInventoryItem item)
            {
                return Item == null || Item.Known && item.Type == (VRage.Game.ModAPI.Ingame.MyItemType)Item.Id;
            }

            /// <summary>Moves what can move now, what fits and what the conveyors carry, and keeps in <paramref name="seen" /> how much has moved.</summary>
            /// <remarks>The game's transfer takes what fits, so the amount moved is read off the inventory the items leave. Runs on the server, where the game moves items at once.</remarks>
            public void Moved(IMyTerminalBlock block, ReaderMemory seen)
            {
                var source = Source(block);
                if (source == null)
                    return;

                for (var t = 0; t < To.Count; t++)
                {
                    var target = Inventory(To[t]);
                    if (target == null || ReferenceEquals(target, source))
                        continue;

                    for (var i = source.ItemCount - 1; i >= 0; i--)
                    {
                        var left = Amount < 0 ? double.MaxValue : Amount - seen.Number;
                        if (left <= 0)
                            return;

                        var held = source.GetItemAt(i);
                        if (!held.HasValue || !Takes(held.Value))
                            continue;

                        var before = (double)source.GetItemAmount(held.Value.Type);
                        var amount = left >= (double)held.Value.Amount ? held.Value.Amount : (MyFixedPoint)left;
                        if (amount <= 0)
                            continue;

                        // The conveyor check is spelled out: moving without a connection is a choice this never makes.
                        // ReSharper disable once RedundantArgumentDefaultValue
                        source.TransferItemTo(target, i, null, true, amount, true);
                        seen.Number += before - (double)source.GetItemAmount(held.Value.Type);
                    }
                }
            }

            /// <summary>Whether the step has moved all it was told to: the amount, or everything there was.</summary>
            public bool Reached(IMyTerminalBlock block, ReaderMemory seen)
            {
                return Amount >= 0 ? seen.Number >= Amount : !Holds(Source(block));
            }

            bool Holds(IMyInventory source)
            {
                for (var i = 0; source != null && i < source.ItemCount; i++)
                {
                    var held = source.GetItemAt(i);
                    if (held.HasValue && Takes(held.Value))
                        return true;
                }

                return false;
            }

            /// <summary>Whether <paramref name="target" /> takes items of <paramref name="type" /> at all, however full it is.</summary>
            /// <remarks>Lists what the game accepts there, so it is asked only of a step already stuck, on the hold's pace.</remarks>
            static bool Accepted(IMyInventory target, VRage.Game.ModAPI.Ingame.MyItemType type)
            {
                var accepted = new List<VRage.Game.ModAPI.Ingame.MyItemType>();
                target.GetAcceptedItems(accepted, item => item == type);
                return accepted.Count > 0;
            }

            /// <summary>Why nothing more moves: there is none to move, nothing connected would carry it, the inventory there refuses it, or it has nowhere to fit.</summary>
            public Cause Why(IMyTerminalBlock block)
            {
                var source = Source(block);
                if (!Holds(source))
                    return Cause.ItemsNoStock;

                var connected = false;
                var elsewhere = false;
                for (var t = 0; t < To.Count; t++)
                {
                    var target = Inventory(To[t]);
                    if (target == null || ReferenceEquals(target, source))
                        continue;

                    elsewhere = true;
                    for (var i = 0; i < source.ItemCount; i++)
                    {
                        var held = source.GetItemAt(i);
                        if (!held.HasValue || !Takes(held.Value) || !source.CanTransferItemTo(target, held.Value.Type))
                            continue;

                        connected = true;
                        if (Accepted(target, held.Value.Type))
                            return Cause.ItemsNoRoom;
                    }
                }

                if (!elsewhere)
                    return Cause.ItemsAlreadyThere;

                return connected ? Cause.ItemsNotAccepted : Cause.ItemsNotConnected;
            }
        }
    }
}
