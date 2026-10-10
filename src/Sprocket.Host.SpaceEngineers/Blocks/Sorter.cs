using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

// These live in the Ingame namespace, which cannot be imported whole: IMyConveyorSorter is declared in both.
using MyInventoryItemFilter = Sandbox.ModAPI.Ingame.MyInventoryItemFilter;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A conveyor sorter: the list of items it lets through or holds back, and whether it pulls them.</summary>
    /// <remarks>
    /// Its list is changed as a player changes it at the terminal, item by item or a whole type at once, and the game
    /// passes the change on to every player. Nothing raises an event as the list changes, so what it lets through is
    /// looked at.
    /// </remarks>
    public sealed class Sorter : Capability
    {
        const string Unheard = "A sorter raises nothing as its list changes.";

        const string GamePrefix = "MyObjectBuilder_";

        // The kinds of item the sorter's own terminal offers as a whole, with the game's own words for each. Transcribed
        // from the game's conveyor sorter.
        static readonly string[] Types =
        {
            "AmmoMagazine", "Component", "ConsumableItem", "PhysicalGunObject", "Ingot", "Ore", "SeedItem"
        };

        static readonly string[] TypeTexts =
        {
            "DisplayName_ConvSorterTypes_Ammo", "DisplayName_ConvSorterTypes_Component",
            "DisplayName_ConvSorterTypes_Consumables", "DisplayName_ConvSorterTypes_HandTool",
            "DisplayName_ConvSorterTypes_Ingot", "DisplayName_ConvSorterTypes_Ore", "DisplayName_ConvSorterTypes_Seeds"
        };

        static readonly Doing Adds = Doing.Of<IMyConveyorSorter, Filter>(Filter.Read, (sorter, filter, seen) =>
        {
            if (filter.Known)
                sorter.AddItem(filter.Item);
        });

        static readonly Doing Removes = Doing.Of<IMyConveyorSorter, Filter>(Filter.Read, (sorter, filter, seen) =>
        {
            if (filter.Known)
                sorter.RemoveItem(filter.Item);
        });

        // Handed to the game to empty the list, and never filled.
        readonly List<MyInventoryItemFilter> _nothing = new List<MyInventoryItemFilter>();

        readonly Doing _clears;

        /// <summary>A sorter, with the empty list its list is cleared to.</summary>
        public Sorter()
        {
            _clears = Doing.Of<IMyConveyorSorter>(sorter => sorter.SetFilter(sorter.Mode, _nothing));
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyConveyorSorter;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var listed = Listed();
            var lets = Being.Flag<IMyConveyorSorter, Item>(Item.First,
                (sorter, item, seen) => item.Known && sorter.IsAllowed(item.Id)).Taking(Items.Asked("item"));

            kind.Renaming("blacklistWhitelist", true, "filter mode")
                .Renaming("DrainAll", true, "pulling")
                .Renaming("DrainAll", false, "toggle pulling")
                .WithAction("add-to-the-list", "add to the list", Adds, listed).In(Category.Stock)
                .WithAction("remove-from-the-list", "remove from the list", Removes, listed).In(Category.Stock)
                .WithAction("clear-the-list", "clear the list", _clears).In(Category.Stock)
                .WithValue("lets-through", "lets through", lets).Looked(Unheard).In(Category.Stock);
        }

        /// <summary>
        /// What can go on the list: every kind of item the terminal offers as a whole, then every item the game has.
        /// </summary>
        /// <remarks>Where no definitions are loaded - the test harness - a box that takes any item or kind written out.</remarks>
        static Field Listed()
        {
            var words = new List<string>();
            var means = new List<string>();

            if (MyDefinitionManager.Static != null)
            {
                for (var i = 0; i < Types.Length; i++)
                {
                    words.Add(MyTexts.GetString(MyStringId.GetOrCompute(TypeTexts[i])));
                    means.Add(Types[i]);
                }

                foreach (var item in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
                {
                    if (!item.Public)
                        continue;

                    words.Add(item.DisplayNameText);
                    means.Add(Items.Said(item.Id));
                }
            }

            return Items.Choosing("item", words, means);
        }

        /// <summary>What a step changing the list was given: one item, or every item of one kind.</summary>
        sealed class Filter
        {
            /// <summary>The entry on the list, where <see cref="Known" />.</summary>
            public MyInventoryItemFilter Item;

            /// <summary>Whether it names an item, or a kind of item, the game has.</summary>
            public bool Known;

            /// <summary>The entry a step's argument names: <c>Ore</c> for every ore, <c>Ore/Iron</c> for iron ore alone.</summary>
            public static Filter Read(IList<Literal> arguments)
            {
                var filter = new Filter();
                if (arguments == null || arguments.Count == 0 || string.IsNullOrEmpty(arguments[0].Text))
                    return filter;

                var said = arguments[0].Text;
                MyDefinitionId id;
                if (said.IndexOf('/') >= 0)
                {
                    filter.Known = Items.Read(said, out id);
                    if (filter.Known)
                        filter.Item = new MyInventoryItemFilter(id);

                    return filter;
                }

                MyObjectBuilderType type;
                filter.Known = MyObjectBuilderType.TryParse(GamePrefix + said, out type);
                if (filter.Known)
                    filter.Item = new MyInventoryItemFilter(new MyDefinitionId(type), true);

                return filter;
            }
        }
    }
}
