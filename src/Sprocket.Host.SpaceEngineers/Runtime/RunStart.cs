namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>What started a run, as the debug log says it.</summary>
    public enum RunStart
    {
        /// <summary>The sequence's own trigger.</summary>
        Trigger,
        /// <summary>Another sequence's run step, or a button.</summary>
        Call,
        /// <summary>Somebody at the editor.</summary>
        Hand
    }
}
