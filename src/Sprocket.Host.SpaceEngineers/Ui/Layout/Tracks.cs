using System;
using System.Collections;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>A grid's columns, or its rows, in order.</summary>
    /// <remarks>A track past the last one defined is <see cref="Length.Auto" />.</remarks>
    public sealed class Tracks : IEnumerable<Length>
    {
        readonly Action _changed;
        readonly List<Length> _lengths = new List<Length>();

        /// <summary>No tracks yet, with <paramref name="changed" /> called whenever they change.</summary>
        public Tracks(Action changed)
        {
            _changed = changed;
        }

        /// <summary>How many tracks are defined.</summary>
        public int Count => _lengths.Count;

        /// <summary>The track at <paramref name="index" />, or <see cref="Length.Auto" /> past the last one defined.</summary>
        public Length this[int index]
        {
            get { return index < _lengths.Count ? _lengths[index] : Length.Auto; }
            set
            {
                while (_lengths.Count <= index)
                    _lengths.Add(Length.Auto);

                _lengths[index] = value;
                _changed();
            }
        }

        IEnumerator<Length> IEnumerable<Length>.GetEnumerator()
        {
            return _lengths.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _lengths.GetEnumerator();
        }

        /// <summary>Adds a track after the last.</summary>
        public void Add(Length length)
        {
            _lengths.Add(length);
            _changed();
        }
    }
}
