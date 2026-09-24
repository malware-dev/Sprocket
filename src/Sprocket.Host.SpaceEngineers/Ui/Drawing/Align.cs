namespace Sprocket.Host.SpaceEngineers.Ui.Drawing
{
    /// <summary>Which end of a run of text a position names.</summary>
    /// <remarks>
    ///     Horizontal and nothing else. A text panel aligns across and not down, so an alignment naming
    ///     both at once could not be reproduced there; one naming only this can.
    /// </remarks>
    public enum Align
    {
        /// <summary>The position is where the run starts.</summary>
        Left,

        /// <summary>The position is where the run ends.</summary>
        Right
    }
}