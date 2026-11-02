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
    /// A target must render these as unmistakably fillable, and give nothing else the same look.
    /// </remarks>
    public abstract class SentenceSlot
    {
        /// <summary>A part pressed by <paramref name="opens" />, or one that cannot be pressed for null.</summary>
        protected SentenceSlot(ICommand opens)
        {
            Opens = opens;
        }

        /// <summary>Whether nothing is chosen yet, which a target shows as an invitation.</summary>
        public abstract bool IsEmpty { get; }

        /// <summary>
        /// A color this slot stands for, where it stands for one, to be shown instead of words.
        /// </summary>
        public virtual Color? Sample => null;

        /// <summary>What pressing this part does, or null where it cannot be pressed.</summary>
        public ICommand Opens { get; }

        /// <summary>The word joining this part to the one before it, where what laid out its sentence gave one; empty otherwise.</summary>
        public string Joined { get; set; } = "";

        /// <summary>
        /// What each value it asks for wants, one per value, or null where it asks for none.
        /// </summary>
        public virtual IList<Field> Fields => null;

        /// <summary>
        /// What those fields open holding, or null for empty ones.
        /// </summary>
        public virtual IList<string> Filled => null;
    }
}
