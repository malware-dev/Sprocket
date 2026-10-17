
namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Something done at most once in a while, and the clock that says when.
    /// </summary>
    /// <remarks>A field holding one must not be readonly, or each question is asked of a copy and nothing is kept.</remarks>
    public struct Interval
    {
        readonly int _every;

        int _last;

        /// <summary>Something done at most once every <paramref name="every" /> ticks, due at once.</summary>
        public Interval(int every)
        {
            _every = every;

            // A full period behind, so the first look is due; int.MinValue would overflow the subtraction in Due.
            _last = -every;
        }

        /// <summary>
        /// Whether enough has passed to do it again, and says so only once per turn.
        /// </summary>
        /// <remarks>Asking moves the clock, so ask once per pass and hold the answer.</remarks>
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
