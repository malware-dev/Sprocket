using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>
    /// Predicts each byte from the bytes before it (PPM with escape method C and exclusion, up to five bytes back), and codes it
    /// through a range coder in as many bits as that prediction leaves.
    /// </summary>
    /// <remarks>An encoder and a decoder agree only where both learned the same bytes in the same order.</remarks>
    public sealed class ContextModel
    {
        const int Longest = 5;
        const long Window = (1L << (8 * Longest)) - 1;

        readonly Dictionary<long, ContextCounts> _contexts = new Dictionary<long, ContextCounts>();
        readonly bool[] _excluded = new bool[256];
        long _history;
        int _known;

        /// <summary>Writes <paramref name="symbol" /> to <paramref name="coder" />, then learns it.</summary>
        public void Encode(RangeEncoder coder, byte symbol)
        {
            System.Array.Clear(_excluded, 0, _excluded.Length);
            for (var order = Deepest(); order >= 0; order--)
            {
                ContextCounts counts;
                if (!_contexts.TryGetValue(Key(order), out counts))
                    continue;

                int sum, distinct;
                Measure(counts, out sum, out distinct);
                if (distinct == 0)
                    continue;

                var start = 0;
                for (var i = 0; i < counts.Count; i++)
                {
                    var seen = counts.Symbol(i);
                    if (_excluded[seen])
                        continue;

                    if (seen == symbol)
                    {
                        coder.Encode(start, counts.Times(i), sum + distinct);
                        Learn(symbol);
                        return;
                    }

                    start += counts.Times(i);
                }

                coder.Encode(sum, distinct, sum + distinct);
                Exclude(counts);
            }

            var rank = 0;
            var left = 0;
            for (var seen = 0; seen < 256; seen++)
            {
                if (_excluded[seen])
                    continue;

                if (seen < symbol)
                    rank++;
                left++;
            }

            coder.Encode(rank, 1, left);
            Learn(symbol);
        }

        /// <summary>Reads the next byte from <paramref name="coder" />, then learns it.</summary>
        public byte Decode(RangeDecoder coder)
        {
            System.Array.Clear(_excluded, 0, _excluded.Length);
            for (var order = Deepest(); order >= 0; order--)
            {
                ContextCounts counts;
                if (!_contexts.TryGetValue(Key(order), out counts))
                    continue;

                int sum, distinct;
                Measure(counts, out sum, out distinct);
                if (distinct == 0)
                    continue;

                var target = coder.Target(sum + distinct);
                if (target >= sum)
                {
                    coder.Decode(sum, distinct);
                    Exclude(counts);
                    continue;
                }

                var start = 0;
                for (var i = 0; i < counts.Count; i++)
                {
                    var seen = counts.Symbol(i);
                    if (_excluded[seen])
                        continue;

                    var times = counts.Times(i);
                    if (target < start + times)
                    {
                        coder.Decode(start, times);
                        Learn(seen);
                        return seen;
                    }

                    start += times;
                }
            }

            var left = 0;
            for (var seen = 0; seen < 256; seen++)
            {
                if (!_excluded[seen])
                    left++;
            }

            var rank = coder.Target(left);
            coder.Decode(rank, 1);
            for (var seen = 0; seen < 256; seen++)
            {
                if (_excluded[seen])
                    continue;

                if (rank-- == 0)
                {
                    Learn((byte)seen);
                    return (byte)seen;
                }
            }

            return 0;
        }

        int Deepest() => _known < Longest ? _known : Longest;

        long Key(int order) => ((long)order << 40) | (_history & ((1L << (8 * order)) - 1));

        void Measure(ContextCounts counts, out int sum, out int distinct)
        {
            sum = 0;
            distinct = 0;
            for (var i = 0; i < counts.Count; i++)
            {
                if (_excluded[counts.Symbol(i)])
                    continue;

                sum += counts.Times(i);
                distinct++;
            }
        }

        void Exclude(ContextCounts counts)
        {
            for (var i = 0; i < counts.Count; i++)
                _excluded[counts.Symbol(i)] = true;
        }

        void Learn(byte symbol)
        {
            for (var order = Deepest(); order >= 0; order--)
            {
                var key = Key(order);
                ContextCounts counts;
                if (!_contexts.TryGetValue(key, out counts))
                    _contexts[key] = counts = new ContextCounts();

                counts.Add(symbol);
            }

            _history = ((_history << 8) | symbol) & Window;
            if (_known < Longest)
                _known++;
        }
    }
}
