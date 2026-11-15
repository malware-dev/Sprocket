namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// The start of a part of a sequence that runs as its run ends, after its own steps: a <see cref="Catch" />, run where the
    /// run fails, or a <see cref="Finally" />, run however it ends. Its steps follow it, up to the next one or the sequence's end.
    /// </summary>
    public abstract class Handler : Step
    {
    }
}
