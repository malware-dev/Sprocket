namespace Sprocket.Host.SpaceEngineers.Framework.Game.Wiring
{
    /// <summary>Why a trigger's button does not start its sequence on one of its system: whose button, and what its slot holds instead.</summary>
    public sealed class UnansweredButton
    {
        /// <summary>The button of <paramref name="block" />, whose slot is as <paramref name="why" /> says.</summary>
        public UnansweredButton(string block, Unwired why)
        {
            Block = block;
            Why = why;
        }

        /// <summary>The block whose button it is, by the name the construct last gave it, or null where none is known.</summary>
        public string Block { get; private set; }

        /// <summary>What its slot holds instead.</summary>
        public Unwired Why { get; private set; }
    }
}
