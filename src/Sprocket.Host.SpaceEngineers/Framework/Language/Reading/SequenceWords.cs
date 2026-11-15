namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// The words a sequence's body may end with, and the words its head may carry after its name, read as words where they
    /// stand rather than as keywords, so a program naming something <c>exclusive</c> or <c>catch</c> elsewhere reads as it always did.
    /// </summary>
    /// <remarks>Persisted: every save spells a sequence's head and the end of its body with these words.</remarks>
    public static class SequenceWords
    {
        /// <summary>The word before the id of the sequences a sequence runs one at a time with.</summary>
        public const string Exclusive = "exclusive";

        /// <summary>The word the part of a sequence run where it fails starts with.</summary>
        public const string Catch = "catch";

        /// <summary>The word the part of a sequence run however it ends starts with.</summary>
        public const string Finally = "finally";

        /// <summary>Whether <paramref name="statement" /> starts a <c>catch</c> or a <c>finally</c>.</summary>
        public static bool Handles(Statement statement)
        {
            return statement.Key == Keyword.None && (statement.Head == Catch || statement.Head == Finally);
        }
    }
}
