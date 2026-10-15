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
        Checked,

        /// <summary>How an if compares what it reads with the value it checks for: is, is not, is below.</summary>
        Compared,

        /// <summary>How an if reads its blocks, where it adds a value up over them: the sum of.</summary>
        Read
    }
}
