
namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>What may be typed into a field's box.</summary>
    public enum Entry
    {
        /// <summary>Whatever is typed, read as whatever it spells.</summary>
        Any,

        /// <summary>A whole number.</summary>
        Whole,

        /// <summary>A number that may have a fraction, shown with at least one place after the point.</summary>
        Decimal,

        /// <summary>Text, kept as text whatever it spells.</summary>
        Text,

        /// <summary>One of a list of names a member gives, chosen rather than typed, and given as the name.</summary>
        Name
    }
}
