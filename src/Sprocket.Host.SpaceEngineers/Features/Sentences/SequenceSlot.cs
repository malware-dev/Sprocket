using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// A sequence of the same system, as the thing a <c>run</c> starts.
    /// </summary>
    /// <remarks>
    /// The number and the name both, because what a person should read is the name and what is
    /// actually stored is the number: a sequence nobody has named has no words of its own, and what
    /// to call it then is the target's to decide.
    /// </remarks>
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

        /// <summary>Nothing it could name, which is a program that cannot run.</summary>
        public override bool IsEmpty => Number < 1;
    }
}
