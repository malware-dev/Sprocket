using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>
    /// A sequence of the same system, as the thing a <c>run</c> starts.
    /// </summary>
    public sealed class SequenceSlot : SentenceSlot
    {
        /// <summary>The part naming the sequence <paramref name="number" />, called <paramref name="name" />, opening the question it was answered with through <paramref name="opens" />.</summary>
        public SequenceSlot(int number, string name, ICommand opens) : base(opens)
        {
            Number = number;
            Name = name ?? "";
        }

        /// <summary>The number of the sequence the part names.</summary>
        public int Number { get; private set; }

        /// <summary>What it is called, or empty where nobody has named it.</summary>
        public string Name { get; private set; }

        /// <summary>Whether it names no sequence, which is a program that cannot run.</summary>
        public override bool IsEmpty => Number < 1;
    }
}
