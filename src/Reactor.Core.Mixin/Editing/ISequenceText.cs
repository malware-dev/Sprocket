using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// The words for a sequence, where a person can only read it.
    /// </summary>
    /// <remarks>
    /// <b>Structure where it can be pressed, words where it can only be read.</b> A sentence is
    /// slots, because every part of one is somebody's to change and word order is a language's
    /// business. A list row is a label: nothing on it can be pressed, no part of it is anybody's to
    /// edit, and one line has to stand for a whole sequence. Giving that a model of its own would be
    /// a lot of surface for something only ever looked at.
    /// <para>
    /// <b>Not a phrasebook.</b> Every method takes something from the model and gives back one line,
    /// so this is bounded by the model rather than by how much text a screen happens to have. A
    /// method that took a concept and returned a word would be the phrasebook arriving by the back
    /// door, and belongs somewhere else - or nowhere.
    /// </para>
    /// <para>
    /// <b>One subject per interface.</b> Split from <see cref="ITimeText" /> so neither grows into
    /// a place to put any string that needs a home, and so a target can answer them from wherever it
    /// keeps each kind of wording. When one of these stops being about one subject, it splits again.
    /// </para>
    /// </remarks>
    public interface ISequenceText
    {
        /// <summary>
        /// One line standing for a whole sequence: what starts it, then what it does or is called.
        /// </summary>
        /// <remarks>
        /// Read on every tick for whatever is on screen, so an implementation that builds a fresh
        /// string each time is building one sixty times a second.
        /// </remarks>
        string Sequence(IWorld world, Sequence sequence);

        /// <summary>What starts a sequence, on its own.</summary>
        string Trigger(IWorld world, Trigger trigger);

        /// <summary>
        /// The half a name replaces: what somebody called it, or what it actually does.
        /// </summary>
        /// <remarks>
        /// Split out because a heading is two things and only the second takes a name. A name winning
        /// the whole line would make naming a sequence stop its row saying when it happens.
        /// </remarks>
        string Doing(IWorld world, Sequence sequence);

        string Status(SequenceStatus status);

        /// <summary>Empty for <see cref="SequenceProblem.None" />.</summary>
        string Problem(SequenceProblem problem);
    }
}
