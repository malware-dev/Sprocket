namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// The script a program is written out as and read back from: its version and the words it is spelled with.
    /// </summary>
    public static class ScriptFormat
    {
        /// <summary>The version of the format this build writes, the number after <c>sprocket</c> on a script's first line.</summary>
        public const int Version = 1;

        /// <summary>The draft of the format's version this build writes, the number after <c>alpha</c> on a script's first line.</summary>
        public const int Draft = 25;

        /// <summary>The function this host names a signal with: a slot on a block's toolbar.</summary>
        /// <remarks>Persisted in every program with a trigger.</remarks>
        public const string Toolbar = "toolbar";

        /// <summary>The pattern the time a logic system last changed is written in.</summary>
        public const string Time = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'";
    }
}
