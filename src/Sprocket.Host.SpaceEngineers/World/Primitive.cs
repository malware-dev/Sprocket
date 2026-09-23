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

        Boolean,
        Byte,
        SByte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Char,
        String
    }
}
