using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>
    /// A list of a system's things, such as its slots or sequences, one under the next, each run under one category headed by a
    /// <see cref="CategoryRow" />, with a spine from the heading's grip down to the last row under it.
    /// </summary>
    /// <remarks>Where the list can be arranged, a row is carried by its grip and a category by its own, with what is under it.</remarks>
    public class CategoryList : ReorderList
    {
        /// <summary>
        /// Puts into <paramref name="rows" /> every one of <paramref name="list" />, in its order, with a
        /// <see cref="CategoryHeading" /> before each run of them under one category; a category with nothing under it is headed
        /// too, where it stands, where <paramref name="empties" />; and each category is ended by a <see cref="CategoryEnd" /> under
        /// what it holds, where <paramref name="ends" />.
        /// </summary>
        public static void Listed(ICategorizedList list, List<object> rows, bool empties, bool ends = false)
        {
            rows.Clear();
            var items = list.Items;
            var previous = "";
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var starts = item.Category.Length > 0 && item.Category != previous;
                if (empties && (starts || item.Category.Length == 0))
                    Empty(list, item, rows);

                if (starts)
                    rows.Add(CategoryHeading.Over(item));

                rows.Add(item);
                previous = item.Category;
            }

            if (empties)
            {
                // Under everything: those standing there, and any whose row is gone or now stands inside a run.
                foreach (var empty in list.Empty)
                {
                    if (!rows.Contains(CategoryHeading.Of(empty)))
                        rows.Add(CategoryHeading.Of(empty));
                }
            }

            if (ends)
                Ended(rows);
        }

        /// <summary>Puts a <see cref="CategoryEnd" /> under what each category of <paramref name="rows" /> holds, or under its heading where it holds nothing.</summary>
        static void Ended(List<object> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var heading = rows[i] as CategoryHeading;
                if (heading == null)
                    continue;

                var at = i + 1;
                while (at < rows.Count)
                {
                    var item = rows[at] as ICategorized;
                    if (item == null || item.Category != heading.Name || heading.Empty != null)
                        break;

                    at++;
                }

                rows.Insert(at, new CategoryEnd(heading));
                i = at;
            }
        }

        /// <summary>Adds to <paramref name="rows" /> the heading of every empty category of <paramref name="list" /> standing above <paramref name="item" />.</summary>
        static void Empty(ICategorizedList list, ICategorized item, List<object> rows)
        {
            foreach (var empty in list.Empty)
            {
                if (empty.Above == item)
                    rows.Add(CategoryHeading.Of(empty));
            }
        }

        /// <summary>Lays the rows out, then sets each boxed row under a category in by the list's indent, so its spine runs beside it.</summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);
            var indent = IndentWidth;
            foreach (var child in Children)
            {
                var row = child as IStandsIn;
                if (row == null || !row.Indented || !row.SetsInWhole || !child.IsVisible)
                    continue;

                var at = child.Bounds;
                child.Arrange(new RectangleF(at.X + indent, at.Y, Math.Max(0f, at.Width - indent), at.Height));
            }
        }

        /// <summary>Draws a spine down from each category's grip to the middle of its last row, of the rows standing in the list or of those being dragged.</summary>
        protected override void DrawBeside(IGraphics g, bool dragged)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var heading = Children[i] as CategoryRow;
                if (heading == null || Dragged(i) != dragged)
                    continue;

                Element last = null;
                for (var j = i + 1; j < Children.Count; j++)
                {
                    var under = Children[j] as IStandsIn;
                    if (under == null || !under.Indented || Dragged(j) != dragged)
                        break;

                    last = Children[j];
                }

                if (last == null)
                    continue;

                var grip = ((IReorderable)heading).DragHandle.Bounds;
                var x = grip.X + grip.Width * 0.5f;
                var top = heading.Bounds.Y + heading.Bounds.Height + heading.RenderTransform.Translate.Y;
                var middle = last.Bounds.Y + last.Bounds.Height * 0.5f + last.RenderTransform.Translate.Y;
                TreeLine.Turning(g, x, top, middle, Sizes.Spine, Sizes.SpineTurn, Palette.Line);
            }
        }
    }
}
