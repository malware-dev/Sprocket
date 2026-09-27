using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A gas tank, oxygen or hydrogen, which can stockpile and be waited on until it is full.
    /// </summary>
    /// <remarks>
    /// Its fill is sampled: a tank has no events. The game lands a filling tank exactly on full, so
    /// full is a comparison and needs no band.
    /// </remarks>
    sealed class TankExtension : IBlockExtension
    {
        /// <summary>Stockpiling until full, which is Sprocket's own and waits until the tank is full.</summary>
        public const string Stockpile = "stockpile";

        const string Full = "full";
        const string IsFull = "is-full";

        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyGasTank;
        }

        /// <summary>Nothing to arrange: this block's events fire when they fire.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Full, "full")
                .WithState(IsFull, Full, true, "is full")
                .WithAction(Stockpile, "stockpile", IsFull, "Stockpile_On").Lasting().In(Category.Air);
        }

        /// <inheritdoc />
        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var tank = block as IMyGasTank;
            if (tank == null || propertyId != Full)
                return false;

            value = EntityValue.Of(tank.FilledRatio >= 1.0);
            return true;
        }

        /// <inheritdoc />
        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var tank = block as IMyGasTank;
            if (tank == null || actionId != Stockpile)
                return false;

            tank.Stockpile = true;
            return true;
        }

        /// <inheritdoc />
        public string StatusOf(IMyTerminalBlock block)
        {
            var tank = block as IMyGasTank;
            if (tank == null)
                return null;

            if (tank.FilledRatio >= 1.0)
                return "full";

            if (tank.Stockpile)
            {
                Trouble stuck = Filling(tank);
                if (stuck.Exists)
                    return stuck.Said;
            }

            if (In(tank) > 0f)
                return "filling…";

            return tank.FilledRatio <= 0.0 ? "empty" : null;
        }

        /// <summary>
        /// Why this tank will not fill, or null while it might.
        /// </summary>
        /// <remarks>
        /// Stockpile switched off is its own answer: whatever switched it has overruled the step, and
        /// a tank that is not stockpiling hands its gas on as fast as it takes it in.
        /// </remarks>
        public Trouble TroubleWith(IMyTerminalBlock block, string stateId)
        {
            var tank = block as IMyGasTank;
            if (tank == null || stateId != IsFull || tank.FilledRatio >= 1.0)
                return Trouble.None;

            if (!tank.Stockpile)
                return Trouble.Fatal("stopped - something else switched stockpile off");

            return Filling(tank);
        }

        /// <summary>Why a stockpiling tank is not filling, or null while it is or nothing can say.</summary>
        static Trouble Filling(IMyGasTank tank)
        {
            if (!tank.IsWorking)
                return Trouble.Waiting("cannot fill - it is switched off or has no power");

            MyGasTankDefinition definition = Definition(tank);
            if (definition == null || In(tank, definition.StoredGasId) != 0f)
                return Trouble.None;

            return Trouble.Waiting("cannot fill - no " + definition.StoredGasId.SubtypeName.ToLowerInvariant()
                + " coming in");
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
            return sink == null ? -1f : sink.CurrentInputByType(gas);
        }

        /// <summary>What the game says this tank is, or null where nothing has been loaded to say.</summary>
        static MyGasTankDefinition Definition(IMyGasTank tank)
        {
            var definitions = MyDefinitionManager.Static;
            return definitions == null
                ? null
                : definitions.GetCubeBlockDefinition(tank.BlockDefinition) as MyGasTankDefinition;
        }

        /// <summary>Busy while it fills on a stockpile, which is the one filling a step asks for.</summary>
        /// <remarks>
        /// A tank that is not stockpiling fills whenever there is gas to spare, so counting that would
        /// light every tank on a ship with an oxygen generator.
        /// </remarks>
        public bool IsBusy(IMyTerminalBlock block)
        {
            var tank = block as IMyGasTank;
            return tank != null && tank.Stockpile && tank.FilledRatio < 1.0 && In(tank) > 0f;
        }

        /// <summary>Nothing: a tank left stockpiling holds nobody in danger, and switching it off is a step of its own.</summary>
        public void Stop(IMyTerminalBlock block, string actionId)
        {
        }

        /// <inheritdoc />
        public int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            return GridEntity.Unestimated;
        }
    }
}
