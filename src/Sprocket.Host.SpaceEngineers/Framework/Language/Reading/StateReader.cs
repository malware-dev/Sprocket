using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
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

            if (Taken(statement, name.Word, system, false, trouble))
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

            if (Taken(statement, name.Word, system, true, trouble))
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

        /// <summary>
        /// Whether <paramref name="name" /> is already a type or state of <paramref name="system" />, or a slot where
        /// <paramref name="slots" />, and trouble where it is.
        /// </summary>
        /// <remarks>A type and a slot may share a name, and the type is then written qualified, <c>types.side</c>.</remarks>
        public static bool Taken(Statement statement, string name, AutomationSystem system, bool slots, ScriptTrouble trouble)
        {
            if (slots && system.FindPart(name) != null)
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

            var setting = SettingReader.Referenced(statement, value, system);
            var having = new List<string>();
            if (ScriptQuotes.Named(value))
            {
                foreach (var declared in system.Types)
                {
                    if (declared.Has(value.Word))
                        having.Add(declared.Name);
                }
            }

            if (setting != null && value.Form == ValueForm.Qualified && system.FindType(Namespaces.Settings)?.Has(statement.PartsOf(value)[1].Word) == true)
            {
                trouble.Say(statement, $"'{Namespaces.Settings}.{setting}' could be a setting or a choice of the type {Namespaces.Settings}; give the type another name.");
                return false;
            }

            if (setting != null && having.Count > 0)
            {
                SettingReader.Ambiguous(statement, setting, new[] { "a setting", $"a choice of {having[0]}" }, new[] { $"{Namespaces.Settings}.{setting}", $"{having[0]}.{setting}" }, trouble);
                return false;
            }

            if (setting != null)
                return StartingAt(statement, setting, system, trouble, out type, out start);

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
        /// A state starting at the setting <paramref name="setting" />, which each installation gives a flag or a choice: its
        /// type into <paramref name="type" />; or false and trouble where the setting holds neither.
        /// </summary>
        static bool StartingAt(Statement statement, string setting, AutomationSystem system, ScriptTrouble trouble, out string type, out Literal start)
        {
            type = "";
            start = Literal.Setting(setting);
            if (!SettingReader.Owe(statement, setting, system, held => held == SettingReader.Flag || system.FindType(held) != null,
                    "a flag or a choice", trouble))
                return false;

            var kind = system.FindPart(setting).Setting;
            type = kind == SettingReader.Flag ? "" : kind;
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
        /// choices, or a setting holding one, into <paramref name="value" />, or another state of the same type into
        /// <paramref name="other" />; or false and trouble where it is none of those.
        /// </summary>
        public static bool Valued(Statement statement, int at, State state, AutomationSystem system, ScriptTrouble trouble,
            out Literal value, out string other)
        {
            return Valued(statement, at, state.IsFlag ? SettingReader.Flag : state.Type, $"state '{state.Name}'", system, trouble,
                out value, out other);
        }

        /// <summary>
        /// What a state or a setting, <paramref name="about" />, holding <paramref name="type" />, a flag or a declared type, is set
        /// to or compared with at <paramref name="at" />: a flag or one of the type's choices, or a setting holding one, into
        /// <paramref name="value" />, or a state of the type into <paramref name="other" />; or false and trouble where it is
        /// none of those.
        /// </summary>
        /// <remarks>
        /// A bare name may be a state or a setting of any type, or a choice of the type wanted; one that could be more than one is
        /// trouble, and is written qualified: <c>side.inside</c>, <c>settings.home</c>.
        /// </remarks>
        public static bool Valued(Statement statement, int at, string type, string about, AutomationSystem system,
            ScriptTrouble trouble, out Literal value, out string other)
        {
            value = default(Literal);
            other = "";
            var written = statement.ValueAt(at);
            var flag = type == SettingReader.Flag;
            var expected = flag ? "on or off" : $"a choice of {type}";
            if (written.Says(Keyword.On) || written.Says(Keyword.Off))
            {
                if (!flag)
                {
                    trouble.Say(statement, $"{about} holds {expected}, not a flag.");
                    return false;
                }

                value = Literal.Of(written.Says(Keyword.On));
                return true;
            }

            var declared = flag ? null : system.FindType(type);
            string space = null;
            var name = written.Word;
            if (written.Form == ValueForm.Qualified)
            {
                var parts = statement.PartsOf(written);
                if (parts.Count != 2)
                {
                    trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; expected {expected} or a state of the same type.");
                    return false;
                }

                space = parts[0].Word;
                name = parts[1].Word;
            }
            else if (!ScriptQuotes.Named(written))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; expected {expected} or a state of the same type.");
                return false;
            }

            var named = space == null || space == Namespaces.States ? system.FindState(name) : null;
            var part = space == null || space == Namespaces.Settings ? system.FindPart(name) : null;
            var setting = part != null && part.IsSetting;
            var choice = declared != null && declared.Has(name) && (space == null || space == type);
            var meanings = new List<string>();
            var spellings = new List<string>();
            if (named != null)
            {
                meanings.Add("a state");
                spellings.Add($"{Namespaces.States}.{name}");
            }

            if (setting)
            {
                meanings.Add("a setting");
                spellings.Add($"{Namespaces.Settings}.{name}");
            }

            if (choice)
            {
                meanings.Add($"a choice of {type}");
                spellings.Add($"{type}.{name}");
            }

            if (meanings.Count > 1)
            {
                SettingReader.Ambiguous(statement, space == null ? name : $"{space}.{name}", meanings, spellings, trouble);
                return false;
            }

            if (choice)
            {
                value = Literal.Name(name);
                return true;
            }

            if (named != null && (named.IsFlag ? SettingReader.Flag : named.Type) == type)
            {
                other = named.Name;
                return true;
            }

            if (setting)
            {
                value = Literal.Setting(name);
                return SettingReader.Owe(statement, name, system, held => held == type, SettingReader.Holding(type), trouble);
            }

            if (named != null)
            {
                trouble.Say(statement, $"state '{named.Name}' is of another type than {about}.");
                return false;
            }

            if (space == Namespaces.States || space == Namespaces.Settings)
            {
                trouble.Say(statement, $"no {(space == Namespaces.States ? "state" : "setting")} called '{name}'.");
                return false;
            }

            if (space != null)
            {
                if (flag)
                {
                    trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; expected {expected} or a state of the same type.");
                    return false;
                }

                string of;
                return Qualified(statement, at, system, type, trouble, out of, out value);
            }

            trouble.Say(statement, $"no state or choice '{name}'; {about} holds {expected}.");
            return false;
        }

        /// <summary>The state <paramref name="statement" /> names at <paramref name="at" />, bare or as <c>states.where</c>, or null and trouble where the system has none by that name.</summary>
        public static State Named(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var written = statement.ValueAt(at);
            var parts = statement.PartsOf(written);
            var name = parts.Count == 2 && parts[0].Word == Namespaces.Settings ? null : Called(statement, written);
            var state = name == null ? null : system.FindState(name);
            if (state == null)
                trouble.Say(statement, $"no state '{name ?? ScriptQuotes.Spelling(parts)}' declared above this line.");

            return state;
        }

        /// <summary>The name <paramref name="written" /> gives a state or a setting: a bare name, or one qualified by <c>states</c> or <c>settings</c>; null where it is neither.</summary>
        public static string Called(Statement statement, Value written)
        {
            if (ScriptQuotes.Named(written))
                return written.Word;

            var parts = statement.PartsOf(written);
            return parts.Count == 2 && (parts[0].Word == Namespaces.States || parts[0].Word == Namespaces.Settings) ? parts[1].Word : null;
        }

        /// <summary>
        /// <c>where == inside</c> or <c>'Outer light' == on</c> from <paramref name="at" />: a state or a setting compared, equal
        /// or not, with a value, a state or a setting; or null and trouble where it is not that.
        /// </summary>
        public static StateTest Test(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var setting = SettingReader.Referenced(statement, statement.ValueAt(at), system);
            if (setting != null)
                return SettingReader.Test(statement, at, setting, system, trouble);

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
