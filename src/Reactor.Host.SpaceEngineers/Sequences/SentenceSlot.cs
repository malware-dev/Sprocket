namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A fillable part of a sentence: what it refers to, and how to change it.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means. A target must render these as
    /// unmistakably fillable, and must not give anything else the same look.
    /// <para>
    /// A slot says what it refers to and never what it reads as. The words are the target's, because
    /// the words are the only part of a sentence that has a language - and a slot that carried its own
    /// text would carry English into every target that ever renders one.
    /// </para>
    /// </remarks>
    public abstract class SentenceSlot
    {
        protected SentenceSlot(int command)
        {
            Command = command;
        }

        /// <summary>Nothing chosen yet, so it should read as an invitation.</summary>
        public abstract bool IsEmpty { get; }

        public int Command { get; private set; }
    }
}
