
namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>
    /// What a field is filled in with.
    /// </summary>
    public enum FieldEditor
    {
        /// <summary>A box that takes whatever is put in it.</summary>
        Typed,

        /// <summary>A color.</summary>
        Color,

        /// <summary>
        /// One of a listed few, chosen rather than typed.
        /// </summary>
        Chosen,

        /// <summary>An angle in degrees.</summary>
        Angle,

        /// <summary>Text over several lines, in a box where Enter starts a new one.</summary>
        Lines
    }
}
