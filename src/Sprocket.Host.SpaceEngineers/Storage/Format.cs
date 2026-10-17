using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// The script a program is written out as and read back from: its version and the words it is spelled with.
    /// </summary>
    /// <remarks>
    /// Space Engineers does not whitelist IO, and a mod stores its data in the save. So a program is turned into a string
    /// and back, by <see cref="Scribe" /> and <see cref="Reader" />, and whoever holds the string decides where it lives.
    /// <para>
    /// <b>The script is the program.</b> There is nothing else to project it from - the model lives in
    /// memory in a mod the game may unload at any moment - so this is not a view of the truth, it is
    /// the truth written down. Two things follow and both are load-bearing: it stays readable so a save
    /// that breaks can be looked at and mended, and a text with anything wrong in it is read as nothing,
    /// kept whole as it was written and marked <see cref="Automation.Intact" /> false so that the next
    /// save cannot write over it.
    /// </para>
    /// <para>
    /// It is a language and not prose, and that is the line to hold: keywords here are syntax, the way
    /// <c>if</c> and <c>while</c> are, and nobody localises those. The moment it drifts towards English
    /// it puts word order and vocabulary into the model and freezes them in a file format, where they
    /// become a compatibility promise. Every word a person reads is the host's; every word in here is
    /// the format's.
    /// </para>
    /// </remarks>
    public static class Format
    {
        /// <summary>The version of the format this build writes, the number after <c>sprocket</c> on a script's first line.</summary>
        public const int Version = 1;

        /// <summary>The draft of the format's version this build writes, the number after <c>alpha</c> on a script's first line.</summary>
        public const int Draft = 16;

        /// <summary>The function this host names a signal with: a slot on a block's toolbar.</summary>
        /// <remarks>Persisted in every program with a trigger, and written by <see cref="Pen.Signal" />.</remarks>
        public const string Toolbar = "toolbar";

        /// <summary>The pattern the time a logic system last changed is written in.</summary>
        public const string Time = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'";
    }
}
