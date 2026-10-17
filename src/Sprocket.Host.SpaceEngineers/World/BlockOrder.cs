
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What one block is told by one step, made ready once and given every time the step runs.
    /// </summary>
    /// <remarks>Made when a program is bound, so a step that runs does nothing but give it.</remarks>
    public abstract class BlockOrder
    {
        /// <summary>Tells the block what it was made ready to be told.</summary>
        public abstract void Give();
    }
}
