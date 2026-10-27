using System.Collections.Generic;
using System.Globalization;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>
    /// Changes how a logic system's slots stand on its installation panel, each change one step back: a slot or a category
    /// moved, or a category added, renamed or removed.
    /// </summary>
    /// <remarks>
    /// Works on the rows as <see cref="PartList.Listed" /> lists them with every category headed: a <see cref="Part" /> for a
    /// slot, and a <see cref="CategoryHeading" /> before the run of slots under a category, or before none.
    /// </remarks>
    public sealed class SlotArranger
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;

        /// <summary>Changes the slots of <paramref name="system" />, through <paramref name="editor" />.</summary>
        public SlotArranger(SprocketEditor editor, AutomationSystem system)
        {
            _editor = editor;
            _system = system;
        }

        /// <summary>How many rows after the one at <paramref name="index" /> of <paramref name="rows" /> go with it: a category's run of slots, and none for a slot.</summary>
        public static int Carries(IList<object> rows, int index)
        {
            var heading = rows[index] as CategoryHeading;
            if (heading == null || heading.Empty != null)
                return 0;

            var count = 0;
            for (var i = index + 1; i < rows.Count; i++)
            {
                var part = rows[i] as Part;
                if (part == null || part.Category != heading.Name)
                    break;

                count++;
            }

            return count;
        }

        /// <summary>
        /// Where the row at <paramref name="index" /> of <paramref name="rows" /> may land, each as how many of the rows it does
        /// not carry stand above it: a slot anywhere, null, and a category anywhere but inside another's run of slots.
        /// </summary>
        public static IList<int> Landings(IList<object> rows, int index)
        {
            if (!(rows[index] is CategoryHeading))
                return null;

            var rest = Rest(rows, index);
            var landings = new List<int>();
            for (var to = 0; to <= rest.Count; to++)
            {
                bool inside;
                Around(rest, to, out inside);
                if (!inside)
                    landings.Add(to);
            }

            return landings;
        }

        /// <summary>
        /// Whether the slot at <paramref name="index" /> of <paramref name="rows" />, landing under <paramref name="to" /> of the
        /// others, stands at the end of a category and may go into it or stay out of it.
        /// </summary>
        public static bool Nests(IList<object> rows, int index, int to)
        {
            if (!(rows[index] is Part))
                return false;

            bool inside;
            return Around(Rest(rows, index), to, out inside).Length > 0 && !inside;
        }

        /// <summary>Whether the row at <paramref name="index" /> of <paramref name="rows" /> stands under a category.</summary>
        public static bool StandsIn(IList<object> rows, int index)
        {
            var part = rows[index] as Part;
            return part != null && part.Category.Length > 0;
        }

        /// <summary>
        /// Moves the row at <paramref name="from" /> of <paramref name="rows" />, with what it carries, to stand under
        /// <paramref name="to" /> of the others; a slot landing at the end of a category goes into it where
        /// <paramref name="into" />.
        /// </summary>
        public void Move(IList<object> rows, int from, int to, bool into)
        {
            var span = Carries(rows, from);
            var rest = Rest(rows, from);
            var moved = new List<object>();
            for (var i = from; i <= from + span; i++)
                moved.Add(rows[i]);

            var slot = moved[0] as Part;
            var category = "";
            if (slot != null)
            {
                bool inside;
                var around = Around(rest, to, out inside);
                category = inside || into ? around : "";
            }

            var final = new List<object>(rest);
            final.InsertRange(to, moved);

            var now = new SlotArrangement();
            for (var i = 0; i < final.Count; i++)
            {
                var part = final[i] as Part;
                if (part != null)
                {
                    now.Order.Add(part);
                    now.Placed.Add(part == slot ? category : part.Category);
                    continue;
                }

                // A heading whose slots have all left it, or that a slot has not just filled, stands empty where it is.
                var heading = (CategoryHeading)final[i];
                var next = i + 1 < final.Count ? final[i + 1] as Part : null;
                var filled = next != null && (next == slot ? category : next.Category) == heading.Name;
                if (!filled)
                    now.Empty.Add(heading.Empty != null && heading.Empty.Above == Below(final, i) ? heading.Empty : new EmptyCategory(heading.Name, Below(final, i)));
            }

            // The slots the panel does not list stay as they were, after the ones it does.
            var parts = _system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (now.Order.Contains(parts[i]))
                    continue;

                now.Order.Add(parts[i]);
                now.Placed.Add(parts[i].Category);
            }

            var thing = slot != null ? (object)slot : ((CategoryHeading)moved[0]).Name;
            Make(slot != null ? ChangeKind.MoveSlot : ChangeKind.MoveCategory, now, thing, null);
        }

        /// <summary>Adds an empty category under every slot, named as <paramref name="texts" /> names a new one, counting on from how many there are.</summary>
        public void Add(Texts texts)
        {
            var now = SlotArrangement.Of(_system);
            var names = Names();
            var number = names.Count + 1;
            var name = Named(texts, number);
            while (names.Contains(name))
                name = Named(texts, ++number);

            now.Empty.Add(new EmptyCategory(name, null));
            Make(ChangeKind.AddCategory, now, name, null);
        }

        /// <summary>Whether a category may be called <paramref name="name" />: anything but nothing.</summary>
        public static bool Accepts(string name)
        {
            return name.Trim().Length > 0;
        }

        /// <summary>Calls the category <paramref name="heading" /> heads <paramref name="name" />, trimmed, where it may be called that, and its slots stay under it.</summary>
        public void Rename(CategoryHeading heading, string name)
        {
            var wanted = name.Trim();
            if (wanted == heading.Name || !Accepts(wanted))
                return;

            var now = SlotArrangement.Of(_system);
            if (heading.Empty != null)
            {
                var at = now.Empty.IndexOf(heading.Empty);
                if (at < 0)
                    return;

                now.Empty[at] = new EmptyCategory(wanted, heading.Empty.Above);
            }
            else
            {
                for (var i = Run(now, heading); i >= 0 && i < now.Order.Count && now.Placed[i] == heading.Name; i++)
                    now.Placed[i] = wanted;
            }

            Make(ChangeKind.RenameCategory, now, wanted, heading.Name);
        }

        /// <summary>Removes the category <paramref name="heading" /> heads, leaving its slots under none.</summary>
        public void Remove(CategoryHeading heading)
        {
            var now = SlotArrangement.Of(_system);
            if (heading.Empty != null)
            {
                if (!now.Empty.Remove(heading.Empty))
                    return;
            }
            else
            {
                for (var i = Run(now, heading); i >= 0 && i < now.Order.Count && now.Placed[i] == heading.Name; i++)
                    now.Placed[i] = "";
            }

            Make(ChangeKind.RemoveCategory, now, heading.Name, null);
        }

        /// <summary>The rows of <paramref name="rows" /> other than the one at <paramref name="index" /> and what it carries.</summary>
        static List<object> Rest(IList<object> rows, int index)
        {
            var span = Carries(rows, index);
            var rest = new List<object>();
            for (var i = 0; i < rows.Count; i++)
            {
                if (i < index || i > index + span)
                    rest.Add(rows[i]);
            }

            return rest;
        }

        /// <summary>
        /// The category a row landing under <paramref name="to" /> of <paramref name="rest" /> stands at the end of or inside of,
        /// or empty for none; <paramref name="inside" /> says whether it stands inside, with a slot of that category under it.
        /// </summary>
        static string Around(IList<object> rest, int to, out bool inside)
        {
            var above = to > 0 ? rest[to - 1] : null;
            var below = to < rest.Count ? rest[to] as Part : null;

            var heading = above as CategoryHeading;
            var category = heading != null ? heading.Name : above is Part ? ((Part)above).Category : "";

            inside = category.Length > 0 && below != null && below.Category == category && (heading == null || heading.Empty == null);
            return category;
        }

        /// <summary>The first slot under the row at <paramref name="index" /> of <paramref name="rows" />, or null where none is.</summary>
        static Part Below(IList<object> rows, int index)
        {
            for (var i = index + 1; i < rows.Count; i++)
            {
                var part = rows[i] as Part;
                if (part != null)
                    return part;
            }

            return null;
        }

        /// <summary>Where the run <paramref name="heading" /> heads starts in <paramref name="now" />, or -1.</summary>
        static int Run(SlotArrangement now, CategoryHeading heading)
        {
            return now.Order.IndexOf(heading.First);
        }

        /// <summary>Every name a category on the panel has, each once.</summary>
        List<string> Names()
        {
            var names = new List<string>();
            foreach (var part in _system.Parts)
            {
                if (part.Category.Length > 0 && !names.Contains(part.Category))
                    names.Add(part.Category);
            }

            foreach (var empty in _system.EmptyCategories)
            {
                if (!names.Contains(empty.Name))
                    names.Add(empty.Name);
            }

            return names;
        }

        static string Named(Texts texts, int number)
        {
            return Template.Fill(texts.CategoryDefault, "number", number.ToString(CultureInfo.InvariantCulture));
        }

        void Make(ChangeKind change, SlotArrangement now, object moved, string renamed)
        {
            _editor.Do(_system, new ArrangeSlots(change, SlotArrangement.Of(_system), now, moved, renamed));
        }
    }
}
