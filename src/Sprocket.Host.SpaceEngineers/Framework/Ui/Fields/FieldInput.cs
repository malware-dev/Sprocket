
namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>What may be typed into a field's box.</summary>
    public enum FieldInput
    {
        /// <summary>Whatever is typed, read as whatever it spells.</summary>
        Any,

        /// <summary>An integer.</summary>
        Integer,

        /// <summary>A real number, shown with at least one place after the point.</summary>
        Real,

        /// <summary>Text, kept as text whatever it spells.</summary>
        Text,

        /// <summary>One of a list of names a member gives, chosen rather than typed, and given as the name.</summary>
        Name
    }
}
