namespace Sprocket.Localization.Generator
{
    /// <summary>One text in a .resx file: its key, its value, the note beside it, and the line it starts on.</summary>
    sealed class Entry
    {
        public Entry(string key, string value, string comment, int line)
        {
            Key = key;
            Value = value;
            Comment = comment;
            Line = line;
        }

        /// <summary>The key the text is found by.</summary>
        public string Key { get; }

        /// <summary>The text.</summary>
        public string Value { get; }

        /// <summary>The note for whoever translates it, or null.</summary>
        public string Comment { get; }

        /// <summary>The line in the file the entry starts on, from zero.</summary>
        public int Line { get; }
    }
}
