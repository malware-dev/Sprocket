
namespace Sprocket.Host.SpaceEngineers.Framework.Text
{
    /// <summary>One text a program says: its key, and its words in the language it was written in.</summary>
    /// <remarks>Written by the text generator from the file the texts are defined in, one for each key.</remarks>
    public sealed class Phrase
    {
        /// <summary>The text keyed <paramref name="key" />, <paramref name="index" /> in its list, reading <paramref name="written" />.</summary>
        public Phrase(int index, string key, string written)
        {
            Index = index;
            Key = key;
            Written = written;
        }

        /// <summary>Where this is in the list of texts it was written with.</summary>
        public int Index { get; }

        /// <summary>The key the text is found by.</summary>
        public string Key { get; }

        /// <summary>The words as written.</summary>
        public string Written { get; }
    }
}
