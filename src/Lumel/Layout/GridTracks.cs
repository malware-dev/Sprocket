using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Layout
{
    /// <summary>A grid's columns, or its rows, in order.</summary>
    /// <remarks>A track past the last one defined is <see cref="GridLength.Auto" />.</remarks>
    public sealed class GridTracks : IEnumerable<GridLength>
    {
        readonly Action _changed;
        readonly List<GridLength> _lengths = new List<GridLength>();

        /// <summary>No tracks yet, with <paramref name="changed" /> called whenever they change.</summary>
        public GridTracks(Action changed)
        {
            _changed = changed;
        }

        /// <summary>How many tracks are defined.</summary>
        public int Count => _lengths.Count;

        /// <summary>The track at <paramref name="index" />, or <see cref="GridLength.Auto" /> past the last one defined.</summary>
        public GridLength this[int index]
        {
            get { return index < _lengths.Count ? _lengths[index] : GridLength.Auto; }
            set
            {
                while (_lengths.Count <= index)
                    _lengths.Add(GridLength.Auto);

                _lengths[index] = value;
                _changed();
            }
        }

        IEnumerator<GridLength> IEnumerable<GridLength>.GetEnumerator()
        {
            return _lengths.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _lengths.GetEnumerator();
        }

        /// <summary>Adds a track after the last.</summary>
        public void Add(GridLength length)
        {
            _lengths.Add(length);
            _changed();
        }
    }
}
