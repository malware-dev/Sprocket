using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Reads a logic system's types and states out of the script, and what its steps, tests and triggers do with a state.</summary>
    public static class StateReader
    {
        // The names of the built-in types, which a declared type may not take.
        static readonly string[] BuiltIn = { "flag", "text", "integer", "real", "duration", "color", "vector", "list", "block" };

        /// <summary><c>type side = inside | outside</c>: a type of <paramref name="system" />, its choices in order.</summary>
        /// <remarks>One with fewer than two choices, a choice twice, or a name already taken is trouble and not read.</remarks>
        public static void ReadType(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var name = statement.ValueAt(0);
            if (!ScriptQuotes.Named(name))
            {
                trouble.Say(statement, "expected the type's name after 'type'.");
                return;
            }

            if (Array.IndexOf(BuiltIn, name.Word) >= 0)
            {
                trouble.Say(statement, $"'{name.Word}' is a built-in type; a declared type takes another name.");
                return;
            }

            if (Taken(statement, name.Word, system, trouble))
                return;

            if (!statement.Says(1, Keyword.Equals))
            {
                trouble.Say(statement, "expected '=' and the type's choices after its name.");
                return;
            }

            var type = new DeclaredType(name.Word);
            for (var at = 2; at < statement.Count; at += 2)
            {
                var choice = statement.ValueAt(at);
                if (!ScriptQuotes.Named(choice))
                {
                    trouble.Say(statement, $"expected a choice, a name, where {ScriptQuotes.Spelled(statement, at)} is.");
                    return;
                }

                if (type.Has(choice.Word))
                {
                    trouble.Say(statement, $"choice '{choice.Word}' is written twice in type '{type.Name}'.");
                    return;
                }

                type.Choices.Add(choice.Word);
                if (at + 1 < statement.Count && !statement.Says(at + 1, Keyword.Bar))
                {
                    trouble.Say(statement, $"expected '|' between the choices, where {ScriptQuotes.Spelled(statement, at + 1)} is.");
                    return;
                }
            }

            if (type.Choices.Count < 2)
            {
                trouble.Say(statement, $"type '{type.Name}' needs at least two choices.");
                return;
            }

            type.Annotation = statement.Annotation;
            type.Attributes = ScriptAttributes.Unused(statement);
            system.Types.Add(type);
        }

        /// <summary><c>let where = inside</c>: a state of <paramref name="system" />, its type that of its starting value.</summary>
        /// <remarks>A starting value that is neither a flag nor one of the system's choices, or a name already taken, is trouble.</remarks>
        public static void ReadState(Statement statement, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            var name = statement.ValueAt(0);
            if (!ScriptQuotes.Named(name))
            {
                trouble.Say(statement, "expected the state's name after 'let'.");
                return;
            }

            if (Taken(statement, name.Word, system, trouble))
                return;

            if (!system.IsBlueprint && (program.FindRef(name.Word) != null || program.FindSignalRef(name.Word) != null))
            {
                trouble.Say(statement, $"'{name.Word}' is already a variable of the file; a state takes another name.");
                return;
            }

            if (!statement.Says(1, Keyword.Equals) || statement.Count < 3)
            {
                trouble.Say(statement, "expected let <name> = <value>, the state's starting value.");
                return;
            }

            if (!ScriptQuotes.Ends(statement, 3, trouble))
                return;

            string type;
            Literal start;
            if (!Starting(statement, 2, system, trouble, out type, out start))
                return;

            var state = new State(name.Word, type, start);
            state.Annotation = statement.Annotation;
            state.Attributes = ScriptAttributes.Unused(statement);
            system.States.Add(state);
        }

        /// <summary>Whether <paramref name="name" /> is already a slot, type or state of <paramref name="system" />, and trouble where it is.</summary>
        public static bool Taken(Statement statement, string name, AutomationSystem system, ScriptTrouble trouble)
        {
            if (system.FindPart(name) != null)
                trouble.Say(statement, $"'{name}' is already the name of a slot.");
            else if (system.FindType(name) != null)
                trouble.Say(statement, $"'{name}' is already the name of a type.");
            else if (system.FindState(name) != null)
                trouble.Say(statement, $"'{name}' is already the name of a state.");
            else
                return false;

            return true;
        }

        /// <summary>
        /// A state's starting value at <paramref name="at" />: a flag, an empty type, or a choice and the name of its type;
        /// or false and trouble where it is neither.
        /// </summary>
        /// <remarks>A bare choice the system has in more than one type cannot be told apart, and must be qualified.</remarks>
        static bool Starting(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble, out string type,
            out Literal start)
        {
            type = "";
            start = default(Literal);
            var value = statement.ValueAt(at);
            if (value.Says(Keyword.On) || value.Says(Keyword.Off))
            {
                start = Literal.Of(value.Says(Keyword.On));
                return true;
            }

            if (value.Form == ValueForm.Qualified)
                return Qualified(statement, at, system, null, trouble, out type, out start);

            if (!ScriptQuotes.Named(value))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; a state starts as on, off or one of the system's choices.");
                return false;
            }

            if (system.FindState(value.Word) != null)
            {
                trouble.Say(statement, $"'{value.Word}' is a state; a state starts as on, off or one of the system's choices.");
                return false;
            }

            var having = new List<string>();
            foreach (var declared in system.Types)
            {
                if (declared.Has(value.Word))
                    having.Add(declared.Name);
            }

            if (having.Count == 0)
            {
                trouble.Say(statement, $"no choice '{value.Word}' in the system's types.");
                return false;
            }

            if (having.Count > 1)
            {
                trouble.Say(statement, $"'{value.Word}' is a choice of {having[0]} and {having[1]}; qualify it, like {having[0]}.{value.Word}.");
                return false;
            }

            type = having[0];
            start = Literal.Name(value.Word);
            return true;
        }

        /// <summary>
        /// <c>side.inside</c> at <paramref name="at" />: a choice qualified by one of the system's types, of the type
        /// <paramref name="wanted" /> where that is not null; or false and trouble where it is not that.
        /// </summary>
        static bool Qualified(Statement statement, int at, AutomationSystem system, string wanted, ScriptTrouble trouble,
            out string type, out Literal choice)
        {
            type = "";
            choice = default(Literal);
            var parts = statement.Parts(at);
            var declared = parts.Count == 2 ? system.FindType(parts[0].Word) : null;
            if (declared == null)
            {
                trouble.Say(statement, $"no type '{(parts.Count == 2 ? parts[0].Word : ScriptQuotes.Spelling(parts))}' in the system.");
                return false;
            }

            if (!declared.Has(parts[1].Word))
            {
                trouble.Say(statement, $"no choice '{parts[1].Word}' in type '{declared.Name}'.");
                return false;
            }

            if (wanted != null && declared.Name != wanted)
            {
                trouble.Say(statement, $"'{declared.Name}.{parts[1].Word}' is of type {declared.Name}; expected a choice of {wanted}.");
                return false;
            }

            type = declared.Name;
            choice = Literal.Name(parts[1].Word);
            return true;
        }

        /// <summary>
        /// What <paramref name="state" /> is set to or compared with at <paramref name="at" />: a flag or one of its type's
        /// choices into <paramref name="value" />, or another state of the same type into <paramref name="other" />; or
        /// false and trouble where it is none of those.
        /// </summary>
        /// <remarks>A bare name is the state of that name where there is one, and otherwise a choice.</remarks>
        public static bool Valued(Statement statement, int at, State state, AutomationSystem system, ScriptTrouble trouble,
            out Literal value, out string other)
        {
            value = default(Literal);
            other = "";
            var written = statement.ValueAt(at);
            var expected = state.IsFlag ? "on or off" : $"a choice of {state.Type}";
            if (written.Says(Keyword.On) || written.Says(Keyword.Off))
            {
                if (!state.IsFlag)
                {
                    trouble.Say(statement, $"state '{state.Name}' holds {expected}, not a flag.");
                    return false;
                }

                value = Literal.Of(written.Says(Keyword.On));
                return true;
            }

            if (written.Form == ValueForm.Qualified && !state.IsFlag)
            {
                string type;
                return Qualified(statement, at, system, state.Type, trouble, out type, out value);
            }

            if (!ScriptQuotes.Named(written))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; expected {expected} or a state of the same type.");
                return false;
            }

            var named = system.FindState(written.Word);
            if (named != null)
            {
                if (named.Type != state.Type)
                {
                    trouble.Say(statement, $"state '{named.Name}' is of another type than '{state.Name}'.");
                    return false;
                }

                other = named.Name;
                return true;
            }

            var declared = state.IsFlag ? null : system.FindType(state.Type);
            if (declared == null || !declared.Has(written.Word))
            {
                trouble.Say(statement, $"no state or choice '{written.Word}'; state '{state.Name}' holds {expected}.");
                return false;
            }

            value = Literal.Name(written.Word);
            return true;
        }

        /// <summary>The state <paramref name="statement" /> names at <paramref name="at" />, or null and trouble where the system has none by that name.</summary>
        public static State Named(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var state = system.FindState(statement.Word(at));
            if (state == null)
                trouble.Say(statement, $"no state '{statement.Word(at)}' declared above this line.");

            return state;
        }

        /// <summary>
        /// <c>where == inside</c> from <paramref name="at" />: a state compared, equal or not, with a value or another state;
        /// or null and trouble where it is not that.
        /// </summary>
        public static StateTest Test(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var state = Named(statement, at, system, trouble);
            if (state == null)
                return null;

            var relation = statement.ValueAt(at + 1);
            if (!relation.Says(Keyword.EqualTo) && !relation.Says(Keyword.NotEqualTo))
            {
                trouble.Say(statement, "expected == or != after the state.");
                return null;
            }

            if (statement.Count <= at + 2)
            {
                trouble.Say(statement, "expected a value after the comparison.");
                return null;
            }

            Literal value;
            string other;
            if (!Valued(statement, at + 2, state, system, trouble, out value, out other))
                return null;

            return new StateTest
            {
                State = state.Name,
                Relation = relation.Says(Keyword.EqualTo) ? Relation.Is : Relation.IsNot,
                Value = value,
                Other = other
            };
        }

        /// <summary><c>set where to outside</c>: a state given a value, or another state's; or null and trouble where it is not that.</summary>
        public static SetState Set(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var state = Named(statement, 0, system, trouble);
            if (state == null)
                return null;

            if (!statement.Says(1, Keyword.To) || statement.Count < 3)
            {
                trouble.Say(statement, "expected set <state> to <value>.");
                return null;
            }

            if (!ScriptQuotes.Ends(statement, 3, trouble))
                return null;

            Literal value;
            string other;
            if (!Valued(statement, 2, state, system, trouble, out value, out other))
                return null;

            return new SetState { State = state.Name, Value = value, From = other };
        }
    }
}
