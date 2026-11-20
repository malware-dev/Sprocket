using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// One numbered line of a sequence.
    /// </summary>
    public abstract class Step
    {
        static readonly string[] Nothing = new string[0];
        static readonly Literal[] NoArguments = new Literal[0];

        /// <summary>A step with nothing written about it yet.</summary>
        protected Step()
        {
            Annotation = "";
        }

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it that Sprocket does not use, held as written, or null where there are none.</summary>
        public UnusedAttributes Attributes { get; set; }

        /// <summary>Whether it is switched off: kept whole, and read and checked as ever, but never done.</summary>
        public bool IsOff { get; set; }

        /// <summary>The names it acts on or reads: a system's variables, or a system blueprint's slots; none for most steps.</summary>
        public virtual IList<string> Targets => Nothing;

        /// <summary>What it gives its member, in the order the member declares it; none for most steps.</summary>
        public virtual IList<Literal> Arguments => NoArguments;

        /// <summary>Adds to <paramref name="into" /> every list of names it holds, the lists themselves: what it acts on or reads, and each argument that takes slots.</summary>
        /// <remarks>Walks the step's arguments, so it is for an edit or a screen being built, never for a tick.</remarks>
        public void NameLists(List<IList<string>> into)
        {
            if (Targets.Count > 0)
                into.Add(Targets);

            var given = Arguments;
            for (var i = 0; i < given.Count; i++)
            {
                if (given[i].Kind == ValueKind.Slots)
                    into.Add(given[i].Names);
            }
        }
    }
}
