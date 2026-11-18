using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Every type of choices this host provides, in its namespace <see cref="BlockKinds.Own" />.</summary>
    public static class ChoiceTypes
    {
        /// <summary>A door's status.</summary>
        public static readonly ChoiceType DoorStatus = new ChoiceType("door_status", "open", "closed", "opening", "closing");

        /// <summary>A piston's status.</summary>
        public static readonly ChoiceType PistonStatus = new ChoiceType("piston_status", "extended", "retracted", "extending", "retracting", "stopped");

        /// <summary>Whether a connector or a landing gear is locked.</summary>
        public static readonly ChoiceType LockStatus = new ChoiceType("lock_status", "unlocked", "ready-to-lock", "locked");

        /// <summary>A merge block's status.</summary>
        public static readonly ChoiceType MergeStatus = new ChoiceType("merge_status", "merged", "ready-to-merge", "nothing-in-reach", "not-working");

        /// <summary>A jump drive's status.</summary>
        public static readonly ChoiceType JumpStatus = new ChoiceType("jump_status", "charging", "ready", "jumping");

        /// <summary>A laser antenna's link.</summary>
        public static readonly ChoiceType LinkStatus = new ChoiceType("link_status", "idle", "turning", "searching", "connecting", "connected", "out-of-range");

        /// <summary>What an air vent is doing to its room.</summary>
        public static readonly ChoiceType RoomStatus = new ChoiceType("room_status", "filling", "full", "emptying", "empty", "stopped");

        /// <summary>A drill's mode.</summary>
        public static readonly ChoiceType DrillMode = new ChoiceType("drill_mode", "mining", "clearing-terrain");

        /// <summary>An assembler's mode.</summary>
        public static readonly ChoiceType AssemblerMode = new ChoiceType("assembler_mode", "assembling", "disassembling");

        /// <summary>What a screen shows.</summary>
        public static readonly ChoiceType ScreenContent = new ChoiceType("screen_content", "nothing", "text and images", "app");

        /// <summary>How a screen's text is aligned.</summary>
        public static readonly ChoiceType Alignment = new ChoiceType("alignment", "left", "center", "right");

        /// <summary>Whether a piston move's speed is each piston's or all of theirs together.</summary>
        public static readonly ChoiceType SpeedSplit = new ChoiceType("speed_split", "each", "total");

        // Every type, for finding one by its name; an immutable table read where a program names a type.
        static readonly ChoiceType[] All =
        {
            DoorStatus, PistonStatus, LockStatus, MergeStatus, JumpStatus, LinkStatus, RoomStatus, DrillMode, AssemblerMode,
            ScreenContent, Alignment, SpeedSplit
        };

        /// <summary>The one type that has the choice <paramref name="choice" />, or null where none or more than one has it.</summary>
        public static ChoiceType Having(string choice)
        {
            ChoiceType having = null;
            foreach (var type in All)
            {
                if (!type.Has(choice))
                    continue;

                if (having != null)
                    return null;

                having = type;
            }

            return having;
        }

        /// <summary>The type called <paramref name="id" />, or null where this host provides none.</summary>
        public static ChoiceType Named(string id)
        {
            foreach (var type in All)
            {
                if (type.Id == id)
                    return type;
            }

            return null;
        }
    }
}
