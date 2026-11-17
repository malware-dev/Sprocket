using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// An append-only run of values, kept in fixed chunks rather than one growing array.
    /// </summary>
    /// <remarks>
    /// Measured: one list crosses the runtime's large object threshold, 85,000 bytes, at a few thousand values, and a
    /// program of six thousand statements then caused full collections. Each chunk stays below it and is never copied.
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

        /// <summary>Puts <paramref name="value" /> in place of the value at <paramref name="index" />.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public void Set(int index, Value value)
        {
            _chunks[index >> Shift][index & Mask] = value;
        }

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
