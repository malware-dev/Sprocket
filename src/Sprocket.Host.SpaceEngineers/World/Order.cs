namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What one block is told by one step, made ready once and given every time the step runs.
    /// </summary>
    /// <remarks>
    /// Made when a program is bound: the block's action or value found, and every value built as the
    /// type the game takes it as. So a step that runs does nothing but give it.
    /// </remarks>
    public abstract class Order
    {
        public abstract void Give();
    }
}
