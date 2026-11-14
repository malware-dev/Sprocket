
namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// One of the things a value may be: what it is called, and what the program holds for it.
    /// </summary>
    public struct Choice
    {
        /// <summary>The choice said as <paramref name="word" />, meaning <paramref name="means" />.</summary>
        public Choice(string word, string means) : this()
        {
            Word = word ?? "";
            Means = means ?? "";
        }

        /// <summary>What the player has already read on the terminal screen.</summary>
        public string Word { get; private set; }

        /// <summary>What a program holds to mean it.</summary>
        public string Means { get; private set; }
    }
}
