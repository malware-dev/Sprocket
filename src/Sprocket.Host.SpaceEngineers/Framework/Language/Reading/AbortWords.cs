using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// The words of an <c>abort</c> step, read as words where they stand rather than as keywords, so a program naming
    /// something <c>warning</c> or <c>note</c> elsewhere reads as it always did.
    /// </summary>
    /// <remarks>Persisted: every save spells an abort with these words.</remarks>
    public static class AbortWords
    {
        /// <summary>The word an abort step starts with.</summary>
        public const string Abort = "abort";

        /// <summary>The word before an abort's message.</summary>
        public const string Saying = "saying";

        static readonly string[] Levels = { "note", "warning", "problem" };

        /// <summary>How <paramref name="level" /> is spelled.</summary>
        public static string Name(AbortLevel level)
        {
            return Levels[(int)level];
        }

        /// <summary>The level <paramref name="word" /> spells, and whether it spells one.</summary>
        public static bool Level(string word, out AbortLevel level)
        {
            for (var i = 0; i < Levels.Length; i++)
            {
                if (Levels[i] != word)
                    continue;

                level = (AbortLevel)i;
                return true;
            }

            level = AbortLevel.Note;
            return false;
        }
    }
}
