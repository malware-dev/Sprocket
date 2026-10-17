using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;
using VRage.Game.ModAPI;

// These live in the Ingame namespace, which cannot be imported whole: IMyAssembler and friends are declared in both, and
// the mod-side ones are the ones with the events on them.
using MyAssemblerMode = Sandbox.ModAPI.Ingame.MyAssemblerMode;
using MyProductionItem = Sandbox.ModAPI.Ingame.MyProductionItem;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An assembler or a refinery: what it makes, whether it is making it, and what its two inventories hold.</summary>
    public sealed class Production : Capability
    {
        const string Kit = "MyObjectBuilder_SurvivalKit";

        const string Queue = "A queue changed in the production screen raises nothing.";

        const string Repeat = "Repeat is set in the production screen, which raises nothing.";

        static readonly string[] Modes = { "assembling", "taking-apart" };

        static readonly Reading Making = Reading.Of<IMyProductionBlock>(block => block.IsProducing);

        static readonly OwnValue Producing = OwnValue.Flag(Making);

        // Work waiting and nothing made, on a block that could make it: short of materials, or with nowhere to put what it
        // makes. The game's own reason is on a type mods cannot reach.
        static readonly OwnValue Stuck = OwnValue.Flag(Reading.Of<IMyProductionBlock>(block =>
            !block.IsQueueEmpty && !block.IsProducing && block.Enabled && block.IsWorking));

        static readonly OwnValue Progress = OwnValue.Number<IMyAssembler>(assembler => assembler.CurrentProgress * 100.0);

        static readonly OwnValue Mode = OwnValue.OneOf<IMyAssembler>(Modes, Position)
            .Writing<IMyAssembler>((assembler, mode) =>
                assembler.Mode = mode.Whole == 1 ? MyAssemblerMode.Disassembly : MyAssemblerMode.Assembly);

        // The mode of an assembler whose definition keeps it from taking apart, which its production screen offers no
        // choice of.
        static readonly OwnValue Locked = OwnValue.OneOf<IMyAssembler>(Modes, Position);

        static readonly OwnValue Repeating = OwnValue.Flag(Reading.Of<IMyAssembler>(assembler => assembler.Repeating))
            .Writing<IMyAssembler>((assembler, repeating) => assembler.Repeating = repeating.Flag);

        static readonly OwnValue InputFill = OwnValue.Number<IMyProductionBlock>(block => block.InputInventory.VolumeFillFactor * 100.0);

        static readonly OwnValue OutputFill = OwnValue.Number<IMyProductionBlock>(block => block.OutputInventory.VolumeFillFactor * 100.0);

        static readonly OwnAction Clears = OwnAction.Of<IMyProductionBlock>(block => block.ClearQueue());

        // Filled and emptied by one reading at a time, on the game's main thread, so one list serves every assembler.
        readonly List<MyProductionItem> _queue = new List<MyProductionItem>();

        readonly OwnValue _queued;

        /// <summary>Production, with the list the queue is read into.</summary>
        public Production()
        {
            _queued = OwnValue.Whole<IMyAssembler>(Queued);
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyProductionBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var assembler = block as IMyAssembler;
            if (assembler != null)
                kind.Renaming("slaveMode", true, "cooperative mode").Renaming("slaveMode", false, "toggle cooperative mode");

            if (IsKit(block))
                return;

            if (assembler != null)
                Orders(kind, assembler);

            var doing = assembler == null ? "refining" : "producing";
            kind.WithValue(doing, doing, Producing).Announced().In(Category.Production)
                .WithChange($"starts-{doing}", $"starts {doing}", doing, Literal.Of(true))
                .Reading($"is {doing}", $"is not {doing}").Announced()
                .WithChange($"stops-{doing}", $"stops {doing}", doing, Literal.Of(false))
                .Reading($"is not {doing}", $"is {doing}").Announced()
                .WithValue("stuck", "stuck", Stuck).Announced().In(Category.Production);

            if (assembler != null)
                Setup(kind, block);

            kind.WithValue("input-fill", "input fill", InputFill).Percent().Announced().In(Category.Stock)
                .WithValue("output-fill", "output fill", OutputFill).Percent().Announced().In(Category.Stock);

            // The amount of one item in either inventory waits for after the beta: no form asks which item, so neither could be
            // filled in. Kept for the form that asks.
            // kind.WithValue("amount-in-input", "amount in input", Holding(true)).Announced().In(Category.Stock)
            //     .WithValue("amount-in-output", "amount in output", Holding(false)).Announced().In(Category.Stock);
        }

        /// <summary>Told as the block starts or stops producing, gets stuck or works again, or its inventories change; on an assembler also as its progress, state or mode changes.</summary>
        /// <remarks>The inventories announce their changes on the server only.</remarks>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var production = block as IMyProductionBlock;
            return production == null || IsKit(block) ? null : new Listening(production, told);
        }

        /// <inheritdoc />
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var production = block as IMyProductionBlock;
            return production != null && !IsKit(block) && production.IsProducing;
        }

        /// <summary>What an assembler is ordered to make, and how far it has got.</summary>
        void Orders(EntityKind kind, IMyAssembler assembler)
        {
            var words = new List<string>();
            var means = new List<string>();
            var blueprints = new Dictionary<string, MyDefinitionId>();
            Blueprints(assembler, words, means, blueprints);

            var amount = Field.Between("amount", "1", "");
            amount.Entry = FieldInput.Whole;
            amount.Needed = true;
            amount.Default = "1";

            var makes = OwnAction.Of<IMyAssembler, Order>(given => Order.Read(given, blueprints),
                (made, order, seen) =>
                {
                    if (order.Known)
                        made.AddQueueItem(order.Blueprint, (double)order.Amount);
                });

            kind.WithAction("make", "make", makes, ItemIds.Choosing("item", words, means), amount).In(Category.Production)
                .WithAction("clear-the-queue", "clear the queue", Clears).In(Category.Production)
                .WithValue("items-queued", "items queued", _queued).Looked(Queue).In(Category.Production)
                .WithValue("progress", "progress", Progress).Percent().Announced().In(Category.Production);
        }

        /// <summary>How an assembler is set to work: making or taking apart, and over again.</summary>
        static void Setup(EntityKind kind, IMyTerminalBlock block)
        {
            // Whether an assembler can take apart is fixed by its definition, so it never changes and there is nothing to announce.
            var able = TakesApart(block);
            kind.WithValue("mode", "mode", able ? Mode : Locked).Announced().In(Category.Production)
                .WithValue("able-to-take-apart", "able to take apart", OwnValue.Flag(Reading.Of<IMyAssembler>(assembler => able)))
                .Announced().In(Category.Production)
                .WithValue("repeating", "repeating", Repeating).Looked(Repeat).In(Category.Production);
        }

        /// <summary>
        /// Adds to <paramref name="words" /> and <paramref name="means" /> each item <paramref name="assembler" /> can make,
        /// and to <paramref name="blueprints" /> the blueprint that makes it.
        /// </summary>
        static void Blueprints(IMyAssembler assembler, IList<string> words, IList<string> means,
            IDictionary<string, MyDefinitionId> blueprints)
        {
            var definitions = MyDefinitionManager.Static;
            if (definitions == null)
                return;

            foreach (var blueprint in definitions.GetBlueprintDefinitions())
            {
                if (!blueprint.Public || blueprint.Results == null || blueprint.Results.Length != 1
                    || blueprint.Results[0].Amount != 1 || !assembler.CanUseBlueprint(blueprint.Id))
                    continue;

                var said = ItemIds.Said(blueprint.Results[0].Id);
                if (blueprints.ContainsKey(said))
                    continue;

                blueprints.Add(said, blueprint.Id);
                words.Add(blueprint.DisplayNameText);
                means.Add(said);
            }
        }

        /// <summary>Whether an assembler of <paramref name="block" />'s definition can be set to take apart, or true where no definitions are loaded, as the game's own default.</summary>
        static bool TakesApart(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            var definition = definitions?.GetCubeBlockDefinition(block.BlockDefinition) as MyAssemblerDefinition;

            return definition == null || definition.EnableDisassembly;
        }

        /// <summary>Where an assembler's mode stands among the modes: making, or taking apart.</summary>
        static int Position(IMyAssembler assembler)
        {
            return assembler.Mode == MyAssemblerMode.Disassembly ? 1 : 0;
        }

        // Read by the amounts above, which wait for after the beta.
        // /// <summary>How much of an item one of the block's two inventories holds: in kilograms for ore and ingots, and in pieces for anything else.</summary>
        // static Being Holding(bool input)
        // {
        //     return OwnValue.Number<IMyProductionBlock, Item>(Item.First, (block, item, seen) =>
        //             item.Known ? (double)(input ? block.InputInventory : block.OutputInventory).GetItemAmount(item.Id) : 0.0)
        //         .Taking(ItemIds.Asked("item"));
        // }

        /// <summary>How many items an assembler has queued, all orders together.</summary>
        long Queued(IMyAssembler assembler)
        {
            _queue.Clear();
            assembler.GetQueue(_queue);

            long count = 0;
            for (var i = 0; i < _queue.Count; i++)
                count += _queue[i].Amount.ToIntSafe();

            _queue.Clear();
            return count;
        }

        /// <summary>Whether <paramref name="block" /> is a survival kit, which is an assembler to the game and a life support block to Sprocket.</summary>
        static bool IsKit(IMyTerminalBlock block)
        {
            return block.BlockDefinition.TypeIdString == Kit;
        }

        /// <summary>What a step ordering an assembler to make something was given.</summary>
        sealed class Order
        {
            /// <summary>The blueprint that makes the item, where it is one the assembler can make.</summary>
            public MyDefinitionId Blueprint;

            /// <summary>How many.</summary>
            public long Amount;

            /// <summary>Whether the item is one the assembler can make.</summary>
            public bool Known;

            /// <summary>The order a step's arguments make, against the blueprints the assembler can use.</summary>
            public static Order Read(IList<Literal> arguments, IDictionary<string, MyDefinitionId> blueprints)
            {
                var order = new Order();
                if (arguments == null || arguments.Count < 2)
                    return order;

                order.Known = blueprints.TryGetValue(arguments[0].Text ?? "", out order.Blueprint);
                order.Amount = arguments[1].Whole;
                return order;
            }
        }

        /// <summary>A production block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyProductionBlock _block;
            readonly IMyAssembler _assembler;
            readonly IMyInventory _input;
            readonly IMyInventory _output;
            readonly Action _told;

            public Listening(IMyProductionBlock block, Action told)
            {
                _block = block;
                _assembler = block as IMyAssembler;
                _input = block.InputInventory;
                _output = block.OutputInventory;
                _told = told;

                _block.StartedProducing += Changed;
                _block.StoppedProducing += Changed;
                _block.IsWorkingChanged += Worked;
                if (_input != null)
                    _input.OnVolumeChanged += Filled;
                if (_output != null)
                    _output.OnVolumeChanged += Filled;

                if (_assembler == null)
                    return;

                _assembler.CurrentProgressChanged += Assembled;
                _assembler.CurrentStateChanged += Assembled;
                _assembler.CurrentModeChanged += Assembled;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.StartedProducing -= Changed;
                _block.StoppedProducing -= Changed;
                _block.IsWorkingChanged -= Worked;
                if (_input != null)
                    _input.OnVolumeChanged -= Filled;
                if (_output != null)
                    _output.OnVolumeChanged -= Filled;

                if (_assembler == null)
                    return;

                _assembler.CurrentProgressChanged -= Assembled;
                _assembler.CurrentStateChanged -= Assembled;
                _assembler.CurrentModeChanged -= Assembled;
            }

            void Changed()
            {
                _told();
            }

            void Worked(IMyCubeBlock block)
            {
                _told();
            }

            void Filled(IMyInventory inventory, float before, float after)
            {
                _told();
            }

            void Assembled(IMyAssembler assembler)
            {
                _told();
            }
        }
    }
}
