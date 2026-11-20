using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>How the script spells what a statement says, for the trouble that names it.</summary>
    public static class ScriptQuotes
    {
        /// <summary>Whether <paramref name="statement" /> says nothing after its first <paramref name="count" /> values, and trouble naming the first where it does.</summary>
        public static bool Ends(Statement statement, int count, ScriptTrouble trouble)
        {
            if (statement.Count <= count)
                return true;

            trouble.Say(statement, $"unexpected {Spelled(statement, count)}.");
            return false;
        }

        /// <summary>The word a statement starts with, as it is written, for trouble to name it.</summary>
        public static string Headed(Statement statement)
        {
            return $"'{(statement.Key == Keyword.None ? statement.Head : Keywords.Name(statement.Key))}'";
        }

        /// <summary>The value at <paramref name="index" /> of <paramref name="statement" /> as it is written, or what it is, for trouble to name it.</summary>
        public static string Spelled(Statement statement, int index)
        {
            var value = statement.ValueAt(index);
            if (value.Form == ValueForm.Qualified)
                return $"'{Spelling(statement.Parts(index))}'";

            return Spelled(statement, value);
        }

        /// <summary><paramref name="value" /> as it is written, or what it is, for trouble to name it.</summary>
        public static string Spelled(Statement statement, Value value)
        {
            if (value.Form == ValueForm.Reference)
                return $"'{Spelling(statement.Member(value))}'";

            if (value.Form == ValueForm.Qualified)
                return $"'{Spelling(statement.PartsOf(value))}'";

            if (value.Form == ValueForm.Kinded)
                return $"'{Kinded(statement, value)}'";

            if (value.Form == ValueForm.Real)
                return value.Real.ToString(CultureInfo.InvariantCulture);

            if (value.Form == ValueForm.Color)
                return "a color";

            if (value.Form == ValueForm.List)
                return "'['";

            if (value.Form == ValueForm.Vector)
                return "'('";


            if (value.Form == ValueForm.Keyword)
                return $"'{Keywords.Name(value.Keyword)}'";

            if (value.IsWord)
                return $"'{value.Word}'";

            if (value.Form == ValueForm.Text)
                return $"\"{value.Word}\"";

            if (value.Form == ValueForm.Integer)
                return value.Integer.ToString(CultureInfo.InvariantCulture);

            if (value.Form == ValueForm.Duration && value.Word == Parser.Seconds)
                return $"{value.Real.ToString("R", CultureInfo.InvariantCulture)}s";

            if (value.Form == ValueForm.Duration)
                return $"{value.Integer.ToString(CultureInfo.InvariantCulture)}t";

            return "what follows";
        }

        /// <summary>A thing with its kind as it is written, <c>side(inside)</c>, its kind and thing spelled as trouble names them.</summary>
        static string Kinded(Statement statement, Value value)
        {
            return $"{Bare(statement, statement.KindOf(value))}({Bare(statement, statement.ThingOf(value))})";
        }

        /// <summary>A value as <see cref="Spelled(Statement, Value)" /> spells it, without the quotes around a name.</summary>
        static string Bare(Statement statement, Value value)
        {
            if (value.Form == ValueForm.Kinded)
                return Kinded(statement, value);

            if (value.Form == ValueForm.Qualified)
                return Spelling(statement.PartsOf(value));

            return value.IsWord ? value.Word : Spelled(statement, value);
        }

        /// <summary>A name's parts joined as they are written: <c>sprocket.closed</c>.</summary>
        public static string Spelling(ValueRange parts)
        {
            var joined = "";
            for (var i = 0; i < parts.Count; i++)
                joined += (i > 0 ? "." : "") + parts[i].Word;

            return joined;
        }

        /// <summary>The namespace of a qualified name: every part before the last, joined as written, or empty where it has one part.</summary>
        public static string Namespace(ValueRange parts)
        {
            var joined = "";
            for (var i = 0; i < parts.Count - 1; i++)
                joined += (i > 0 ? "." : "") + parts[i].Word;

            return joined;
        }

        /// <summary>Whether every part of a name has a word, so it is a name of one part or more with nothing left out.</summary>
        public static bool Whole(ValueRange parts)
        {
            if (parts.Count == 0)
                return false;

            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i].Word.Length == 0)
                    return false;
            }

            return true;
        }

        /// <summary>Whether <paramref name="value" /> is a name, bare or quoted, rather than one of the language's reserved words or anything else.</summary>
        /// <remarks>A bare name spelled like a contextual keyword is one; the construct that reads that keyword asks for it first.</remarks>
        public static bool Named(Value value)
        {
            return value.Form == ValueForm.Identifier && value.Word.Length > 0;
        }
    }
}
