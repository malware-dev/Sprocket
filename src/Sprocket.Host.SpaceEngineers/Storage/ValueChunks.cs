using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// An append-only run of values, kept in fixed chunks rather than one growing array.
    /// </summary>
    /// <remarks>
    /// <b>Measured, and it replaced a plain list that made things worse.</b> One list for every value in a
    /// read crosses the runtime's large object threshold - 85,000 bytes - at a few thousand values, and
    /// from there every doubling is a fresh large object that only a full collection reclaims. Reading a
    /// program of six thousand statements started causing full collections where it had caused none, while
    /// allocating less: it traded many cheap short-lived arrays for a few expensive long-lived ones. The
    /// game's runtime has the same threshold.
    /// <para>
    /// So values go into chunks small enough never to be large objects, whatever a value grows into, and
    /// a chunk is never copied: growing is adding another one. Finding a value is a shift and a mask.
    /// </para>
    /// </remarks>
    public sealed class ValueChunks
    {
        const int Shift = 8;

        const int Size = 1 << Shift;
        const int Mask = Size - 1;

        readonly List<Value[]> _chunks = new List<Value[]>();
        Value[] _current;

        /// <summary>How many values it holds.</summary>
        public int Count { get; private set; }

        /// <summary>The value at <paramref name="index" />.</summary>
        public Value this[int index] => _chunks[index >> Shift][index & Mask];

        /// <summary>Adds <paramref name="value" /> after the last.</summary>
        public void Add(Value value)
        {
            var slot = Count & Mask;
            if (slot == 0)
            {
                _current = new Value[Size];
                _chunks.Add(_current);
            }

            _current[slot] = value;
            Count++;
        }
    }
}
