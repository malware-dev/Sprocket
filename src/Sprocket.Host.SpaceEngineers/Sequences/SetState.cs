using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A step that gives one of the logic system's states a new value, in the installation it runs for: <c>set where to outside</c>.</summary>
    public sealed class SetState : Step
    {
        /// <summary>The name of the state set.</summary>
        public string State { get; set; } = "";

        /// <summary>What it is set to where <see cref="From" /> is empty: a flag, or the name of a choice.</summary>
        public Literal Value { get; set; }

        /// <summary>The name of the state whose value it takes, or empty where it is set to <see cref="Value" />.</summary>
        public string From { get; set; } = "";
    }
}
