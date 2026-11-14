using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Blocks
{
    /// <summary>
    /// The kinds selector: a row for any kind where the page offers one, then each kind its filter offers under its family, kept in the rows a page gives
    /// it, each pressed as the page says.
    /// </summary>
    /// <remarks>The block picker narrows its blocks with it and a slot chooses its type with it; only what each offers differs.</remarks>
    public sealed class KindRows
    {
        readonly KindFilter _filter;
        readonly KeyedChildren<string, KindRow> _rows;
        readonly List<string> _shown = new List<string>();

        /// <summary>The selector of what <paramref name="filter" /> offers, in <paramref name="rows" />, each row pressed through what <paramref name="press" /> makes of its key.</summary>
        /// <remarks>A row's key is empty for any kind, a kind's id, or a family's key from <see cref="KindFilter.FamilyKey" />.</remarks>
        public KindRows(KindFilter filter, Element.Collection rows, Func<string, ICommand> press)
        {
            _filter = filter;
            _rows = new KeyedChildren<string, KindRow>(rows, key => new KindRow { Command = press(key) });
        }

        /// <summary>The row kept under <paramref name="key" />.</summary>
        public KindRow this[string key] => _rows[key];

        /// <summary>
        /// Shows what the filter offers, the row for any kind saying <paramref name="every" /> or left out where that is null,
        /// each counted by <paramref name="counts" /> where it is not null; the words of the row in force, or empty for none.
        /// </summary>
        /// <remarks>Asked as the page is built, never on a tick.</remarks>
        public string Show(Texts texts, string every, IKindCounts counts)
        {
            _filter.Fill(texts);
            _shown.Clear();
            for (var i = every == null ? 1 : 0; i < _filter.Keys.Count; i++)
                _shown.Add(_filter.Keys[i]);

            _rows.Show(_shown);

            var current = every ?? "";
            if (every != null)
            {
                var anything = _rows[""];
                anything.Text = every;
                anything.Count = counts == null ? -1 : counts.All;
                anything.IsCurrent = _filter.Kind.Length == 0;
            }

            for (var i = 0; i < _filter.Shown.Count; i++)
            {
                var row = _rows[_filter.Keys[i + 1]];
                var kind = _filter.Shown[i];
                var family = _filter.Families[i];
                row.Text = kind == null ? FamilyText.Of(texts, family) : kind.DisplayName;
                row.Count = counts == null ? -1 : kind == null ? counts.CountOfFamily(family) : counts.CountOfKind(kind.Id);
                row.IsNested = kind != null && family != Family.None;
                row.IsCurrent = _filter.Kind == _filter.Keys[i + 1];
                if (row.IsCurrent)
                    current = row.Text;
            }

            return current;
        }
    }
}
