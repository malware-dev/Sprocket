
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// The type the game takes one of an action's values as.
    /// </summary>
    /// <remarks>
    /// The game's own, not the language's: a program writes a whole number and the game may want it
    /// as a byte, a 32-bit integer or a float, and refuses anything else. A value is put into this
    /// once, when a program is bound. Spelled in a description file as the name in lower case:
    /// <c>byte</c>, <c>int32</c>, <c>single</c>.
    /// </remarks>
    public enum Primitive
    {
        /// <summary>Nothing says, so a value goes as it was written.</summary>
        None,

        /// <summary>A truth value.</summary>
        Boolean,
        /// <summary>A whole number from 0 to 255.</summary>
        Byte,
        /// <summary>A whole number from -128 to 127.</summary>
        SByte,
        /// <summary>A whole number in 16 bits, signed.</summary>
        Int16,
        /// <summary>A whole number in 16 bits, unsigned.</summary>
        UInt16,
        /// <summary>A whole number in 32 bits, signed.</summary>
        Int32,
        /// <summary>A whole number in 32 bits, unsigned.</summary>
        UInt32,
        /// <summary>A whole number in 64 bits, signed.</summary>
        Int64,
        /// <summary>A whole number in 64 bits, unsigned.</summary>
        UInt64,
        /// <summary>A real number in 32 bits.</summary>
        Single,
        /// <summary>A real number in 64 bits.</summary>
        Double,
        /// <summary>One character.</summary>
        Char,
        /// <summary>Text.</summary>
        String
    }
}
