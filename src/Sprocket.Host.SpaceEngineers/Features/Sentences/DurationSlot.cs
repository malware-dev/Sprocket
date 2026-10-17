using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>A length of time, in the unit the program writes it in.</summary>
    public sealed class DurationSlot : SentenceSlot
    {
        /// <summary>A part saying <paramref name="length" />, pressed by <paramref name="opens" />.</summary>
        public DurationSlot(Duration length, ICommand opens) : base(opens)
        {
            Length = length;
        }

        /// <summary>The length it says.</summary>
        public Duration Length { get; private set; }

        /// <summary>False, since a duration always has a value.</summary>
        public override bool IsEmpty => false;
    }
}
