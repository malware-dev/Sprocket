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

        /// <summary>
        /// Empties this part, where emptying it is a thing to offer. Zero where it is not.
        /// </summary>
        /// <remarks>
        /// Pressing a slot changes what is in it; this takes what is in it away, and the two are
        /// different intentions. A trigger that runs a sequence is asked "which one" when its slot is
        /// pressed, because that is what somebody changing it almost always means - and being asked
        /// the whole question of what a trigger is for, every time, to change one door is three
        /// answers where one would do. Clearing it is how they ask for that whole question back.
        /// <para>
        /// The slot, not the things named in it. Taking one block out of three is an answer to the
        /// picker, which is where that choice was made.
        /// </para>
        /// </remarks>
        public int ClearCommand { get; set; }
    }
}
