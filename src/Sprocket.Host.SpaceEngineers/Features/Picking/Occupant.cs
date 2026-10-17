using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What already answers a toolbar slot: a trigger of the program on one of its system, or, with no trigger, a slot the program did not write.</summary>
    public sealed class Occupant
    {
        /// <summary>The block whose toolbar it is.</summary>
        public GridEntity Panel { get; set; }

        /// <summary>The slot answered.</summary>
        public ToolbarSlot At { get; set; }

        /// <summary>The logic system whose trigger answers it, or null where the program did not write it.</summary>
        public AutomationSystem System { get; set; }

        /// <summary>The trigger that answers it, or null where the program did not write it.</summary>
        public SignalTrigger Trigger { get; set; }

        /// <summary>The installation the trigger answers it through, or null.</summary>
        public SystemInstance Instance { get; set; }

        /// <summary>Whether <paramref name="other" /> is the same claim: the same slot of the same block, by the same trigger and installation.</summary>
        public bool Same(Occupant other)
        {
            return Panel.Id == other.Panel.Id && At.Same(other.At) && Trigger == other.Trigger && Instance == other.Instance;
        }
    }
}
