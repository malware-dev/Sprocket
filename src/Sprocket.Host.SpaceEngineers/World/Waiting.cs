namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>How the editor shows and edits a sequence's waits, the variants of the trial an admin chooses between.</summary>
    public enum Waiting
    {
        /// <summary>Each step that takes time has a switch saying whether the sequence waits for it.</summary>
        Switch,

        /// <summary>A wait is a step of its own in the list.</summary>
        Step
    }
}
