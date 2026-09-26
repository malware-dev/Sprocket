namespace Sprocket.Localization.Generator
{
    /// <summary>One text the generated class says: the key it is found by, the member that says it, and its words as written.</summary>
    sealed class Said
    {
        public Said(string key, string member, string written, string comment)
        {
            Key = key;
            Member = member;
            Written = written;
            Comment = comment;
        }

        /// <summary>The key.</summary>
        public string Key { get; }

        /// <summary>The name of the member that says it.</summary>
        public string Member { get; }

        /// <summary>The words as written.</summary>
        public string Written { get; }

        /// <summary>The note for whoever translates it, or null.</summary>
        public string Comment { get; }
    }
}
