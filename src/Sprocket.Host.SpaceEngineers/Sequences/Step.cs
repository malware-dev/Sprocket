using System.Collections.Generic;


namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One numbered line of a sequence.
    /// </summary>
    public abstract class Step
    {
        static readonly string[] Nothing = new string[0];

        /// <summary>A step with nothing written about it yet.</summary>
        protected Step()
        {
            Annotation = "";
        }

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>Whether it is switched off: kept whole, and read and checked as ever, but never done.</summary>
        public bool IsOff { get; set; }

        /// <summary>The names it acts on or reads: a system's variables, or a system blueprint's slots; none for most steps.</summary>
        public virtual IList<string> Targets => Nothing;
    }
}
