using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>A state made in a state selector, and the type made for it there where one was, both still to be added to the logic system.</summary>
    public sealed class NewState
    {
        /// <summary>The state <paramref name="state" />, holding the type <paramref name="type" /> made with it, or null where it holds one the system has.</summary>
        public NewState(State state, DeclaredType type)
        {
            State = state;
            Type = type;
        }

        /// <summary>The state made.</summary>
        public State State { get; }

        /// <summary>The type made for it, or null where it holds a flag or one of the system's types.</summary>
        public DeclaredType Type { get; }
    }
}
