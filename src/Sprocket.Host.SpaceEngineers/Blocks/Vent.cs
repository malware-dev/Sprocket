using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// The air vent, which the terminal describes only as a switch and which the airlock is built around.
    /// </summary>
    /// <remarks>
    /// It has no events of any kind - see docs/Catalogue.md - so all of this is sampled, and a room that
    /// empties and refills between two ticks is invisible. That bound was accepted knowingly; it is worth
    /// remembering that the vent is the block it bites.
    /// <para>
    /// <b>Neither the full nor the empty reading reads the reported status, and that is deliberate.</b> The
    /// game cannot answer either question with it: an airtight room never reports Depressurized, because
    /// that value is reserved for a room that is not airtight or is not there, and it only reports
    /// Pressurized once the level has reached exactly one. Both are raised with the game as a bug; neither
    /// is waitable meanwhile.
    /// </para>
    /// <para>
    /// So both come from the oxygen in the room, and the vent has to be working for the reading to mean
    /// anything. That last part is what keeps a loading world safe: a vent with no power, or with no room
    /// worked out yet, reports nothing left whatever the room actually holds. Anything Sprocket cannot read
    /// is "not yet" and never "done" - a sequence held at a wait can be recovered from, and one that opened
    /// a door on a room it could not read cannot.
    /// </para>
    /// </remarks>
    public sealed class Vent : Capability
    {
        const float Emptied = 0.05f;

        const float Filled = 0.95f;

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
            vent.IsWorking && vent.GetOxygenLevel() >= Filled && !VentGas.Working(vent, false, seen));

        // Emptied is a band rather than a reading. A sealed room with gas to spare settles below full, 95.4% on an
        // airlock measured in game, and nothing says where a given room stops. The game's own status cannot stand in:
        // it calls a room pressurized only at a level of one or more, and reports an empty airtight room as
        // depressurizing.
        static readonly Reading Empty = Reading.Remembering<IMyAirVent>((vent, seen) =>
            vent.IsWorking && vent.GetOxygenLevel() <= Emptied && !VentGas.Working(vent, true, seen));

        static readonly Doing Fills = Doing.Of<IMyAirVent, Stuck>(Stuck.Read,
            (vent, stuck, seen) => vent.Depressurize = false, Air,
            (vent, stuck, seen) => vent.IsWorking && Passes(Filling(vent, seen), stuck, Cause.VentNoOxygen),
            (vent, stuck, seen) => vent.IsWorking ? Unless(Filling(vent, seen), stuck, Cause.VentNoOxygen) : Trouble.None);

        static readonly Doing Empties = Doing.Of<IMyAirVent, Stuck>(Stuck.Read,
            (vent, stuck, seen) => vent.Depressurize = true, Empty,
            (vent, stuck, seen) => vent.IsWorking && Passes(Emptying(vent, seen), stuck, Cause.VentNowhere),
            (vent, stuck, seen) => vent.IsWorking ? Unless(Emptying(vent, seen), stuck, Cause.VentNowhere) : Trouble.None);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyAirVent;
        }

        /// <inheritdoc />
        /// <remarks>Nothing to call off: a vent goes on with the air it was moving, and a room half emptied is no kind of stop.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithEvent("filled", Air, Edge.Rising, "finishes filling").Reading("has air", "has no air")
                .Looked("A vent raises no event of its own, and nothing it announces rules a room's air in or out.")
                .WithEvent("emptied", Empty, Edge.Rising, "finishes emptying").Reading("is empty", "is not empty")
                .Looked("A vent raises no event of its own, and nothing it announces rules a room's air in or out.")
                .WithAction(Pressurize, "pressurize", Fills, "Depressurize_Off").Lasting().Asking(Asked(
                    "**Hold** waits until the room is full. Where there is no more oxygen to give, as when the tanks are "
                    + "empty, that wait never ends.\n\n**Move on** carries on once the room will fill no further, with what "
                    + "air it has. A room that is not airtight always holds: carrying on would open it to the leak."))
                .WithAction(Depressurize, "depressurize", Empties, "Depressurize_On").Lasting().Asking(Asked(
                    "**Hold** waits until the room is empty. Where its air has nowhere to go, as when the tanks are full, "
                    + "that wait never ends.\n\n**Move on** carries on once the room will empty no further, and what comes "
                    + "next, such as an outer door opening, lets the rest of the air out."));
        }

        /// <inheritdoc />
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var vent = block as IMyAirVent;
            if (vent == null)
                return Status.None;

            // What it cannot do comes before what it says it is doing, asked about the thing it is
            // currently set to do. A vent that has stopped moving gas short of where it was going
            // goes on reporting Pressurizing for as long as the world exists, so the game's own
            // word for it is the one thing that never says so.
            Trouble stuck = Said(vent);
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

        /// <summary>
        /// What a room told to fill or empty does when it is stuck: hold, waiting until it gets there, or move on, carrying on
        /// once the gas has nowhere to come from or go to. <paramref name="about" /> says which is which for the action.
        /// </summary>
        /// <remarks>
        /// Full tanks are the common case, and an airlock waiting on them never lets anybody out. Holding is what the action
        /// does anyway, so a sentence leaves it out, and says "or move on" where it moves on.
        /// </remarks>
        static Field Asked(string about)
        {
            var field = Field.Among("when stuck", new[] { "move on", "hold" }, new[] { Literal.Of(MoveOn).Said, Literal.Of(Hold).Said });
            field.Entry = Entry.Whole;
            field.Default = "hold";
            field.Quiet = "hold";
            field.Lead = "or";
            field.About = about;
            return field;
        }

        /// <summary>
        /// Why the room will not finish what a step asked, or none while it might: what stands in its way, unless the step
        /// moves on past it.
        /// </summary>
        static Trouble Unless(Trouble trouble, Stuck stuck, Cause passes)
        {
            return Passes(trouble, stuck, passes) ? Trouble.None : trouble;
        }

        /// <summary>
        /// Whether <paramref name="trouble" /> is one a step told to move on lets pass: <paramref name="passes" />, which is
        /// nothing to move the gas with.
        /// </summary>
        /// <remarks>A room that is not airtight never passes: carrying on would open it to the leak.</remarks>
        static bool Passes(Trouble trouble, Stuck stuck, Cause passes)
        {
            return stuck.MovesOn && trouble.Cause == passes;
        }

        /// <summary>
        /// Why the room will not finish what the vent is set to do, for a screen rather than for a step.
        /// </summary>
        /// <remarks>
        /// Reads the gas without keeping a clock on it. Drawing a status must not move the timing
        /// the runtime settles a step by, and it has no use for it: what is on screen is about this
        /// moment and is drawn again a moment later.
        /// </remarks>
        static Trouble Said(IMyAirVent vent)
        {
            if (vent.Depressurize)
                return vent.GetOxygenLevel() > Emptied && !VentGas.Moving(vent, true)
                    ? Trouble.Waiting(Cause.VentNowhere)
                    : Trouble.None;

            if (vent.GetOxygenLevel() >= Filled)
                return Trouble.None;

            if (!vent.CanPressurize)
                return Trouble.Waiting(Cause.VentNotAirtight);

            return VentGas.Moving(vent, false)
                ? Trouble.None
                : Trouble.Waiting(Cause.VentNoOxygen);
        }

        /// <summary>
        /// Why the room will not empty, or null while it still might.
        /// </summary>
        /// <remarks>
        /// Told to fill instead is its own answer and not a silence. Two sequences share one vent -
        /// a cycle out and a cycle in - and the one that has been overruled is left waiting for a
        /// state nothing is heading towards any more.
        /// </remarks>
        static Trouble Emptying(IMyAirVent vent, Recall seen)
        {
            if (vent.GetOxygenLevel() <= Emptied)
                return Trouble.None;

            if (!vent.Depressurize)
                return Trouble.Fatal(Cause.VentToldToFill);

            return VentGas.Working(vent, true, seen)
                ? Trouble.None
                : Trouble.Waiting(Cause.VentNowhere);
        }

        /// <summary>Why the room will not fill, or null while it still might.</summary>
        static Trouble Filling(IMyAirVent vent, Recall seen)
        {
            if (vent.GetOxygenLevel() >= Filled)
                return Trouble.None;

            if (vent.Depressurize)
                return Trouble.Fatal(Cause.VentToldToEmpty);

            if (!vent.CanPressurize)
                return Trouble.Waiting(Cause.VentNotAirtight);

            return VentGas.Working(vent, false, seen)
                ? Trouble.None
                : Trouble.Waiting(Cause.VentNoOxygen);
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
