using System.Collections.Generic;
using System.ComponentModel;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Something whose values a target can watch rather than poll.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="INotifyPropertyChanged"/> deliberately rather than an interface
    /// of Sprocket's own. Space Engineers whitelists it, and every XAML-based target binds to
    /// it for nothing - a bespoke equivalent would buy purity and cost a translation
    /// layer in every such target.
    ///
    /// A target that cannot bind loses nothing: it ignores the event and re-reads the
    /// properties whenever it likes.
    ///
    /// Property names are passed with <c>nameof</c> rather than inferred from the call
    /// site, so nothing here depends on caller-info attributes being available.
    /// </remarks>
    public abstract class Observable : INotifyPropertyChanged
    {
        PropertyChangedEventHandler _changed;

        /// <summary>
        /// Counted as they come and go, so that letting go can be tested rather than trusted.
        /// </summary>
        /// <remarks>
        /// A component outlives the views that show it, so one that keeps a listener it no longer has
        /// any business with keeps that listener's whole screen alive with it. There is no way to see
        /// that from the outside, and no way to see it from the target either - by the time a view is
        /// dropped the target has already forgotten it. Here is where it accumulates, so here is where
        /// it is counted.
        /// </remarks>
        internal int Listeners { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _changed += value;
                Listeners++;
            }
            remove
            {
                _changed -= value;

                // Never below none. A handler removed twice would otherwise take the count negative,
                // and a test asserting "no more than one listener" would read that as spotless.
                if (Listeners > 0)
                    Listeners--;
            }
        }

        /// <summary>
        /// Stores a value and announces it, or does nothing if it has not changed.
        /// </summary>
        /// <remarks>
        /// The equality check is the point. Values are written every tick from live
        /// state, and without it every watcher would be woken sixty times a second to
        /// be told nothing had happened.
        /// </remarks>
        protected bool Set<T>(ref T field, T value, string name)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            Raise(name);
            return true;
        }

        protected void Raise(string name)
        {
            var handler = _changed;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
