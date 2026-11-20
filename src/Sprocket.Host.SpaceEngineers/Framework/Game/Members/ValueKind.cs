
namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>What sort of reading a property gives.</summary>
    /// <remarks>
    /// The game's <c>ITerminalProperty.TypeName</c> is one of <c>Boolean</c>, <c>Single</c>, <c>Int64</c>,
    /// <c>StringBuilder</c> and <c>Color</c>.
    /// </remarks>
    public enum ValueKind
    {
        /// <summary>On or off.</summary>
        Flag,

        /// <summary>
        /// A whole number, held exactly.
        /// </summary>
        /// <remarks>An entity id runs to seventeen digits, past the 2^53 a <c>double</c> holds exactly.</remarks>
        Integer,

        /// <summary>A real number.</summary>
        Real,

        /// <summary>Text.</summary>
        Text,

        /// <summary>
        /// Red, green, blue and alpha.
        /// </summary>
        /// <remarks>Written in a file as #RRGGBBAA, and stored packed inside the number.</remarks>
        Color,

        /// <summary>One of a list of names a member gives, written as the name: <c>closed</c>.</summary>
        /// <remarks>Stored as the name rather than as a position in the list, so a list that gains a name keeps its meaning.</remarks>
        Name,

        /// <summary>Slots, by name, for a parameter that takes slots.</summary>
        Slots,

        /// <summary>A length of time, as a setting holds one: in ticks, or in seconds.</summary>
        /// <remarks>No block's value is one; a step given one is given its ticks.</remarks>
        Duration,

        /// <summary>One of a system blueprint's settings, by name, standing in for the value each installation gives it.</summary>
        /// <remarks>Never handed to a block: the installation's value takes its place first.</remarks>
        Setting
    }
}
