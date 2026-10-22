using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What a check compares its value with, and how: the relation it asks first, then what it is compared with.</summary>
    /// <remarks>
    /// The relation is held as the first of a check's arguments, by a word of its own.
    /// </remarks>
    public static class ComparisonQuestion
    {
        const string Name = "comparison";

        // Between a status value and one of its states in the id of the choice that checks for it: status=open.
        const char Between = '=';

        static readonly string[] Same = { "is", "is not" };

        static readonly string[] SameMeans = { "is", "is-not" };

        static readonly string[] Measured = { "is", "is not", "is above", "is at least", "is below", "is at most" };

        static readonly string[] MeasuredMeans = { "is", "is-not", "above", "at-least", "below", "at-most" };

        static readonly Relation[] Relations = { Relation.Is, Relation.IsNot, Relation.Above, Relation.AtLeast, Relation.Below, Relation.AtMost };

        /// <summary>
        /// The fields a check on <paramref name="value" /> asks for: the relation, then what it is compared with, in the state
        /// words <paramref name="events" /> say it in where they say any.
        /// </summary>
        public static List<Field> Fields(EntityAction value, IList<EntityEvent> events)
        {
            var fields = new List<Field>();
            if (value == null)
                return fields;

            var numeric = value.Holds == ValueKind.Integer || value.Holds == ValueKind.Real;
            var relation = Field.Among(Name, numeric ? Measured : Same, numeric ? MeasuredMeans : SameMeans);
            relation.Entry = FieldInput.Name;
            relation.Default = Same[0];
            fields.Add(relation);

            var compared = new List<Field>();
            value.Fields(compared);
            foreach (var field in compared)
                fields.Add(value.Holds == ValueKind.Flag ? Stated(field, value, events) : field);

            return fields;
        }

        /// <summary>The fields a check for one state of a status asks for: the relation alone, the state being chosen already.</summary>
        public static List<Field> StateFields()
        {
            var relation = Field.Among(Name, Same, SameMeans);
            relation.Entry = FieldInput.Name;
            relation.Default = Same[0];
            return new List<Field> { relation };
        }

        /// <summary>The id of the choice that checks the status <paramref name="value" /> for the state <paramref name="state" />, as the program holds it.</summary>
        public static string Id(string value, string state)
        {
            return value + Between + state;
        }

        /// <summary>Whether <paramref name="id" /> is a choice checking a status for one of its states, and which.</summary>
        public static bool Split(string id, out string value, out string state)
        {
            var at = id.IndexOf(Between);
            value = at < 0 ? id : id.Substring(0, at);
            state = at < 0 ? "" : id.Substring(at + 1);
            return at >= 0;
        }

        /// <summary>The literal a check's arguments hold <paramref name="relation" /> as.</summary>
        public static Literal Of(Relation relation)
        {
            for (var i = 0; i < Relations.Length; i++)
            {
                if (Relations[i] == relation)
                    return Literal.Name(MeasuredMeans[i]);
            }

            return Literal.Name(MeasuredMeans[0]);
        }

        /// <summary>The words <paramref name="relation" /> is said in where it is asked: "is", "is not", "is above".</summary>
        public static string Said(Relation relation)
        {
            for (var i = 0; i < Relations.Length; i++)
            {
                if (Relations[i] == relation)
                    return Measured[i];
            }

            return Measured[0];
        }

        /// <summary>The relation <paramref name="said" /> holds, or <see cref="Relation.Is" /> where it holds none.</summary>
        public static Relation Read(Literal said)
        {
            for (var i = 0; i < MeasuredMeans.Length; i++)
            {
                if (said.Kind == ValueKind.Name && said.Text == MeasuredMeans[i])
                    return Relations[i];
            }

            return Relation.Is;
        }

        /// <summary>
        /// A flag's field in the words its signals say its two sides in, where they say both: "occupied" and "empty" rather
        /// than on and off.
        /// </summary>
        static Field Stated(Field field, EntityAction value, IList<EntityEvent> events)
        {
            string yes = null, no = null;
            for (var i = 0; i < events.Count; i++)
            {
                var signal = events[i];
                if (signal.Value != value.Id || !signal.IsState || signal.Becomes.Kind != ValueKind.Flag)
                    continue;

                if (signal.Becomes.Flag)
                    yes = Word(signal.State);
                else
                    no = Word(signal.State);
            }

            if (yes == null && value.IsStatus && !string.IsNullOrEmpty(value.Named))
                yes = char.ToLowerInvariant(value.Named[0]) + value.Named.Substring(1);

            if (yes == null)
                return field;

            // A side said only as the other's absence, "not working", is the relation's to say, so the flag has one word.
            var stated = no == null || no.StartsWith("not ", System.StringComparison.Ordinal)
                ? Field.Among(field.Name, new[] { yes }, new[] { Literal.Of(true).Said })
                : Field.Among(field.Name, new[] { yes, no }, new[] { Literal.Of(true).Said, Literal.Of(false).Said });
            stated.Entry = field.Entry;
            return stated;
        }

        /// <summary>A state's words without the "is" it is read after.</summary>
        static string Word(string state)
        {
            return state.StartsWith("is ", System.StringComparison.Ordinal) ? state.Substring(3) : state;
        }
    }
}
