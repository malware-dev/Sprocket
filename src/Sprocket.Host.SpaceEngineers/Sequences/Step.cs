using System.Collections.Generic;


namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One numbered line of a sequence.
    /// </summary>
    /// <remarks>
    /// Flat and numbered: a sequence is one list of lines, read top to bottom, and an <see cref="If" />'s branches are
    /// lines in it like any other, between the if, its <see cref="Else" />s and its <see cref="EndIf" />.
    ///
    /// A line starts something (<see cref="Act"/>, <see cref="Run"/>), waits for what has been started
    /// (<see cref="Await"/>), spends time (<see cref="Wait"/>), or chooses which lines run.
    /// </remarks>
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
        /// <remarks>
        /// Data somebody typed, not a comment: it is part of the model, round-tripped like a name, and
        /// shown by whatever is drawing. What a target labels it on screen - "description", "notes",
        /// anything, in any language - is the host's word and has no bearing on this one.
        /// </remarks>
        public string Annotation { get; set; }

        /// <summary>Whether it is switched off: kept whole, and read and checked as ever, but never done.</summary>
        public bool IsOff { get; set; }

        /// <summary>The names it acts on or reads: a system's variables, or a system blueprint's slots; none for most steps.</summary>
        public virtual IList<string> Targets => Nothing;
    }
}
