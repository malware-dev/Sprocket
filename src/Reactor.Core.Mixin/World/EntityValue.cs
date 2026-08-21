namespace Reactor.World
{
    /// <summary>What sort of reading a property gives.</summary>
    public enum ValueKind
    {
        Flag,
        Number,
        Text
    }

    /// <summary>
    /// One reading off an entity.
    /// </summary>
    /// <remarks>
    /// A struct, and deliberately not <c>object</c>. Every running sequence reads properties
    /// on every tick, on a server with many constructs; boxing a bool that often would put
    /// the garbage collector in the game's update loop, which is the one place it must never
    /// be. Holding a string reference in a struct field costs nothing - the string is already
    /// a reference, and nothing here copies one.
    ///
    /// A flag is a value like any other, and so is a name. The model used to have boolean
    /// signals as its primitive, which had it backwards: a boolean is a property whose
    /// readings happen to be true and false, no more special than a fill level or the name
    /// somebody gave a block.
    /// </remarks>
    public struct EntityValue
    {
        // Chained through the default constructor because core is pinned to C# 6, where a
        // struct may not touch its own auto-properties until every field is assigned.
        EntityValue(ValueKind kind, bool flag, double number, string text) : this()
        {
            Kind = kind;
            Flag = flag;
            Number = number;
            _text = text;
        }

        public static EntityValue Of(bool flag)
        {
            return new EntityValue(ValueKind.Flag, flag, flag ? 1 : 0, flag ? "yes" : "no");
        }

        public static EntityValue Of(double number)
        {
            return new EntityValue(ValueKind.Number, number != 0, number, null);
        }

        public static EntityValue Of(string text)
        {
            return new EntityValue(ValueKind.Text, !string.IsNullOrEmpty(text), 0, text);
        }

        public ValueKind Kind { get; private set; }

        /// <summary>The reading as a flag. A number is false at zero, a text when it is empty.</summary>
        public bool Flag { get; private set; }

        /// <summary>The reading as a number. A flag reads one or zero.</summary>
        public double Number { get; private set; }

        /// <summary>
        /// The reading as text, never null.
        /// </summary>
        /// <remarks>
        /// A number gives back nothing here rather than a rendering of itself. How a number
        /// is written - decimals, thousands, a unit - is presentation, and core deciding it
        /// once for everybody is exactly the sort of thing the host is meant to own.
        /// </remarks>
        public string Text { get { return _text ?? ""; } }

        readonly string _text;
    }
}
