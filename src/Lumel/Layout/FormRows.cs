using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Layout
{
    /// <summary>The rows of a <see cref="Form" />, in order, each a label and a field, or one element across the whole width.</summary>
    /// <remarks>Written as a collection initializer: <c>Rows = { { label, field }, { label, field, FormRowLines.Two }, { swatches } }</c>.</remarks>
    public sealed class FormRows : IEnumerable
    {
        readonly List<FormRow> _rows = new List<FormRow>();
        readonly Action<Element> _joined;
        readonly Action<Element> _released;

        /// <summary>Rows told to <paramref name="joined" /> as each element joins them and <paramref name="released" /> as each leaves.</summary>
        public FormRows(Action<Element> joined, Action<Element> released)
        {
            _joined = joined;
            _released = released;
        }

        /// <summary>How many rows there are.</summary>
        public int Count => _rows.Count;

        /// <summary>The row at <paramref name="index" />.</summary>
        public FormRow this[int index] => _rows[index];

        /// <summary>A row of <paramref name="label" /> and <paramref name="field" />, side by side where they fit.</summary>
        public void Add(Element label, Element field)
        {
            Add(label, field, FormRowLines.Fit);
        }

        /// <summary>A row of <paramref name="label" /> and <paramref name="field" />, on as many lines as <paramref name="lines" /> says.</summary>
        public void Add(Element label, Element field, FormRowLines lines)
        {
            if (label == null || field == null)
                throw new ArgumentNullException(label == null ? nameof(label) : nameof(field));

            _rows.Add(new FormRow(label, field, lines));
            _joined(label);
            _joined(field);
        }

        /// <summary>A row of <paramref name="whole" /> alone, across the whole width.</summary>
        public void Add(Element whole)
        {
            if (whole == null)
                throw new ArgumentNullException(nameof(whole));

            _rows.Add(new FormRow(null, whole, FormRowLines.Two));
            _joined(whole);
        }

        /// <summary>Takes every row away.</summary>
        public void Clear()
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                var row = _rows[i];
                _rows.RemoveAt(i);
                _released(row.Field);
                if (row.Label != null)
                    _released(row.Label);
            }
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator()
        {
            return _rows.GetEnumerator();
        }
    }
}
