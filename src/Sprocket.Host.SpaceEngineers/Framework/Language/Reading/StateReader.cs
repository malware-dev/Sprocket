using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
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

        /// <summary>Whether <paramref name="name" />, in any case, is a built-in type's, which no declared type may take.</summary>
        /// <remarks>The script tells names apart by case; a person reading the editor does not.</remarks>
        public static bool IsBuiltInInAnyCase(string name)
        {
            foreach (var builtIn in BuiltIn)
            {
                if (string.Equals(builtIn, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

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

            if (system.FindType(name.Word) != null)
            {
                trouble.Say(statement, $"'{name.Word}' is already the name of a type.");
                return;
            }

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
        /// <remarks>A starting value that is no flag, number, text, length of time or choice of the system, or a name already taken, is trouble.</remarks>
        public static void ReadState(Statement statement, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            var name = statement.ValueAt(0);
            if (!ScriptQuotes.Named(name))
            {
                trouble.Say(statement, "expected the state's name after 'let'.");
                return;
            }

            if (system.FindState(name.Word) != null)
            {
                trouble.Say(statement, $"'{name.Word}' is already the name of a state.");
                return;
            }

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

            if (ExpressionReader.Is(statement.ValueAt(2)))
            {
                trouble.Constant(statement);
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
        /// A state's starting value at <paramref name="at" />: a flag, a setting holding one or a choice, or a choice and the
        /// name of its type; or false and trouble where it is none of those.
        /// </summary>
        /// <remarks>A bare choice the system has in more than one type cannot be told apart, and is written with its type.</remarks>
        static bool Starting(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble, out string type,
            out Literal start)
        {
            type = "";
            start = default(Literal);
            var value = statement.ValueAt(at);
            bool flag, bare;
            if (Kinds.Flag(statement, value, out flag, out bare))
            {
                if (bare && !Alone(statement, value.Word, system, trouble))
                    return false;

                start = Literal.Of(flag);
                return true;
            }

            var builtIn = StartsBuiltIn(statement, value);
            if (builtIn != null)
            {
                bool said;
                if (SettingReader.Typed(statement, value, builtIn, system, trouble, out said, out start))
                {
                    type = builtIn;
                    return true;
                }

                if (!said)
                    trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}, which is not written as a value of {builtIn}.");

                return false;
            }

            if (value.Form == ValueForm.Kinded)
            {
                var setting = Kinds.Named(statement, value, Kinds.Setting);
                if (setting != null)
                    return StartingAt(statement, setting, system, trouble, out type, out start);

                if (Kinds.Named(statement, value, Kinds.State) != null)
                {
                    trouble.Say(statement, $"{ScriptQuotes.Spelled(statement, at)} is a state; a state starts as on, off, a number, a text, a length of time or one of the system's choices.");
                    return false;
                }

                DeclaredType of;
                string choice;
                if (!Choice(statement, value, system, trouble, out of, out choice))
                    return false;

                type = of.Name;
                start = Literal.Name(choice);
                return true;
            }

            if (!ScriptQuotes.Named(value))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; a state starts as on, off, a number, a text, a length of time or one of the system's choices.");
                return false;
            }

            var name = value.Word;
            var meanings = new List<string>();
            var spellings = new List<string>();
            Kinds.Meanings(system, name, meanings, spellings);
            var having = new List<string>();
            foreach (var declared in system.Types)
            {
                if (declared.Has(name))
                {
                    having.Add(declared.Name);
                    meanings.Add($"a choice of {declared.Name}");
                    spellings.Add(ScriptWriter.KindSpelling(declared.Name, name));
                }
            }

            if (meanings.Count > 1)
            {
                Kinds.Ambiguous(statement, name, meanings, spellings, trouble);
                return false;
            }

            var part = system.FindPart(name);
            if (part != null && part.IsSetting)
                return StartingAt(statement, name, system, trouble, out type, out start);

            if (system.FindState(name) != null)
            {
                trouble.Say(statement, $"'{name}' is a state; a state starts as on, off, a number, a text, a length of time or one of the system's choices.");
                return false;
            }

            if (having.Count == 0)
            {
                trouble.Say(statement, $"no choice '{name}' in the system's types.");
                return false;
            }

            type = having[0];
            start = Literal.Name(name);
            return true;
        }

        /// <summary>
        /// The built-in type other than a flag that <paramref name="written" />, a state's starting value, starts a state of: its
        /// kind where it is written with one, <c>real(0)</c>, or else what it is written as, a whole number starting an integer
        /// state; null where it is none of them.
        /// </summary>
        static string StartsBuiltIn(Statement statement, Value written)
        {
            if (written.Form == ValueForm.Kinded)
            {
                var kind = statement.KindOf(written);
                return kind.IsWord && State.IsBuiltInType(kind.Word) ? kind.Word : null;
            }

            switch (written.Form)
            {
                case ValueForm.Integer: return SettingReader.Integer;
                case ValueForm.Real: return SettingReader.Real;
                case ValueForm.Text: return SettingReader.Text;
                case ValueForm.Duration: return SettingReader.Duration;
            }

            return null;
        }

        /// <summary>What <paramref name="state" /> holds, in words for trouble: <c>a flag</c>, <c>an integer</c>, <c>a choice of side</c>.</summary>
        static string Holding(State state)
        {
            return SettingReader.Holding(state.IsFlag ? SettingReader.Flag : state.Type);
        }

        /// <summary>
        /// Whether the bare flag <paramref name="name" /> starting a state means the flag alone; trouble where it could also mean
        /// a thing of <paramref name="system" /> so named, or a choice of any of its types, no type being known.
        /// </summary>
        static bool Alone(Statement statement, string name, AutomationSystem system, ScriptTrouble trouble)
        {
            var meanings = new List<string>();
            var spellings = new List<string>();
            Kinds.Meanings(system, name, meanings, spellings);
            foreach (var declared in system.Types)
            {
                if (declared.Has(name))
                {
                    meanings.Add($"a choice of {declared.Name}");
                    spellings.Add(ScriptWriter.KindSpelling(declared.Name, name));
                }
            }

            return Kinds.FlagAlone(statement, name, meanings, spellings, trouble);
        }

        /// <summary>
        /// A state starting at the setting <paramref name="setting" />, which each installation gives a value a state holds: its
        /// type into <paramref name="type" />; or false and trouble where the setting holds none, such as a color.
        /// </summary>
        static bool StartingAt(Statement statement, string setting, AutomationSystem system, ScriptTrouble trouble, out string type, out Literal start)
        {
            type = "";
            start = Literal.Setting(setting);
            if (!SettingReader.Owe(statement, setting, system, held => held == SettingReader.Flag || State.IsBuiltInType(held) || system.FindType(held) != null,
                    "a flag, a number, a text, a length of time or a choice", trouble))
                return false;

            var kind = system.FindPart(setting).Setting;
            type = kind == SettingReader.Flag ? "" : kind;
            return true;
        }

        /// <summary>
        /// The type and the choice of <paramref name="written" />, a choice with its type: <c>side(inside)</c>, or
        /// <c>type(side)(inside)</c>; or false and trouble where its kind is no type of the system, the choice none of the
        /// type's, or a bare type's name is also another thing's.
        /// </summary>
        public static bool Choice(Statement statement, Value written, AutomationSystem system, ScriptTrouble trouble, out DeclaredType type,
            out string choice)
        {
            type = null;
            choice = null;
            var kind = statement.KindOf(written);
            var thing = statement.ThingOf(written);
            var typed = Kinds.Named(statement, kind, Keywords.Name(Keyword.Type));
            var bare = typed == null && ScriptQuotes.Named(kind) ? kind.Word : null;
            var name = typed ?? bare;
            if (name == null || !ScriptQuotes.Named(thing))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, written)}; expected a choice with its type, like side(inside).");
                return false;
            }

            type = system.FindType(name);
            if (type == null)
            {
                trouble.Say(statement, $"no type '{name}' in the system.");
                return false;
            }

            if (bare != null)
            {
                var meanings = new List<string>();
                var spellings = new List<string>();
                Kinds.Meanings(system, name, meanings, spellings);
                if (meanings.Count > 1)
                {
                    Kinds.Ambiguous(statement, name, meanings, spellings, trouble);
                    return false;
                }
            }

            choice = thing.Word;
            if (!type.Has(choice))
            {
                trouble.Say(statement, $"no choice '{choice}' in type '{type.Name}'.");
                return false;
            }

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
        /// A bare choice is looked for among the type's choices alone; a bare name that could also be anything else the system
        /// names is trouble, and is written with its kind: <c>side(inside)</c>, <c>state(home)</c>.
        /// </remarks>
        public static bool Valued(Statement statement, int at, string type, string about, AutomationSystem system,
            ScriptTrouble trouble, out Literal value, out string other)
        {
            value = default(Literal);
            other = "";
            var written = statement.ValueAt(at);
            if (State.IsBuiltInType(type))
                return BuiltInValued(statement, written, type, about, system, trouble, out value, out other);

            var flag = type == SettingReader.Flag;
            var expected = flag ? "on or off" : $"a choice of {type}";
            // A bare on or off is looked for among the type's choices where the type is not a flag, as any bare choice is.
            bool on, bare;
            var flagged = Kinds.Flag(statement, written, out on, out bare);
            if (flagged && (flag || !bare))
            {
                if (!flag)
                {
                    trouble.Say(statement, $"{about} holds {expected}, not a flag.");
                    return false;
                }

                if (bare && !Kinds.FlagAlone(statement, written.Word, system, trouble))
                    return false;

                value = Literal.Of(on);
                return true;
            }

            if (written.Form == ValueForm.Kinded)
                return Kinded(statement, written, type, about, system, trouble, out value, out other);

            if (!ScriptQuotes.Named(written))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; expected {expected} or a state of the same type.");
                return false;
            }

            var name = written.Word;
            var declared = flag ? null : system.FindType(type);
            var choice = declared != null && declared.Has(name);
            var meanings = new List<string>();
            var spellings = new List<string>();
            Kinds.Meanings(system, name, meanings, spellings);
            if (choice)
            {
                meanings.Add($"a choice of {type}");
                spellings.Add(ScriptWriter.KindSpelling(type, name));
            }

            if (meanings.Count > 1)
            {
                Kinds.Ambiguous(statement, name, meanings, spellings, trouble);
                return false;
            }

            if (choice)
            {
                value = Literal.Name(name);
                return true;
            }

            var named = system.FindState(name);
            if (named != null)
                return Other(statement, named, type, about, trouble, out other);

            var part = system.FindPart(name);
            if (part != null && part.IsSetting)
            {
                value = Literal.Setting(name);
                return SettingReader.Owe(statement, name, system, held => held == type, SettingReader.Holding(type), trouble);
            }

            trouble.Say(statement, flagged ? $"{about} holds {expected}, not a flag." : $"no state or choice '{name}'; {about} holds {expected}.");
            return false;
        }

        /// <summary>
        /// What <paramref name="about" />, holding <paramref name="type" />, is set to or compared with where it is written with
        /// its kind: <c>state(home)</c>, <c>setting(home)</c> or <c>side(inside)</c>; or false and trouble where it is none of those.
        /// </summary>
        static bool Kinded(Statement statement, Value written, string type, string about, AutomationSystem system, ScriptTrouble trouble,
            out Literal value, out string other)
        {
            value = default(Literal);
            other = "";
            var state = Kinds.Named(statement, written, Kinds.State);
            if (state != null)
            {
                var named = system.FindState(state);
                if (named != null)
                    return Other(statement, named, type, about, trouble, out other);

                trouble.Say(statement, $"no state called '{state}'.");
                return false;
            }

            var setting = Kinds.Named(statement, written, Kinds.Setting);
            if (setting != null)
            {
                value = Literal.Setting(setting, true);
                return SettingReader.Owe(statement, setting, system, held => held == type, SettingReader.Holding(type), trouble);
            }

            if (type == SettingReader.Flag)
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, written)}; expected on or off or a state of the same type.");
                return false;
            }

            DeclaredType of;
            string choice;
            if (!Choice(statement, written, system, trouble, out of, out choice))
                return false;

            if (of.Name != type)
            {
                trouble.Say(statement, $"{ScriptQuotes.Spelled(statement, written)} is of type {of.Name}; expected a choice of {type}.");
                return false;
            }

            value = Literal.Name(choice);
            return true;
        }

        /// <summary>
        /// What <paramref name="about" />, holding the built-in <paramref name="type" />, an integer, a real, a text or a length of
        /// time, is set to or compared with: a value of the type, an integer for a real, or a setting holding one, into
        /// <paramref name="value" />, or a state of the type into <paramref name="other" />; or false and trouble where it is
        /// none of those.
        /// </summary>
        static bool BuiltInValued(Statement statement, Value written, string type, string about, AutomationSystem system,
            ScriptTrouble trouble, out Literal value, out string other)
        {
            value = default(Literal);
            other = "";
            Func<string, bool> suits = held => held == type || type == SettingReader.Real && held == SettingReader.Integer;
            if (written.Form == ValueForm.Kinded)
            {
                var stateName = Kinds.Named(statement, written, Kinds.State);
                if (stateName != null)
                {
                    var named = system.FindState(stateName);
                    if (named != null)
                        return Other(statement, named, type, about, trouble, out other);

                    trouble.Say(statement, $"no state called '{stateName}'.");
                    return false;
                }

                var settingName = Kinds.Named(statement, written, Kinds.Setting);
                if (settingName != null)
                {
                    value = Literal.Setting(settingName, true);
                    return SettingReader.Owe(statement, settingName, system, suits, SettingReader.Holding(type), trouble);
                }
            }
            else if (ScriptQuotes.Named(written) && written.Form != ValueForm.Text)
            {
                var name = written.Word;
                var meanings = new List<string>();
                var spellings = new List<string>();
                Kinds.Meanings(system, name, meanings, spellings);
                if (meanings.Count > 1)
                {
                    Kinds.Ambiguous(statement, name, meanings, spellings, trouble);
                    return false;
                }

                var named = system.FindState(name);
                if (named != null)
                    return Other(statement, named, type, about, trouble, out other);

                var part = system.FindPart(name);
                if (part != null && part.IsSetting)
                {
                    value = Literal.Setting(name);
                    return SettingReader.Owe(statement, name, system, suits, SettingReader.Holding(type), trouble);
                }

                trouble.Say(statement, $"no state or setting '{name}'; {about} holds {SettingReader.Holding(type)}.");
                return false;
            }

            bool said;
            if (SettingReader.Typed(statement, written, type, system, trouble, out said, out value))
                return true;

            if (!said)
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, written)}; {about} holds {SettingReader.Holding(type)}.");

            return false;
        }

        /// <summary>The state <paramref name="named" /> into <paramref name="other" />, where it holds <paramref name="type" />; or false and trouble where it holds another type than <paramref name="about" />.</summary>
        static bool Other(Statement statement, State named, string type, string about, ScriptTrouble trouble, out string other)
        {
            other = "";
            if ((named.IsFlag ? SettingReader.Flag : named.Type) != type)
            {
                trouble.Say(statement, $"state '{named.Name}' is of another type than {about}.");
                return false;
            }

            other = named.Name;
            return true;
        }

        /// <summary>
        /// The state <paramref name="statement" /> names at <paramref name="at" />, bare or as <c>state(where)</c>, or null and
        /// trouble where the system has none by that name, or a bare name could also be another thing's.
        /// </summary>
        public static State Named(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var written = statement.ValueAt(at);
            var name = written.Form == ValueForm.Kinded ? Kinds.Named(statement, written, Kinds.State) : Called(statement, written);
            if (name != null && written.Form != ValueForm.Kinded)
            {
                var meanings = new List<string>();
                var spellings = new List<string>();
                Kinds.Meanings(system, name, meanings, spellings);
                if (meanings.Count > 1)
                {
                    Kinds.Ambiguous(statement, name, meanings, spellings, trouble);
                    return null;
                }
            }

            var state = name == null ? null : system.FindState(name);
            if (state == null)
                trouble.Say(statement, $"no state {(name == null ? ScriptQuotes.Spelled(statement, at) : $"'{name}'")} declared above this line.");

            return state;
        }

        /// <summary>The name <paramref name="written" /> gives a state or a setting: a bare name, or one with its kind, <c>state(where)</c> or <c>setting(where)</c>; null where it is neither.</summary>
        public static string Called(Statement statement, Value written)
        {
            if (ScriptQuotes.Named(written))
                return written.Word;

            return Kinds.Named(statement, written, Kinds.State) ?? Kinds.Named(statement, written, Kinds.Setting);
        }

        /// <summary>
        /// <c>where == inside</c> or <c>'Outer light' == on</c> from <paramref name="at" />: a state or a setting compared, equal
        /// or not, with a value, a state or a setting; or null and trouble where it is not that.
        /// </summary>
        public static StateTest Test(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var written = statement.ValueAt(at);
            if (ScriptQuotes.Named(written))
            {
                var meanings = new List<string>();
                var spellings = new List<string>();
                Kinds.Meanings(system, written.Word, meanings, spellings);
                if (meanings.Count > 1)
                {
                    Kinds.Ambiguous(statement, written.Word, meanings, spellings, trouble);
                    return null;
                }
            }

            var setting = SettingReader.Referenced(statement, written, system);
            if (setting != null)
                return SettingReader.Test(statement, at, setting, system, trouble);

            var state = Named(statement, at, system, trouble);
            if (state == null)
                return null;

            Relation relation;
            var related = StepReader.Related(statement.ValueAt(at + 1), out relation);
            var ordering = relation != Relation.Is && relation != Relation.IsNot;
            if (!related || ordering && !state.IsOrdered)
            {
                trouble.Say(statement, state.IsOrdered
                    ? "expected ==, !=, <, <=, > or >= after the state."
                    : $"expected == or != after the state; state '{state.Name}' holds {Holding(state)}, which has no order.");
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
                Relation = relation,
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

            if (ExpressionReader.Is(statement.ValueAt(2)))
            {
                Literal worked;
                return ExpressionReader.Read(statement, statement.ValueAt(2), system, trouble, out worked)
                    ? new SetState { State = state.Name, Expression = worked.Tree }
                    : null;
            }

            Literal value;
            string other;
            if (!Valued(statement, 2, state, system, trouble, out value, out other))
                return null;

            return new SetState { State = state.Name, Value = value, From = other };
        }
    }
}
