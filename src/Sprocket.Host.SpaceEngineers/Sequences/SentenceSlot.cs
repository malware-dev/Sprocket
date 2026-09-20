using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Sequences
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

        /// <summary>
        /// A colour this slot stands for, where it stands for one, to be shown instead of words.
        /// </summary>
        /// <remarks>
        /// The one exception to a slot never saying how it reads, and it is an exception because it
        /// is not words: a colour is the same thing in every language, and spelling it out as three
        /// numbers is a worse answer in all of them. Nothing else here has a rendering that is not
        /// text, which is why this is one property rather than a way of saying anything at all.
        /// </remarks>
        public virtual Color? Sample
        {
            get { return null; }
        }

        public int Command { get; private set; }

        /// <summary>
        /// What pressing it asks for, or empty when pressing it opens a list instead.
        /// </summary>
        /// <remarks>
        /// A slot whose contents are typed rather than chosen from something. The field hangs under
        /// the chip that was pressed, which is where somebody is already looking, and what they
        /// typed goes to the same command.
        /// </remarks>
        public string Question { get; set; }

        public bool Asks => !string.IsNullOrEmpty(Question);

        /// <summary>
        /// What each value it asks for wants, one per value, or null where it asks for none.
        /// </summary>
        /// <remarks>
        /// One field each, named and bounded where anybody has said what the value is - which only
        /// a description can say, an action declaring how many values it takes and nothing else.
        /// </remarks>
        public virtual IList<Field> Fields => null;

        /// <summary>
        /// What those fields open holding, or null for empty ones.
        /// </summary>
        /// <remarks>
        /// What is already in the slot. Changing one value of three should not mean typing all three
        /// again, and a form that opened empty over values somebody had given would read as having
        /// thrown them away.
        /// </remarks>
        public virtual IList<string> Filled => null;

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
