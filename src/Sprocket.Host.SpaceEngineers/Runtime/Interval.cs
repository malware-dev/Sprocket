
namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Something done at most once in a while, and the clock that says when.
    /// </summary>
    /// <remarks>
    /// Some of what a screen says cannot be announced to it. The game tells nobody when a grid is
    /// renamed or a door opens, so those have to be asked for - and asking sixty times a second for
    /// an answer that changes when somebody walks past is the shape this exists to stop.
    /// <para>
    /// Four of these were written by hand, in four classes, before it was a type: the same three lines
    /// of arithmetic each time, each with its own sentinel, and one of them reset by a method that did
    /// not know the others existed. The comparison is <c>now - last &gt;= every || now &lt; last</c>,
    /// and the second half of that matters: a tick count can be reset by a world being reloaded, and
    /// without it a cadence would then wait forever.
    /// </para>
    /// <para>
    /// The sentinel is one whole period behind rather than zero or <c>int.MinValue</c>. Zero would make
    /// the first period of a new world the one period in which nothing is done; <c>int.MinValue</c> is
    /// worse, because subtracting it from a tick overflows to a negative number, so "long ago" reads as
    /// "just now" and the answer is never yes again.
    /// </para>
    /// <para>
    /// <b>A field holding one must not be readonly.</b> It remembers when it last said yes, so a
    /// readonly field would be copied to be asked and the answer would never be kept.
    /// </para>
    /// </remarks>
    struct Interval
    {
        readonly int _every;

        int _last;

        public Interval(int every)
        {
            _every = every;

            // A full period behind, so the first look is due rather than waiting: something that has
            // never been said has to be said now.
            _last = -every;
        }

        /// <summary>
        /// Whether enough has passed to do it again, and says so only once per turn.
        /// </summary>
        /// <remarks>
        /// Asking moves the clock, so two callers in one pass would leave the second told that no time
        /// had gone by. Ask once and hold the answer.
        /// </remarks>
        public bool Due(int now)
        {
            if (now - _last < _every && now >= _last)
                return false;

            _last = now;
            return true;
        }

        /// <summary>Says it has never been done, so the next look is due.</summary>
        public void Restart()
        {
            _last = -_every;
        }
    }
}
