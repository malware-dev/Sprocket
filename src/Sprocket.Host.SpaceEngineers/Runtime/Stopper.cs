namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>What stopped a sequence's last run from outside, before it finished.</summary>
    public enum Stopper
    {
        /// <summary>Nothing did: it finished, or it has not run since.</summary>
        None,

        /// <summary>Another sequence of its system, by a step that stops it.</summary>
        Sequence,

        /// <summary>A trigger of its system that stops it.</summary>
        Trigger,

        /// <summary>Somebody at the editor.</summary>
        Hand
    }
}
