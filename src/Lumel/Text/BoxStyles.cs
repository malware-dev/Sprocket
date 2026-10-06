using System;
using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>What the host's box attributes ask for, found by name whatever its case.</summary>
    /// <remarks>Never changes once made, so one can be held by a style that every text shares.</remarks>
    public sealed class BoxStyles
    {
        readonly Dictionary<string, BoxStyle> _byName = new Dictionary<string, BoxStyle>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The attributes <paramref name="styles" /> give, no two of them under one name.</summary>
        public BoxStyles(params BoxStyle[] styles)
        {
            for (var i = 0; i < styles.Length; i++)
            {
                if (_byName.ContainsKey(styles[i].Name))
                    throw new ArgumentException("Two box styles are given for the attribute " + styles[i].Name + ".", nameof(styles));

                _byName.Add(styles[i].Name, styles[i]);
            }
        }

        /// <summary>What the attribute <paramref name="name" /> asks for, or null where the host gives it nothing.</summary>
        public BoxStyle Find(string name)
        {
            BoxStyle style;
            return name != null && _byName.TryGetValue(name, out style) ? style : null;
        }
    }
}
