namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>One attribute of a statement as written: its name and its one value.</summary>
    public struct ScriptAttribute
    {
        /// <summary>The attribute <paramref name="name" />, holding <paramref name="value" />, written as <paramref name="written" />.</summary>
        public ScriptAttribute(string name, Value value, string written)
        {
            Name = name;
            Value = value;
            Written = written;
        }

        /// <summary>The attribute's name, an identifier.</summary>
        public string Name { get; }

        /// <summary>The attribute's value, read as any value of a statement is.</summary>
        public Value Value { get; }

        /// <summary>The value's text exactly as it was written, so an attribute nothing reads is written back unchanged.</summary>
        public string Written { get; }
    }
}
