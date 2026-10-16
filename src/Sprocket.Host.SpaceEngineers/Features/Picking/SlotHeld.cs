using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What one slot of a toolbar a person presses holds, as the slots offered under that toolbar show it.</summary>
    public sealed class SlotHeld
    {
        /// <summary>Whether something of the game's sits in the slot: an action, a group's action or a thing.</summary>
        public bool Taken;

        /// <summary>Whether the slot already answers a trigger of an automation.</summary>
        public bool Answering;

        /// <summary>The picture the game draws for what the slot holds, or empty for none.</summary>
        public string Picture = "";

        /// <summary>What the slot holds in words, on the first of the blocks it holds anything on, or empty where it holds nothing.</summary>
        public string Says = "";

        /// <summary>What the slot holds on each of the blocks it holds anything on, one line each, the first being <see cref="Says" />.</summary>
        public readonly List<string> Lines = new List<string>();

        /// <summary>The label a player gave the slot or its button, or empty for none.</summary>
        public string Label = "";

        /// <summary>Whether the slot holds anything at all.</summary>
        public bool Holds => Taken || Answering;
    }
}
