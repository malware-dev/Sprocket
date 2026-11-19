using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// Reads a system blueprint's settings: the slots answered by one value, their defaults, the values instances answer them
    /// with, and every setting standing in for a value, by its name or with its kind, <c>setting('Open angle')</c>.
    /// </summary>
    public static class SettingReader
    {
        /// <summary>The built-in type a setting holds a flag of.</summary>
        public const string Flag = "flag";

        /// <summary>The built-in type a setting holds a length of time of.</summary>
        public const string Duration = "duration";

        const string Integer = "integer";

        const string Real = "real";

        const string Text = "text";

        const string Color = "color";

        // The built-in types a setting may hold, besides the types a system declares.
        static readonly string[] BuiltIn = { Flag, Text, Integer, Real, Duration, Color };

        /// <summary>Whether a setting of a type holds a number: an integer or a real.</summary>
        public static readonly Func<string, bool> Numeric = type => type == Integer || type == Real;

        /// <summary>Whether a setting of a type holds a length of time.</summary>
        public static readonly Func<string, bool> Lasting = type => type == Duration;

        /// <summary>Whether a setting of a type holds text.</summary>
        public static readonly Func<string, bool> Worded = type => type == Text;

        /// <summary>Whether <paramref name="type" /> is one of the built-in types a setting may hold.</summary>
        public static bool IsBuiltIn(string type)
        {
            return Array.IndexOf(BuiltIn, type) >= 0;
        }

        /// <summary>What a setting of <paramref name="type" /> holds, in words for trouble: <c>a real</c>, <c>a choice of side</c>.</summary>
        public static string Holding(string type)
        {
            switch (type)
            {
                case Flag: return "a flag";
                case Text: return "text";
                case Integer: return "an integer";
                case Real: return "a real";
                case Duration: return "a duration";
                case Color: return "a color";
            }

            return $"a choice of {type}";
        }

        /// <summary>
        /// The setting <paramref name="written" /> names: <c>setting('Open angle')</c>, or a bare name of one of
        /// <paramref name="system" />'s settings; null where it names none.
        /// </summary>
        public static string Referenced(Statement statement, Value written, AutomationSystem system)
        {
            if (written.Form == ValueForm.Kinded)
                return Kinds.Named(statement, written, Kinds.Setting);

            if (!ScriptQuotes.Named(written))
                return null;

            var part = system.FindPart(written.Word);
            return part != null && part.IsSetting ? written.Word : null;
        }

        /// <summary>
        /// Reads the default of <paramref name="part" />, a setting read from <paramref name="statement" />; trouble where a
        /// default is given to a slot for blocks, or the default is missing, unwanted or not of its type.
        /// </summary>
        /// <remarks>Once the whole system is read, as its types may be written below its slots.</remarks>
        public static void Slot(Statement statement, Part part, AutomationSystem system, ScriptTrouble trouble)
        {
            var defaulted = statement.Says(2, Keyword.Expects) && statement.Says(4, Keyword.Equals);
            if (!part.IsSetting)
            {
                if (defaulted)
                    trouble.Say(statement, $"unexpected '='; '{part.Id}' expects blocks, and only a setting takes a default.");

                return;
            }

            var type = part.Setting;
            if (!defaulted)
            {
                if (part.Optional)
                    trouble.Say(statement, $"optional setting '{part.Id}' needs a default after '=', a value of its type: {Holding(type)}.");

                return;
            }

            if (!part.Optional)
            {
                trouble.Say(statement, $"required setting '{part.Id}' takes no default; every instance answers it.");
                return;
            }

            if (statement.Count < 6)
            {
                trouble.Say(statement, $"expected the default after '=', {Holding(type)}.");
                return;
            }

            if (!ScriptQuotes.Ends(statement, 6, trouble))
                return;

            Literal value;
            if (!Typed(statement, 5, part, system, trouble, out value))
                return;

            part.Default = value;
            part.HasDefault = true;
        }

        /// <summary><c>'Open angle' = 85</c>: the value an instance answers <paramref name="part" />, a setting, with; or trouble where it is not one of its type.</summary>
        public static void Answer(Statement statement, Part part, AutomationSystem system, SystemInstance instance, ScriptTrouble trouble)
        {
            if (statement.ValueAt(1).Form == ValueForm.List)
            {
                trouble.Say(statement, $"setting '{part.Id}' is answered with one value, not a list, like '{part.Id}' = <value>.");
                return;
            }

            if (!ScriptQuotes.Ends(statement, 2, trouble))
                return;

            Literal value;
            if (!Typed(statement, 1, part, system, trouble, out value))
                return;

            instance.Answer(part.Id, value);
        }

        /// <summary>
        /// The value at <paramref name="at" /> as one of the type <paramref name="part" />, a setting, holds, a whole number
        /// standing for a real one; or false and trouble where it is none.
        /// </summary>
        /// <remarks>A choice of a declared type is written bare, or with its type as a kind, <c>side(inside)</c>.</remarks>
        public static bool Typed(Statement statement, int at, Part part, AutomationSystem system, ScriptTrouble trouble, out Literal value)
        {
            bool said;
            if (Typed(statement, statement.ValueAt(at), part.Setting, system, trouble, out said, out value))
                return true;

            if (!said)
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, at)}; setting '{part.Id}' holds {Holding(part.Setting)}.");

            return false;
        }

        /// <summary>
        /// <paramref name="written" /> as one of <paramref name="type" />; or false where it is none, with
        /// <paramref name="said" /> true where trouble already says why.
        /// </summary>
        static bool Typed(Statement statement, Value written, string type, AutomationSystem system, ScriptTrouble trouble, out bool said,
            out Literal value)
        {
            said = false;
            value = default(Literal);
            if (written.Form == ValueForm.Kinded && statement.KindOf(written).IsWord && IsBuiltIn(statement.KindOf(written).Word))
            {
                // A built-in kind only says what the thing already is: an integer stands for a real, as written alone.
                var kind = statement.KindOf(written).Word;
                if (kind != type && !(kind == Integer && type == Real))
                    return false;

                var thing = statement.ThingOf(written);
                Literal checkedAs;
                return Typed(statement, thing, kind, system, trouble, out said, out checkedAs)
                    && Typed(statement, thing, type, system, trouble, out said, out value);
            }

            switch (type)
            {
                case Flag:
                    bool on;
                    if (!Kinds.Spells(statement, written, out on))
                        return false;

                    value = Literal.Of(on);
                    return true;

                case Text:
                    if (written.Form != ValueForm.Text)
                        return false;

                    value = Literal.Of(written.Word);
                    return true;

                case Integer:
                    if (written.Form != ValueForm.Integer)
                        return false;

                    value = Literal.Of(written.Whole);
                    return true;

                case Real:
                    if (written.Form != ValueForm.Integer && written.Form != ValueForm.Real)
                        return false;

                    value = Literal.Of(written.Number());
                    return true;

                case Duration:
                    Duration length;
                    if (!StepReader.Lasting(written, 0, out length))
                        return false;

                    value = Literal.Of(length);
                    return true;

                case Color:
                    if (written.Form != ValueForm.Color)
                        return false;

                    byte r, g, b, a;
                    written.Color(out r, out g, out b, out a);
                    value = Literal.Color(r, g, b, a);
                    return true;
            }

            var declared = system.FindType(type);
            if (declared == null)
                return false;

            string choice = null;
            if (written.Form == ValueForm.Kinded)
            {
                DeclaredType of;
                if (!StateReader.Choice(statement, written, system, trouble, out of, out choice))
                {
                    said = true;
                    return false;
                }

                if (of != declared)
                    return false;
            }
            else if (ScriptQuotes.Named(written))
                choice = written.Word;

            if (choice == null || !declared.Has(choice))
                return false;

            value = Literal.Name(choice);
            return true;
        }

        /// <summary>
        /// Says what is wrong with the setting <paramref name="name" /> standing where <paramref name="statement" /> has it: no
        /// such setting in <paramref name="system" />, or one whose type <paramref name="suits" /> does not take, which
        /// <paramref name="wanted" /> says in words; any setting suits where it is null.
        /// </summary>
        /// <returns>Whether nothing is wrong with it.</returns>
        public static bool Owe(Statement statement, string name, AutomationSystem system, Func<string, bool> suits, string wanted,
            ScriptTrouble trouble)
        {
            var part = Named(statement, name, system, trouble);
            if (part == null)
                return false;

            if (suits == null || suits(part.Setting))
                return true;

            trouble.Say(statement, $"setting '{name}' holds {Holding(part.Setting)}; expected {wanted} here.");
            return false;
        }

        /// <summary>
        /// <c>if 'Outer light' == on then</c>, from <paramref name="at" />, where it names <paramref name="name" />, a setting:
        /// the setting compared, equal or not, with a value of its type, a state of its type, or another setting of its type;
        /// or null and trouble where it is not that.
        /// </summary>
        public static StateTest Test(Statement statement, int at, string name, AutomationSystem system, ScriptTrouble trouble)
        {
            var part = Named(statement, name, system, trouble);
            if (part == null)
                return null;

            var relation = statement.ValueAt(at + 1);
            if (!relation.Says(Keyword.EqualTo) && !relation.Says(Keyword.NotEqualTo))
            {
                trouble.Say(statement, "expected == or != after the setting.");
                return null;
            }

            if (statement.Count <= at + 2)
            {
                trouble.Say(statement, "expected a value after the comparison.");
                return null;
            }

            var test = new StateTest { Setting = name, Relation = relation.Says(Keyword.EqualTo) ? Relation.Is : Relation.IsNot };
            Literal value;
            string other;
            if (part.Setting == Flag || system.FindType(part.Setting) != null)
            {
                if (!StateReader.Valued(statement, at + 2, part.Setting, $"setting '{name}'", system, trouble, out value, out other))
                    return null;

                test.Value = value;
                test.Other = other;
                return test;
            }

            var written = statement.ValueAt(at + 2);
            var compared = Referenced(statement, written, system);
            if (compared != null)
            {
                if (!Owe(statement, compared, system, type => type == part.Setting, Holding(part.Setting), trouble))
                    return null;

                test.Value = Literal.Setting(compared);
                return test;
            }

            if (!Typed(statement, at + 2, part, system, trouble, out value))
                return null;

            test.Value = value;
            return test;
        }

        /// <summary>The setting <paramref name="name" /> of <paramref name="system" />, or null and trouble saying it has none by that name.</summary>
        static Part Named(Statement statement, string name, AutomationSystem system, ScriptTrouble trouble)
        {
            if (!system.IsBlueprint)
            {
                trouble.Say(statement, $"no setting called '{name}'; only a system blueprint has settings.");
                return null;
            }

            var part = system.FindPart(name);
            if (part == null)
                trouble.Say(statement, $"no setting called '{name}'.");
            else if (part.IsSignal)
                trouble.Say(statement, $"'{name}' is a signal slot; expected a setting.");
            else if (!part.IsSetting)
                trouble.Say(statement, $"'{name}' is a slot for blocks; expected a setting.");
            else
                return part;

            return null;
        }
    }
}
