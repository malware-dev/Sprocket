using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>Writes symbols as bytes, each in as few bits as its chance says, the way LZMA's range coder does.</summary>
    public sealed class RangeEncoder
    {
        const uint Top = 1u << 24;

        readonly List<byte> _bytes = new List<byte>();
        ulong _low;
        uint _range = uint.MaxValue;
        byte _cache;
        long _pending = 1;
        bool _started;

        /// <summary>Writes the symbol whose share of <paramref name="total" /> starts at <paramref name="start" /> and is <paramref name="size" /> long.</summary>
        /// <remarks><paramref name="total" /> is at most 65536.</remarks>
        public void Encode(int start, int size, int total)
        {
            _range /= (uint)total;
            _low += (ulong)start * _range;
            _range *= (uint)size;
            while (_range < Top)
            {
                _range <<= 8;
                ShiftLow();
            }
        }

        /// <summary>Ends the stream and answers every byte written, without the zero bytes it would end on.</summary>
        /// <remarks>A <see cref="RangeDecoder" /> reads missing bytes as zeros, so it reads the stream the same.</remarks>
        public byte[] Finish()
        {
            // Any value from low up to low + range ends the stream right; the one with the most zero bits at its end leaves the
            // most zero bytes to leave out.
            for (var bits = 32; bits > 0; bits--)
            {
                var mask = (1UL << bits) - 1;
                var rounded = (_low + mask) & ~mask;
                if (rounded < _low + _range)
                {
                    _low = rounded;
                    break;
                }
            }

            for (var i = 0; i < 5; i++)
                ShiftLow();

            var length = _bytes.Count;
            while (length > 0 && _bytes[length - 1] == 0)
                length--;

            return _bytes.GetRange(0, length).ToArray();
        }

        void ShiftLow()
        {
            if ((uint)_low < 0xFF000000u || (_low >> 32) != 0)
            {
                var carry = (byte)(_low >> 32);
                var held = _cache;
                do
                {
                    // The coder's first byte is always 0, so it is left out and the decoder starts without it.
                    if (_started)
                        _bytes.Add((byte)(held + carry));

                    _started = true;
                    held = 0xFF;
                } while (--_pending != 0);

                _cache = (byte)(_low >> 24);
            }

            _pending++;
            _low = (_low & 0x00FFFFFFu) << 8;
        }
    }
}
