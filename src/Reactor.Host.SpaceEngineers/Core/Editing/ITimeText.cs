namespace Reactor.Host.SpaceEngineers.Core.Editing
{
    /// <summary>
    /// The words for a length of time.
    /// </summary>
    /// <remarks>
    /// Its own interface rather than part of <see cref="ISequenceText" />, because time is not
    /// about sequences: the day something else has a duration, this already answers for it, and a
    /// target that says time the same way everywhere says it in one place.
    /// <para>
    /// Core counts in ticks and only in ticks - it is the one honest unit, since the simulation sags
    /// under load and anything promising seconds would drift exactly when the machine matters most.
    /// What that is worth saying as belongs here.
    /// </para>
    /// </remarks>
    public interface ITimeText
    {
        /// <summary>How long ago something happened. Empty when it never has.</summary>
        string Ago(int ticksAgo);

        /// <summary>
        /// One offered length of time, as it reads while being chosen.
        /// </summary>
        /// <remarks>
        /// The one place the two vocabularies have to meet: somebody thinks in seconds and the
        /// runtime counts ticks, and the moment of choosing is where both belong on one line.
        /// </remarks>
        string WaitChoice(int ticks);
    }
}
