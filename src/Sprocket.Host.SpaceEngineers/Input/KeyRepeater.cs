using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Input
{
    /// <summary>Turns a held key into the press, pause, flood pattern every text field has.</summary>
    /// <remarks>
    /// The game reports a key as newly pressed for exactly one tick, which is right for a jump and wrong
    /// for a caret. Typed characters do not need this: the keyboard's own repeat has already put them in
    /// the text buffer by the time we see it. This is only for the keys that are read raw.
    /// <para>
    /// The caller passes the tick and the key state, so none of it needs a game running.
    /// </para>
    /// </remarks>
    public sealed class KeyRepeater
    {
        private struct State
        {
            public int FirstPress;
            public int LastFire;
        }

        private readonly Dictionary<int, State> _states = new Dictionary<int, State>();
        private readonly int _delayTicks;
        private readonly int _intervalTicks;

        /// <summary>Half a second before it starts, then about twenty a second, which is the usual feel.</summary>
        public KeyRepeater()
            : this(30, 3)
        {
        }

        public KeyRepeater(int delayTicks, int intervalTicks)
        {
            _delayTicks = delayTicks;
            _intervalTicks = intervalTicks;
        }

        /// <summary>Whether the key should count as pressed this tick, by first press or by repeat.</summary>
        public bool Fires(int key, bool newPress, bool held, int now)
        {
            if (newPress)
            {
                State fresh;
                fresh.FirstPress = now;
                fresh.LastFire = now;
                _states[key] = fresh;
                return true;
            }

            if (!held)
            {
                _states.Remove(key);
                return false;
            }

            State state;
            if (!_states.TryGetValue(key, out state))
                return false;

            if (now - state.FirstPress < _delayTicks || now - state.LastFire < _intervalTicks)
                return false;

            state.LastFire = now;
            _states[key] = state;
            return true;
        }

        public void Clear()
        {
            _states.Clear();
        }
    }
}
