namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format
{
    /// <summary>
    /// The script a program is written out as and read back from: its version and the words it is spelled with.
    /// </summary>
    public static class ScriptFormat
    {
        /// <summary>The version of the format this build writes, the number after <c>sprocket</c> on a script's first line.</summary>
        public const int Version = 3;

        /// <summary>
        /// The draft of the format's version this build writes, the number after <c>alpha</c> on a script's first line, or
        /// <see cref="Finished" /> where it writes the finished version.
        /// </summary>
        public const int Draft = 3;

        /// <summary>The draft a finished version is read as: later than every draft of it, and written with no <c>alpha</c>.</summary>
        public const int Finished = int.MaxValue;

        /// <summary>The name a script's first line starts with, unquoted, before its version: <c>sprocket 2 alpha 1</c>.</summary>
        /// <remarks>Persisted in every program.</remarks>
        public const string Header = "sprocket";

        /// <summary>Whether this build writes a draft rather than the finished version.</summary>
        /// <remarks>Read only, not const, so code for both cases compiles whichever this build is.</remarks>
        public static readonly bool IsDraft = Draft != Finished;

        /// <summary>The first line this build writes, such as <c>sprocket 2</c>.</summary>
        public static readonly string Line = Spelled(Version, Draft);

        /// <summary>Returns version <paramref name="version" /> as a first line spells it, with <paramref name="draft" /> where it is unfinished.</summary>
        public static string Spelled(int version, int draft) =>
            draft == Finished ? $"{Header} {version}" : $"{Header} {version} alpha {draft}";

        /// <summary>The function this host names a signal with: a slot on a block's toolbar.</summary>
        /// <remarks>Persisted in every program with a trigger.</remarks>
        public const string Toolbar = "toolbar";

        /// <summary>The pattern the time a logic system last changed is written in.</summary>
        public const string Time = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'";
    }
}
