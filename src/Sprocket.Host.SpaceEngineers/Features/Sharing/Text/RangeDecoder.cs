namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>Reads back the symbols a <see cref="RangeEncoder" /> wrote.</summary>
    public sealed class RangeDecoder
    {
        const uint Top = 1u << 24;

        readonly byte[] _bytes;
        int _next;
        uint _code;
        uint _range = uint.MaxValue;

        /// <summary>A decoder over <paramref name="bytes" />, starting at <paramref name="from" />.</summary>
        public RangeDecoder(byte[] bytes, int from)
        {
            _bytes = bytes;
            _next = from;
            for (var i = 0; i < 4; i++)
                _code = (_code << 8) | Next();
        }

        /// <summary>Whether the decoder has read further past the end of its bytes than the zeros a whole stream leaves out.</summary>
        public bool IsOverrun => _next > _bytes.Length + 16;

        /// <summary>Where in <paramref name="total" /> the next symbol lies; <see cref="Decode" /> follows with that symbol's share.</summary>
        public int Target(int total)
        {
            _range /= (uint)total;
            var target = _code / _range;
            return target < (uint)total ? (int)target : total - 1;
        }

        /// <summary>Takes the symbol whose share starts at <paramref name="start" /> and is <paramref name="size" /> long off the stream.</summary>
        public void Decode(int start, int size)
        {
            _code -= (uint)start * _range;
            _range *= (uint)size;
            while (_range < Top)
            {
                _code = (_code << 8) | Next();
                _range <<= 8;
            }
        }

        uint Next()
        {
            var at = _next++;
            return at < _bytes.Length ? _bytes[at] : 0u;
        }
    }
}
