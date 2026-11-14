namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>How a test reads the blocks it names: <c>any</c>, <c>all</c>, <c>sum</c> or <c>average</c>.</summary>
    public enum Quantifier
    {
        /// <summary>The test holding on one block is enough.</summary>
        Any,

        /// <summary>The test must hold on every block.</summary>
        All,

        /// <summary>The member is read on every block and the readings added, and the total is tested once.</summary>
        Sum,

        /// <summary>The member is read on every block and the readings averaged, and the average is tested once.</summary>
        Average
    }
}
