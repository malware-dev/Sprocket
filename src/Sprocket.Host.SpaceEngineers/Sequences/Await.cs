
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A step that waits for everything started above it to finish.
    /// </summary>
    /// <remarks>
    /// What makes a numbered list mean what it looks like it means: "close the inner door, then open
    /// the outer one" is two acts with one of these between them. An action nothing can tell the end
    /// of - a raw terminal action, or one that takes effect at once - is nothing to wait for, so this
    /// passes straight over it.
    /// </remarks>
    public sealed class Await : Step
    {
    }
}
