using System;
using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>The looks a host gives tagged quotes, found by tag whatever its case.</summary>
    /// <remarks>Never changes once made, so one can be held by a style that every text shares.</remarks>
    public sealed class QuoteStyles
    {
        readonly Dictionary<string, QuoteStyle> _byTag = new Dictionary<string, QuoteStyle>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The looks <paramref name="styles" /> give, no two of them for one tag.</summary>
        public QuoteStyles(params QuoteStyle[] styles)
        {
            for (var i = 0; i < styles.Length; i++)
            {
                if (_byTag.ContainsKey(styles[i].Tag))
                    throw new ArgumentException("Two quote styles are given for the tag " + styles[i].Tag + ".", nameof(styles));

                _byTag.Add(styles[i].Tag, styles[i]);
            }
        }

        /// <summary>The look of quotes tagged <paramref name="tag" />, or null where there is none and they look like any other quote.</summary>
        public QuoteStyle Find(string tag)
        {
            QuoteStyle style;
            return tag != null && _byTag.TryGetValue(tag, out style) ? style : null;
        }
    }
}
