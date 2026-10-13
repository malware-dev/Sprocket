namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>What the arguments in a part of a sentence are to the step or trigger holding them.</summary>
    public enum Given
    {
        /// <summary>An action's options, such as a piston's position and speed.</summary>
        Options,

        /// <summary>The new value a step or trigger sets.</summary>
        NewValue,

        /// <summary>The value an if checks blocks for.</summary>
        Checked
    }
}
