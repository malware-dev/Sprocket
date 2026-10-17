using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A gas tank, oxygen or hydrogen, which can stockpile and be waited on until it is full.
    /// </summary>
    /// <remarks>
    /// The game lands a filling tank exactly on full and a draining one exactly on empty, so both are comparisons and need
    /// no band.
    /// <para>
    /// Nothing to call off: a tank left stockpiling holds nobody in danger, and switching it off is a step of its own.
    /// </para>
    /// </remarks>
    public sealed class Tank : Capability
    {
        /// <summary>Stockpiling, which is Sprocket's own and waits until the tank is full unless told not to.</summary>
        public const string Stockpile = "stockpile";

        const string Flowing = "A tank announces its fill every tick its gas moves, which saves no look while it fills or drains.";

        static readonly Reading Full = Reading.Of<IMyGasTank>(tank => tank.FilledRatio >= 1.0);

        static readonly OwnAction Stockpiles = OwnAction.Of<IMyGasTank, Waiting>(Waiting.Read,
            (tank, waiting, seen) => tank.Stockpile = true, Full,
            (tank, waiting, seen) => !waiting.Waits,
            (tank, waiting, seen) => waiting.Waits ? Why(tank) : BlockTrouble.None);

        // The game reads the fill as a fraction; every fill Sprocket says is in percent.
        static readonly OwnValue Level = OwnValue.Number<IMyGasTank>(tank => tank.FilledRatio * 100.0);

        static readonly OwnValue Capacity = OwnValue.Number<IMyGasTank>(tank => tank.Capacity);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyGasTank;
        }

        /// <inheritdoc />
        /// <remarks>The level is named for the gas the tank holds, which its definition says.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var gas = Gas(block);
            var level = $"{gas}-level";
            kind.WithAction(Stockpile, "stockpile", Stockpiles, "Stockpile_On").Lasting().In(Category.Gas).Asking(Asked())
                .Renaming("Stockpile", true, "stockpiling").Stating("Stockpile")
                .Renaming("Stockpile", false, "toggle stockpiling")
                .WithValue(level, $"{gas} level", Level).Percent().Looked(Flowing).In(Category.Gas)
                .WithChange("fills-up", "fills up", level, Literal.Of(100.0)).Reading("is full", "is not full").Looked(Flowing)
                .WithChange("runs-dry", "runs dry", level, Literal.Of(0.0)).Reading("is empty", "is not empty").Announced()
                .WithValue("capacity", "capacity", Capacity).Measured(" L").Looked("A tank's capacity is its kind's, and never changes.")
                .In(Category.Gas)
                .Renaming("Refill", false, "refill bottles")
                .Renaming("Auto-Refill", true, "refill bottles automatically")
                .Renaming("Auto-Refill", false, "toggle refill bottles automatically");
        }

        /// <summary>Told as the tank runs out of gas and as it gets some back, which its resource source raises.</summary>
        /// <remarks>
        /// Only that: the tank's own PropertiesChanged comes every tick its gas moves, and hearing it would look at a busy tank
        /// on every tick.
        /// </remarks>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return ResourceListener.Heard(block, told);
        }

        /// <inheritdoc />
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var tank = block as IMyGasTank;
            if (tank == null)
                return Status.None;

            if (tank.FilledRatio >= 1.0)
                return Status.Of(Condition.Full);

            if (tank.Stockpile)
            {
                BlockTrouble stuck = Filling(tank);
                if (stuck.Exists)
                    return Status.Of(stuck);
            }

            if (In(tank) > 0f)
                return Status.Of(Condition.Filling);

            return tank.FilledRatio <= 0.0 ? Status.Of(Condition.Empty) : Status.None;
        }

        /// <summary>Busy while it fills on a stockpile, which is the one filling a step asks for.</summary>
        /// <remarks>
        /// A tank that is not stockpiling fills whenever there is gas to spare, so counting that would
        /// light every tank on a ship with an oxygen generator.
        /// </remarks>
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var tank = block as IMyGasTank;
            return tank != null && tank.Stockpile && tank.FilledRatio < 1.0 && In(tank) > 0f;
        }

        /// <summary>What a step's stockpile is asked: whether it waits until the tank is full.</summary>
        /// <remarks>Waiting is what the action does anyway, so a sentence leaves it out, and says "without waiting" where it does not wait.</remarks>
        static Field Asked()
        {
            var field = Field.Among("wait until full", new[] { "until full", "without waiting" },
                new[] { Literal.Of(true).Said, Literal.Of(false).Said });
            field.Default = "until full";
            field.Quiet = "until full";
            field.About = "**Until full** waits until the tank is full. Where no gas comes in, that wait never ends.\n\n"
                + "**Without waiting** switches stockpile on and carries on at once.";
            return field;
        }

        /// <summary>The gas the tank holds, in lower case: its definition's, or, where none is loaded, the one its subtype names.</summary>
        static string Gas(IMyTerminalBlock block)
        {
            var definition = Definition(block);
            if (definition != null)
                return definition.StoredGasId.SubtypeName.ToLowerInvariant();

            var subtype = block.BlockDefinition.SubtypeName;
            return subtype != null && subtype.IndexOf("Hydrogen", StringComparison.Ordinal) >= 0 ? "hydrogen" : "oxygen";
        }

        /// <summary>
        /// Why this tank will not fill, or null while it might.
        /// </summary>
        /// <remarks>
        /// Stockpile switched off is its own answer: whatever switched it has overruled the step, and
        /// a tank that is not stockpiling hands its gas on as fast as it takes it in.
        /// </remarks>
        static BlockTrouble Why(IMyGasTank tank)
        {
            if (tank.FilledRatio >= 1.0)
                return BlockTrouble.None;

            if (!tank.Stockpile)
                return BlockTrouble.Fatal(Cause.TankStockpileOff);

            return Filling(tank);
        }

        /// <summary>Why a stockpiling tank is not filling, or null while it is or nothing can say.</summary>
        static BlockTrouble Filling(IMyGasTank tank)
        {
            if (!tank.IsWorking)
                return BlockTrouble.Waiting(Cause.TankNoPower);

            MyGasTankDefinition definition = Definition(tank);
            if (definition == null || In(tank, definition.StoredGasId) != 0f)
                return BlockTrouble.None;

            return BlockTrouble.Waiting(Cause.TankNoGas, definition.StoredGasId.SubtypeName.ToLowerInvariant());
        }

        /// <summary>The gas coming in, or zero where nothing says.</summary>
        static float In(IMyGasTank tank)
        {
            MyGasTankDefinition definition = Definition(tank);
            if (definition == null)
                return 0f;

            float flow = In(tank, definition.StoredGasId);
            return flow < 0f ? 0f : flow;
        }

        /// <summary>
        /// What the tank is being given of the gas it holds, or negative where it has no components to ask.
        /// </summary>
        /// <remarks>
        /// What the resource distributor granted rather than what the tank asked for, so a construct
        /// with nothing to give reads zero.
        /// </remarks>
        static float In(IMyGasTank tank, MyDefinitionId gas)
        {
            var sink = tank.Components.Get<MyResourceSinkComponent>();
            return sink?.CurrentInputByType(gas) ?? -1f;
        }

        /// <summary>What the game says this tank is, or null where nothing has been loaded to say.</summary>
        static MyGasTankDefinition Definition(IMyTerminalBlock tank)
        {
            var definitions = MyDefinitionManager.Static;
            return definitions?.GetCubeBlockDefinition(tank.BlockDefinition) as MyGasTankDefinition;
        }

        /// <summary>What a step told a tank to stockpile with.</summary>
        sealed class Waiting
        {
            /// <summary>Whether the step waits until the tank is full.</summary>
            public bool Waits;

            /// <summary>What a step's options come to: waiting unless it says otherwise.</summary>
            public static Waiting Read(IList<Literal> arguments)
            {
                return new Waiting
                {
                    Waits = arguments == null || arguments.Count == 0 || arguments[0].Kind != ValueKind.Flag || arguments[0].Flag
                };
            }
        }
    }
}
