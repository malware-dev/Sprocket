using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>The states Sprocket puts its controls in, each of which a style gives a look of its own.</summary>
    /// <remarks>
    ///     All weigh less than hot (100), so a look for one lies under a style's hot look, as a style based on the plain one
    ///     would; a look a state has while hot is a joining with <see cref="State.PointerOver" />. Still outweighs hot, so a thing
    ///     that cannot be pressed never looks hot.
    /// </remarks>
    public static class LookStates
    {
        /// <summary>Quieter than one that runs: a paused step's words, marks and parts.</summary>
        public static readonly State Paused = State.Declare("Paused", 40);

        /// <summary>Nothing fills it yet, so it reads as an invitation.</summary>
        public static readonly State Empty = State.Declare("Empty", 50);

        /// <summary>What was typed or given in it cannot be used.</summary>
        public static readonly State Wrong = State.Declare("Wrong", 50);

        /// <summary>It holds a slot's blocks or signal.</summary>
        public static readonly State Taken = State.Declare("Taken", 50);

        /// <summary>A thing set aside from what it stands among: switched off, as tips are, or listed only to be read about.</summary>
        public static readonly State Off = State.Declare("Off", 50);

        /// <summary>It stands under another, one step in.</summary>
        public static readonly State Nested = State.Declare("Nested", 50);

        /// <summary>Its words are set apart, as a name not yet given is.</summary>
        public static readonly State Emphasized = State.Declare("Emphasized", 50);

        /// <summary>Something about it is worth a warning.</summary>
        public static readonly State Warned = State.Declare("Warned", 50);

        /// <summary>Something about it is wrong.</summary>
        public static readonly State Broken = State.Declare("Broken", 60);

        /// <summary>It is the one chosen, or the one shown, among those beside it.</summary>
        public static readonly State Chosen = State.Declare("Chosen", 60);

        /// <summary>It is waiting for the signal that fills it.</summary>
        public static readonly State Answering = State.Declare("Answering", 70);

        /// <summary>It heads the rows below it.</summary>
        public static readonly State Heading = State.Declare("Heading", 80);

        /// <summary>It cannot be pressed: it has no command, or cannot be used.</summary>
        public static readonly State Still = State.Declare("Still", 150);
    }
}
