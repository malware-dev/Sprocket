using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
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
        public static List<Field> Fields(EntityAction value, IList<EntityEvent> events, IList<Literal> arguments = null)
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
            Measure(value, compared, arguments);
            foreach (var field in compared)
                fields.Add(value.Holds == ValueKind.Flag ? Stated(field, value, events) : field);

            return fields;
        }

        /// <summary>
        /// Everything a selector asks of a check on <paramref name="value" />: the arguments it is read with where it is a
        /// function, then the relation and what it is compared with, or the relation alone where <paramref name="stated" />.
        /// </summary>
        public static List<Field> Asked(EntityAction value, IList<EntityEvent> events, bool stated)
        {
            var fields = Parameters(value);
            fields.AddRange(stated ? StateFields() : Fields(value, events));
            return fields;
        }

        /// <summary>Gives <paramref name="fields" />, what <paramref name="value" /> compares with, the unit its <paramref name="arguments" /> decide, where they do.</summary>
        public static void Measure(EntityAction value, IList<Field> fields, IList<Literal> arguments)
        {
            var own = value?.OwnValue;
            if (own?.UnitOf == null)
                return;

            var answer = Answer(own, arguments);
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                field.UnitFollows = own.UnitFollows;
                field.UnitOf = own.UnitOf;
                if (answer != null)
                {
                    field.Unit = own.UnitOf(answer, false);
                    field.UnitOfOne = own.UnitOf(answer, true);
                }

                fields[i] = field;
            }
        }

        static string Answer(OwnValue own, IList<Literal> arguments)
        {
            for (var i = 0; arguments != null && i < own.Parameters.Count && i < arguments.Count; i++)
            {
                if (own.Parameters[i].Name == own.UnitFollows)
                    return arguments[i].Said;
            }

            return null;
        }

        /// <summary>The fields <paramref name="value" /> is read with, one per argument in order; empty for a value that takes none.</summary>
        public static List<Field> Parameters(EntityAction value)
        {
            var fields = new List<Field>();
            if (value?.OwnValue != null)
                fields.AddRange(value.OwnValue.Parameters);

            return fields;
        }

        /// <summary>The word joining <paramref name="value" />, read with <paramref name="arguments" />, to its blocks, or null for the usual one.</summary>
        public static string Join(EntityAction value, IList<Literal> arguments)
        {
            var own = value?.OwnValue;
            if (own == null)
                return null;

            for (var i = 0; i < own.Parameters.Count && i < arguments.Count; i++)
            {
                if (own.Parameters[i].IsPart && own.Parameters[i].IsSaid(arguments[i].Said))
                    return own.PartJoin ?? own.Join;
            }

            return own.Join;
        }

        /// <summary>Whether a sentence says anything of <paramref name="arguments" /> given to <paramref name="fields" />.</summary>
        public static bool SaysAny(IList<Field> fields, IList<Literal> arguments)
        {
            for (var i = 0; i < fields.Count && i < arguments.Count; i++)
            {
                if (fields[i].IsSaid(arguments[i].Said))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gives a check the <paramref name="values" /> a panel closed on: the first, what it compares with, to
        /// <paramref name="compared" /> where it <paramref name="checks" />; otherwise all of them, what it is read with, in place
        /// of <paramref name="arguments" />.
        /// </summary>
        public static void Give(IList<Literal> arguments, IList<Literal> values, bool checks, Action<Literal> compared)
        {
            if (checks)
            {
                if (values.Count > 0)
                    compared(values[0]);

                return;
            }

            arguments.Clear();
            for (var i = 0; i < values.Count; i++)
                arguments.Add(values[i]);
        }

        /// <summary>How many of the literals a selector gives back for a check on <paramref name="value" /> are the arguments it is read with.</summary>
        public static int Taken(EntityAction value)
        {
            var taken = 0;
            foreach (var field in Parameters(value))
                taken += field.Covers;

            return taken;
        }

        /// <summary>
        /// What a selector opens holding for a check read with <paramref name="arguments" />, comparing by
        /// <paramref name="relation" /> with <paramref name="compared" />, or by the relation alone where that is null.
        /// </summary>
        public static Literal[] Opening(IList<Literal> arguments, Relation relation, Literal? compared)
        {
            var opening = new List<Literal>(arguments) { Of(relation) };
            if (compared.HasValue)
                opening.Add(compared.Value);

            return opening.ToArray();
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
            var stated = no == null || no.StartsWith("not ", StringComparison.Ordinal)
                ? Field.Among(field.Name, new[] { yes }, new[] { Literal.Of(true).Said })
                : Field.Among(field.Name, new[] { yes, no }, new[] { Literal.Of(true).Said, Literal.Of(false).Said });
            stated.Entry = field.Entry;
            return stated;
        }

        /// <summary>A state's words without the "is" it is read after.</summary>
        static string Word(string state)
        {
            return state.StartsWith("is ", StringComparison.Ordinal) ? state.Substring(3) : state;
        }
    }
}
