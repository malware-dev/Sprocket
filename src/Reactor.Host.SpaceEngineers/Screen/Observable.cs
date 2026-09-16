using System.Collections.Generic;
using System.ComponentModel;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Something whose values a target can watch rather than poll.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="INotifyPropertyChanged"/> deliberately rather than an interface
    /// of our own. Space Engineers whitelists it, and every XAML-based target binds to
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
        public event PropertyChangedEventHandler PropertyChanged;

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
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
