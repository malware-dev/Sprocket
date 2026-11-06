using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

// The status lives in the Ingame namespace, which cannot be imported whole: the vent's interface is declared in both,
// and the mod-side one is the one with the events on it.
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The air vent, which the airlock is built around.</summary>
    /// <remarks>
    /// The game raises no event a vent's room can be heard by, so all of it is sampled, and a room that empties and refills
    /// between two ticks is invisible. The vent's toolbar, whose two slots the game runs as the room fills and as it empties,
    /// is the game's own and stays in the terminal view. Anything that cannot be read is not yet, never done: a vent with no power, or no room worked out yet as
    /// a world loads, reports nothing left whatever the room holds.
    /// </remarks>
    public sealed class Vent : Capability
    {
        const float Emptied = 0.05f;

        const float Filled = 0.95f;

        const string Unheard = "A vent raises no event of its own, and nothing it announces rules a room's air in or out.";

        /// <summary>The action that fills the room, which may be told to carry on where there is no more oxygen to give.</summary>
        public const string Pressurize = "pressurize";

        /// <summary>The action that empties the room, which may be told to carry on where the air has nowhere to go.</summary>
        public const string Depressurize = "depressurize";

        /// <summary>What a stuck room told to fill or empty does: wait until it gets there.</summary>
        public const long Hold = 0;

        /// <summary>What a stuck room told to fill or empty does: carry on once the gas has nowhere to come from or go to.</summary>
        public const long MoveOn = 1;

        // Inside the band and no longer moving gas. Both halves are needed: the level alone finishes a room the vent
        // is still taking to full, and the gas alone would finish one it gave up on at a tenth.
        static readonly Reading Air = Reading.Remembering<IMyAirVent>((vent, seen) =>
            vent.IsWorking && vent.GetOxygenLevel() >= Filled && !VentFlow.Working(vent, false, seen));

        // Emptied is a band rather than a reading. A sealed room with gas to spare settles below full, 95.4% on an
        // airlock measured in game, and nothing says where a given room stops. The game's own status cannot stand in:
        // it calls a room pressurized only at a level of one or more, and reports an empty airtight room as
        // depressurizing.
        static readonly Reading Empty = Reading.Remembering<IMyAirVent>((vent, seen) =>
            vent.IsWorking && vent.GetOxygenLevel() <= Emptied && !VentFlow.Working(vent, true, seen));

        static readonly string[] Rooms = { "filling", "full", "emptying", "empty", "stopped" };

        // Full and empty as the readings above judge them, so a test of the room and a step that fills it agree. A room that
        // is not airtight never fills, and stays filling. Written out rather than asking Air and Empty, which would read
        // whether the vent works and its level once each on every look (BudgetTests.ALookedForMomentIsLookedAtOnItsCadence).
        static readonly OwnValue Room = OwnValue.OneOf<IMyAirVent>(Rooms, (vent, seen) =>
        {
            if (!vent.IsWorking)
                return 4;

            var level = vent.GetOxygenLevel();
            if (level >= Filled && !VentFlow.Working(vent, false, seen))
                return 1;

            if (level <= Emptied && !VentFlow.Working(vent, true, seen))
                return 3;

            return vent.Depressurize ? 2 : 0;
        });

        static readonly OwnValue Airtight = OwnValue.Flag(Reading.Of<IMyAirVent>(vent => vent.CanPressurize));

        // The game reads the room as a fraction; every fill Sprocket says is in percent.
        static readonly OwnValue Level = OwnValue.Number<IMyAirVent>(vent => vent.GetOxygenLevel() * 100.0);

        static readonly OwnValue Into = OwnValue.Number<IMyAirVent>(vent => vent.GasInputPerSecond);

        static readonly OwnValue OutOf = OwnValue.Number<IMyAirVent>(vent => vent.GasOutputPerSecond);

        static readonly OwnAction Fills = OwnAction.Of<IMyAirVent, Stuck>(Stuck.Read,
            (vent, stuck, seen) => vent.Depressurize = false, Air,
            (vent, stuck, seen) => vent.IsWorking && Passes(Filling(vent, seen), stuck, Cause.VentNoOxygen),
            (vent, stuck, seen) => vent.IsWorking ? Unless(Filling(vent, seen), stuck, Cause.VentNoOxygen) : BlockTrouble.None);

        static readonly OwnAction Empties = OwnAction.Of<IMyAirVent, Stuck>(Stuck.Read,
            (vent, stuck, seen) => vent.Depressurize = true, Empty,
            (vent, stuck, seen) => vent.IsWorking && Passes(Emptying(vent, seen), stuck, Cause.VentNowhere),
            (vent, stuck, seen) => vent.IsWorking ? Unless(Emptying(vent, seen), stuck, Cause.VentNowhere) : BlockTrouble.None);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyAirVent;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("room_status", "room status", Room).Status().Looked(Unheard).In(Category.Air)
                .WithChange("finishes_filling", "finishes filling", "room_status", Literal.Name("full"))
                .Reading("has air", "has no air").Looked(Unheard)
                .WithChange("finishes_emptying", "finishes emptying", "room_status", Literal.Name("empty"))
                .Reading("is empty", "is not empty").Looked(Unheard)
                .WithValue("airtight", "airtight", Airtight).Looked(Unheard).In(Category.Air)
                .WithValue("oxygen_level", "oxygen level", Level).Percent().Looked(Unheard).In(Category.Air)
                .WithValue("flow_into_the_room", "flow into the room", Into).Measured(" L/s").Looked(Unheard).In(Category.Air)
                .WithValue("flow_out_of_the_room", "flow out of the room", OutOf).Measured(" L/s").Looked(Unheard).In(Category.Air)
                .Renaming("Depressurize", true, "depressurizing").Stating("Depressurize")
                .Renaming("Depressurize", false, "toggle depressurizing")
                .WithAction(Pressurize, "pressurize", Fills, "Depressurize_Off").Lasting().Asking(Asked())
                .WithAction(Depressurize, "depressurize", Empties, "Depressurize_On").Lasting().Asking(Asked());
        }

        /// <inheritdoc />
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return Status.None;

            // What it cannot do comes first: a vent that has stopped moving gas short of where it was going goes on reporting
            // Pressurizing for as long as the world exists.
            BlockTrouble stuck = Said(vent);
            if (stuck.Exists)
                return Status.Of(stuck);

            switch (vent.Status)
            {
                case VentStatus.Pressurized: return Status.Of(Condition.Pressurized);
                case VentStatus.Depressurized: return Status.Of(Condition.Empty);
                case VentStatus.Pressurizing: return Status.Of(Condition.Filling);
                case VentStatus.Depressurizing: return Status.Of(Condition.Emptying);
            }

            return Status.None;
        }

        /// <inheritdoc />
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            return vent != null
                && (vent.Status == VentStatus.Pressurizing || vent.Status == VentStatus.Depressurizing);
        }

        /// <summary>What a room told to fill or empty does when it is stuck, hold or move on.</summary>
        static Field Asked()
        {
            var field = Field.Among("when stuck", new[] { "move on", "hold" }, new[] { Literal.Of(MoveOn).Said, Literal.Of(Hold).Said });
            field.Entry = FieldInput.Whole;
            field.Default = "hold";
            field.Quiet = "hold";
            field.Lead = "or";
            return field;
        }

        /// <summary>Why the room will not finish what a step asked, or none while it might, unless the step moves on past it.</summary>
        static BlockTrouble Unless(BlockTrouble trouble, Stuck stuck, Cause passes)
        {
            return Passes(trouble, stuck, passes) ? BlockTrouble.None : trouble;
        }

        /// <summary>Whether <paramref name="trouble" /> is <paramref name="passes" />, nothing to move the gas with, which a step told to move on lets pass.</summary>
        /// <remarks>A room that is not airtight never passes.</remarks>
        static bool Passes(BlockTrouble trouble, Stuck stuck, Cause passes)
        {
            return stuck.MovesOn && trouble.Cause == passes;
        }

        /// <summary>Why the room will not finish what the vent is set to do, for a screen rather than for a step.</summary>
        /// <remarks>Keeps no clock on the gas, so drawing a status leaves the timing a step is settled by as it is.</remarks>
        static BlockTrouble Said(IMyAirVent vent)
        {
            if (vent.Depressurize)
                return vent.GetOxygenLevel() > Emptied && !VentFlow.Moving(vent, true)
                    ? BlockTrouble.Waiting(Cause.VentNowhere)
                    : BlockTrouble.None;

            if (vent.GetOxygenLevel() >= Filled)
                return BlockTrouble.None;

            if (!vent.CanPressurize)
                return BlockTrouble.Waiting(Cause.VentNotAirtight);

            return VentFlow.Moving(vent, false)
                ? BlockTrouble.None
                : BlockTrouble.Waiting(Cause.VentNoOxygen);
        }

        /// <summary>Why the room will not empty, or null while it still might.</summary>
        static BlockTrouble Emptying(IMyAirVent vent, ReaderMemory seen)
        {
            if (vent.GetOxygenLevel() <= Emptied)
                return BlockTrouble.None;

            if (!vent.Depressurize)
                return BlockTrouble.Fatal(Cause.VentToldToFill);

            return VentFlow.Working(vent, true, seen)
                ? BlockTrouble.None
                : BlockTrouble.Waiting(Cause.VentNowhere);
        }

        /// <summary>Why the room will not fill, or null while it still might.</summary>
        static BlockTrouble Filling(IMyAirVent vent, ReaderMemory seen)
        {
            if (vent.GetOxygenLevel() >= Filled)
                return BlockTrouble.None;

            if (vent.Depressurize)
                return BlockTrouble.Fatal(Cause.VentToldToEmpty);

            if (!vent.CanPressurize)
                return BlockTrouble.Waiting(Cause.VentNotAirtight);

            return VentFlow.Working(vent, false, seen)
                ? BlockTrouble.None
                : BlockTrouble.Waiting(Cause.VentNoOxygen);
        }

        /// <summary>What a step told a vent to do where the room is stuck.</summary>
        sealed class Stuck
        {
            /// <summary>Whether it carries on once the gas has nowhere to come from or go to, rather than holding.</summary>
            public bool MovesOn;

            /// <summary>What a step's options come to: moving on only where it says so.</summary>
            public static Stuck Read(IList<Literal> arguments)
            {
                return new Stuck
                {
                    MovesOn = arguments != null && arguments.Count > 0 && arguments[0].Kind == ValueKind.Integer
                        && arguments[0].Whole == MoveOn
                };
            }
        }
    }
}
