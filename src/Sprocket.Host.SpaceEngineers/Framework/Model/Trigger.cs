
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// Something that runs or stops a sequence of its system.
    /// </summary>
    public abstract class Trigger : ICategorized
    {
        /// <summary>A trigger with nothing written about it yet.</summary>
        protected Trigger()
        {
            Annotation = "";
        }

        /// <summary>Adds to <paramref name="into" /> every expression it holds, which a look works out.</summary>
        public virtual void Worked(List<Expression> into)
        {
        }

        /// <summary>Adds to <paramref name="into" /> every list of names the expressions it holds read blocks by, the lists themselves.</summary>
        public void ReadLists(List<IList<string>> into)
        {
            var worked = new List<Expression>();
            Worked(worked);
            foreach (var expression in worked)
                expression.NameLists(into);
        }

        /// <summary>The step it starts, which runs or stops a sequence.</summary>
        public Run Does { get; set; }

        /// <summary>The number, within the system, of the sequence it runs or stops, or 0 where it has no step yet.</summary>
        public int Runs => Does?.Number ?? 0;

        /// <summary>
        /// Whether it stops that sequence rather than starting it.
        /// </summary>
        public bool Stops => Does != null && Does.Stops;

        /// <summary>Whether this runs or stops <paramref name="sequence" />.</summary>
        public bool IsAbout(Sequence sequence)
        {
            return Runs == sequence.Number;
        }

        /// <summary>
        /// What somebody wrote about this trigger, in their own words, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The category the trigger is listed under on the Triggers tab, with the triggers beside it under the same one, or empty for none.</summary>
        public string Category { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, held as written, or null where there are none.</summary>
        public UnusedAttributes Attributes { get; set; }
    }
}
