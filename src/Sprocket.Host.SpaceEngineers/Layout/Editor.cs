namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>
    /// What a field is filled in with.
    /// </summary>
    /// <remarks>
    /// A small closed set, because each one is a control somebody has to draw. A file that names one
    /// this build has never heard of is read as plain values and keeps its names, so a mod written
    /// against a later Sprocket still works here: it loses the control and nothing else.
    /// </remarks>
    public enum Editor
    {
        /// <summary>A box that takes whatever is put in it, which is nearly everything.</summary>
        Typed,

        /// <summary>A colour, which is the one value nobody should be typing.</summary>
        Colour
    }
}
