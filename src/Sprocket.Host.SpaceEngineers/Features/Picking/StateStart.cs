using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>A value a state can start at, the word it is offered by, and the type it makes the state hold.</summary>
    public sealed class StateStart
    {
        /// <summary>The value <paramref name="value" />, offered as <paramref name="word" />, of the type <paramref name="type" /> or a flag where it is empty.</summary>
        public StateStart(string word, Literal value, string type)
        {
            Word = word;
            Value = value;
            Type = type;
        }

        /// <summary>The word it is offered by: the value, said with its type where another value is said the same.</summary>
        public string Word { get; }

        /// <summary>The value: a flag, or the name of a choice.</summary>
        public Literal Value { get; }

        /// <summary>The name of the type it is a choice of, or empty for a flag.</summary>
        public string Type { get; }
    }
}
