using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A type a logic system declares: a named set of choices its states may hold, <c>type side = inside | outside</c>.</summary>
    public sealed class DeclaredType
    {
        readonly List<string> _options = new List<string>();

        /// <summary>A type called <paramref name="name" />, with no choices yet.</summary>
        public DeclaredType(string name)
        {
            Name = name;
        }

        /// <summary>The type's name, by which a choice is qualified: <c>side</c> of <c>side.inside</c>.</summary>
        public string Name { get; set; }

        /// <summary>Its choices, in the order they are written.</summary>
        public IList<string> Choices => _options;

        /// <summary>What somebody wrote about the type, in their own words, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>Whether <paramref name="choice" /> is one of its choices.</summary>
        public bool Has(string choice)
        {
            return _options.Contains(choice);
        }
    }
}
