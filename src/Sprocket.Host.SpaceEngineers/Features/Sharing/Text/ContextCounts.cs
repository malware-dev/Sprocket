using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>How often each byte has followed one context, in the order the bytes were first seen.</summary>
    public sealed class ContextCounts
    {
        // Keeps every total a range coder is given under 65536, escape included.
        const int Ceiling = 1 << 15;

        readonly List<byte> _symbols = new List<byte>();
        readonly List<int> _counts = new List<int>();

        /// <summary>How many bytes this context has seen.</summary>
        public int Count => _symbols.Count;

        /// <summary>The <paramref name="index" />th byte seen.</summary>
        public byte Symbol(int index) => _symbols[index];

        /// <summary>How often the <paramref name="index" />th byte has been seen.</summary>
        public int Times(int index) => _counts[index];

        /// <summary>Counts <paramref name="symbol" /> once more, halving every count once they add up past the ceiling.</summary>
        public void Add(byte symbol)
        {
            var index = _symbols.IndexOf(symbol);
            if (index < 0)
            {
                _symbols.Add(symbol);
                _counts.Add(1);
            }
            else
            {
                _counts[index]++;
            }

            var sum = 0;
            foreach (var count in _counts)
                sum += count;
            if (sum <= Ceiling)
                return;

            for (var i = 0; i < _counts.Count; i++)
                _counts[i] = (_counts[i] + 1) / 2;
        }
    }
}
