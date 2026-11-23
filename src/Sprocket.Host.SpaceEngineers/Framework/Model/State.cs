using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A state a logic system declares: a named place for one value, which each of its installations keeps for its whole
    /// life, <c>let where = inside</c>.
    /// </summary>
    public sealed class State
    {
        /// <summary>The built-in types a state holds besides a flag, as the language names them, in the order the editor offers them; no declared type takes these names.</summary>
        public static readonly IReadOnlyList<string> BuiltIn = new[] { "integer", "real", "text", "duration" };

        /// <summary>
        /// A state called <paramref name="name" />, starting at <paramref name="start" />, of the type <paramref name="type" />:
        /// a flag where it is empty, a built-in type such as <c>integer</c>, or a declared type.
        /// </summary>
        public State(string name, string type, Literal start)
        {
            Name = name;
            Type = type;
            Start = start;
        }

        /// <summary>The state's name, by which steps, tests and triggers name it.</summary>
        public string Name { get; set; }

        /// <summary>
        /// The type it holds: empty for a flag, <c>integer</c>, <c>real</c>, <c>text</c> or <c>duration</c> for a built-in
        /// one, or the name of the declared type it holds a choice of.
        /// </summary>
        public string Type { get; set; }

        /// <summary>Whether it holds a flag.</summary>
        public bool IsFlag => string.IsNullOrEmpty(Type);

        /// <summary>Whether it holds a value of a built-in type other than a flag: an integer, a real, a text or a duration.</summary>
        public bool IsBuiltIn => IsBuiltInType(Type);

        /// <summary>Whether it holds a choice of a declared type.</summary>
        public bool IsChoice => !IsFlag && !IsBuiltIn;

        /// <summary>Whether it holds a number or a length, which compare as larger and smaller as well as equal.</summary>
        public bool IsOrdered => Type == "integer" || Type == "real" || Type == "duration";

        /// <summary>The kind of value it holds.</summary>
        public ValueKind Kind
        {
            get
            {
                switch (Type)
                {
                    case "": return ValueKind.Flag;
                    case null: return ValueKind.Flag;
                    case "integer": return ValueKind.Integer;
                    case "real": return ValueKind.Real;
                    case "text": return ValueKind.Text;
                    case "duration": return ValueKind.Duration;
                }

                return ValueKind.Name;
            }
        }

        /// <summary>The value a new installation starts it at: a flag, a value of its built-in type, or the name of one of its type's choices.</summary>
        public Literal Start { get; set; }

        /// <summary>What somebody wrote about the state, in their own words, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, held as written, or null where there are none.</summary>
        public UnusedAttributes Attributes { get; set; }

        /// <summary>Whether <paramref name="type" /> names a built-in type a state can hold besides a flag: <c>integer</c>, <c>real</c>, <c>text</c> or <c>duration</c>.</summary>
        public static bool IsBuiltInType(string type)
        {
            for (var i = 0; i < BuiltIn.Count; i++)
            {
                if (BuiltIn[i] == type)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="value" /> is of the kind a state of <paramref name="kind" /> holds: its own, or an integer for a real.</summary>
        public static bool Takes(ValueKind kind, ValueKind value)
        {
            return value == kind || kind == ValueKind.Real && value == ValueKind.Integer;
        }
    }
}
