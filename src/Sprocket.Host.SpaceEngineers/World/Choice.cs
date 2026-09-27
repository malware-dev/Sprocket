
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One of the things a value may be: what it is called, and what the program holds for it.
    /// </summary>
    /// <remarks>
    /// Two different things, and that is the whole point of this type. The terminal shows a charge
    /// mode as Auto, Recharge or Discharge and the block is told a number, so a form that wrote down
    /// what somebody read would be writing something the block will not take.
    /// <para>
    /// Both are text, because what the program holds is text by the time it gets here: the number is
    /// the block's business and the spelling of it is the language's.
    /// </para>
    /// </remarks>
    public struct Choice
    {
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
