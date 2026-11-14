namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A state a logic system declares: a named place for one value, which each of its installations keeps for its whole
    /// life, <c>let where = inside</c>.
    /// </summary>
    public sealed class State
    {
        /// <summary>A state called <paramref name="name" />, starting at <paramref name="start" />, of the type <paramref name="type" /> or a flag where it is empty.</summary>
        public State(string name, string type, Literal start)
        {
            Name = name;
            Type = type;
            Start = start;
        }

        /// <summary>The state's name, by which steps, tests and triggers name it.</summary>
        public string Name { get; set; }

        /// <summary>The name of the declared type it holds a choice of, or empty for a flag.</summary>
        public string Type { get; set; }

        /// <summary>Whether it holds a flag rather than a choice of a declared type.</summary>
        public bool IsFlag => string.IsNullOrEmpty(Type);

        /// <summary>The value a new installation starts it at: a flag, or the name of one of its type's choices.</summary>
        public Literal Start { get; set; }

        /// <summary>What somebody wrote about the state, in their own words, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }
    }
}
