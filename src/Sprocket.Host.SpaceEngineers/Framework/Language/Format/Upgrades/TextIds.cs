using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>
    /// Version 2 alpha 1: a speaker's selected sound and the app a screen runs are text, the game's id for each, as a font is.
    /// A program written before it compares them with, or sets them to, a bare word, read as that text:
    /// <c>sprocket.selected_sound on [ horn ] == SoundBlockAlert1</c> becomes <c>... == "SoundBlockAlert1"</c>.
    /// </summary>
    public sealed class TextIds : FormatChange
    {
        // The values that became text, by their ids.
        static readonly string[] Values = { Speaker.Selected, ScreenType.App };

        /// <inheritdoc />
        public override int Arrived => 1;

        /// <inheritdoc />
        public override void Statements(List<Statement> constructs)
        {
            foreach (var construct in constructs)
                Upgraded(construct);
        }

        static void Upgraded(Statement statement)
        {
            for (var at = 0; at + 2 < statement.Count; at++)
            {
                var between = statement.ValueAt(at + 1);
                var given = between.Says(Keyword.To) || between.Says(Keyword.EqualTo) || between.Says(Keyword.NotEqualTo);
                var word = statement.ValueAt(at + 2);
                if (given && IsText(statement, statement.ValueAt(at)) && ScriptQuotes.Named(word))
                    statement.Put(at + 2, new Value(ValueForm.Text, word.Word));
            }

            foreach (var inner in statement.Body)
                Upgraded(inner);
        }

        /// <summary>Whether <paramref name="value" /> is one of the values that became text, bare or in Sprocket's namespace.</summary>
        static bool IsText(Statement statement, Value value)
        {
            if (value.Form != ValueForm.Reference)
                return false;

            var member = statement.Member(value);
            var own = member.Count == 1 || member.Count == 2 && member[0].Word == BlockKinds.Own;
            return own && Array.IndexOf(Values, member[member.Count - 1].Word) >= 0;
        }
    }
}
