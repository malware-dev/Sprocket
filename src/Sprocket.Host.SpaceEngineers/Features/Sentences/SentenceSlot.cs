using System.Collections.Generic;
using Lumel.Input;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
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
        /// <summary>A part pressed by <paramref name="opens" />, or one that cannot be pressed for null.</summary>
        protected SentenceSlot(ICommand opens)
        {
            Opens = opens;
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

        /// <summary>What pressing this part does, or null where it cannot be pressed.</summary>
        public ICommand Opens { get; }

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
    }
}
