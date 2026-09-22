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
        Colour,

        /// <summary>
        /// One of a listed few, chosen rather than typed.
        /// </summary>
        /// <remarks>
        /// The only value a form can be sure of, and so the only one where typing can be wrong and
        /// nothing will say so: a number outside its bounds is refused out loud, while a word the
        /// block does not know is written into the program and declined in silence when it runs.
        /// <para>
        /// How many there are is not part of this. Two of them are a switch and more are a row to
        /// pick from, which is a question about what fits on a screen rather than about the value.
        /// </para>
        /// </remarks>
        Chosen,

        /// <summary>An angle in degrees, typed until it has a control of its own.</summary>
        Angle
    }
}
