using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>The kinds the language names, which a thing is written with where alone it could mean more than one thing: <c>sequence(cycle)</c>.</summary>
    /// <remarks>Persisted in every program that writes a thing with its kind.</remarks>
    public static class Kinds
    {
        // The built-in types, each a kind a literal may be written with.
        static readonly string[] BuiltIn = { "flag", "text", "integer", "real", "duration", "color", "vector", "list" };

        /// <summary>A system blueprint's settings: <c>setting('Open angle')</c>.</summary>
        public const string Setting = "setting";

        /// <summary>A logic system's states: <c>state(where)</c>.</summary>
        public const string State = "state";

        /// <summary>A game's type of block, as a slot expects it: <c>block_type(Door)</c>.</summary>
        public const string BlockType = "block_type";

        /// <summary>The name <paramref name="written" /> gives a thing of the kind <paramref name="kind" />, <c>where</c> in <c>state(where)</c>; null where it is not written so.</summary>
        public static string Named(Statement statement, Value written, string kind)
        {
            if (written.Form != ValueForm.Kinded)
                return null;

            var of = statement.KindOf(written);
            var thing = statement.ThingOf(written);
            return of.IsWord && of.Word == kind && ScriptQuotes.Named(thing) ? thing.Word : null;
        }

        /// <summary>
        /// Adds to <paramref name="meanings" /> each thing of <paramref name="system" /> called <paramref name="name" />, in
        /// words, and to <paramref name="spellings" /> how it is written with its kind: its slot or setting, type, state and sequences.
        /// </summary>
        public static void Meanings(AutomationSystem system, string name, IList<string> meanings, IList<string> spellings)
        {
            var part = system.FindPart(name);
            if (part != null)
                Add(part.IsSetting ? Setting : Keywords.Name(Keyword.Slot), name, meanings, spellings);

            if (system.FindType(name) != null)
                Add(Keywords.Name(Keyword.Type), name, meanings, spellings);

            if (system.FindState(name) != null)
                Add(State, name, meanings, spellings);

            foreach (var sequence in system.Sequences)
            {
                if (string.Equals(sequence.Name, name, StringComparison.Ordinal))
                {
                    Add(Keywords.Name(Keyword.Sequence), name, meanings, spellings);
                    break;
                }
            }
        }

        /// <summary>
        /// Says that <paramref name="name" /> could mean each of the things <paramref name="meanings" /> says, where
        /// <paramref name="statement" /> writes it, and how to write each with its kind, <paramref name="spellings" />.
        /// </summary>
        public static void Ambiguous(Statement statement, string name, IList<string> meanings, IList<string> spellings, ScriptTrouble trouble)
        {
            trouble.Say(statement, $"'{name}' could be {string.Join(" or ", meanings)}; write the one meant with its kind: {string.Join(" or ", spellings)}.");
        }

        /// <summary>Adds the thing of the kind <paramref name="kind" /> called <paramref name="name" /> to <paramref name="meanings" />, in words, and to <paramref name="spellings" />, written with its kind.</summary>
        public static void Add(string kind, string name, IList<string> meanings, IList<string> spellings)
        {
            meanings.Add($"the {kind} '{name}'");
            spellings.Add(ScriptWriter.KindSpelling(kind, name));
        }

        /// <summary>
        /// <paramref name="written" /> without a built-in kind it is written with, <c>4</c> for <c>real(4)</c>; false and trouble
        /// where what it is written with is never of that kind, as in <c>text(4)</c>.
        /// </summary>
        /// <remarks>Anything else is left as it is written.</remarks>
        public static bool Literal(Statement statement, ref Value written, ScriptTrouble trouble)
        {
            if (written.Form != ValueForm.Kinded)
                return true;

            var kind = statement.KindOf(written);
            if (!kind.IsWord || Array.IndexOf(BuiltIn, kind.Word) < 0)
                return true;

            var thing = statement.ThingOf(written);
            if (!Fits(kind.Word, thing))
            {
                trouble.Say(statement, $"unexpected {ScriptQuotes.Spelled(statement, written)}; {ScriptQuotes.Spelled(statement, thing)} is not written as a value of {kind.Word}.");
                return false;
            }

            written = thing;
            return true;
        }

        /// <summary>Whether <paramref name="thing" /> is written as a value of the built-in type <paramref name="kind" /> can be, as it would stand alone.</summary>
        static bool Fits(string kind, Value thing)
        {
            switch (kind)
            {
                case "flag": return thing.Says(Keyword.On) || thing.Says(Keyword.Off);
                case "text": return thing.Form == ValueForm.Text;
                case "integer": return thing.Form == ValueForm.Integer;
                case "real": return thing.Form == ValueForm.Integer || thing.Form == ValueForm.Real;
                case "duration": return thing.Form == ValueForm.Duration;
                case "color": return thing.Form == ValueForm.Color;
                case "vector": return thing.Form == ValueForm.Vector;
                case "list": return thing.Form == ValueForm.List;
            }

            return false;
        }

        /// <summary>Whether <paramref name="keyword" /> is a kind as well as a keyword: <c>type</c>, <c>slot</c> or <c>sequence</c>.</summary>
        public static bool IsKeyword(Keyword keyword)
        {
            return keyword == Keyword.Type || keyword == Keyword.Slot || keyword == Keyword.Sequence;
        }
    }
}
