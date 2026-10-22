using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One of the logic system's states, as what a step sets, what an if tests, or the value either takes from it.</summary>
    public sealed class SystemStateSlot : SentenceSlot
    {
        /// <summary>The part naming the state <paramref name="name" />, opening the question it was answered with through <paramref name="opens" />.</summary>
        public SystemStateSlot(string name, ICommand opens) : base(opens)
        {
            Name = name ?? "";
        }

        /// <summary>The name of the state the part names.</summary>
        public string Name { get; private set; }

        /// <summary>Whether it names no state, which is a program that cannot run.</summary>
        public override bool IsEmpty => Name.Length == 0;
    }
}
