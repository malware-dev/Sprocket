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
    /// Works on the rows as <see cref="PartList.Listed" /> lists them with every category named: a <see cref="Part" /> for a
    /// slot, and a category's name before the slots under it.
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

        /// <summary>How many rows after the one at <paramref name="index" /> of <paramref name="rows" /> go with it: a category's slots, and none for a slot.</summary>
        public static int Carries(IList<object> rows, int index)
        {
            if (!(rows[index] is string))
                return 0;

            var count = 0;
            for (var i = index + 1; i < rows.Count && rows[i] is Part; i++)
                count++;

            return count;
        }

        /// <summary>
        /// Where the row at <paramref name="index" /> of <paramref name="rows" /> may land, each as how many of the rows it does
        /// not carry stand above it: a slot anywhere, null, and a category only between the others, under the slots that have none.
        /// </summary>
        public static IList<int> Landings(IList<object> rows, int index)
        {
            if (!(rows[index] is string))
                return null;

            var span = Carries(rows, index);
            var landings = new List<int>();
            var among = 0;
            for (var i = 0; i <= rows.Count; i++)
            {
                if (i >= index && i <= index + span)
                    continue;

                if (i == rows.Count || rows[i] is string)
                    landings.Add(among);

                among++;
            }

            return landings;
        }

        /// <summary>Moves the row at <paramref name="from" /> of <paramref name="rows" />, with what it carries, to stand under <paramref name="to" /> of the others.</summary>
        public void Move(IList<object> rows, int from, int to)
        {
            var span = Carries(rows, from);
            var moved = new List<object>();
            var rest = new List<object>();
            for (var i = 0; i < rows.Count; i++)
            {
                if (i >= from && i <= from + span)
                    moved.Add(rows[i]);
                else
                    rest.Add(rows[i]);
            }

            rest.InsertRange(to, moved);
            var now = new SlotArrangement();
            var category = "";
            foreach (var row in rest)
            {
                var named = row as string;
                if (named != null)
                {
                    category = named;
                    now.Categories.Add(named);
                    continue;
                }

                now.Order.Add((Part)row);
                now.Placed.Add(category);
            }

            // The slots the panel does not list stay under the category they were, after the ones it does.
            var parts = _system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (now.Order.Contains(parts[i]))
                    continue;

                now.Order.Add(parts[i]);
                now.Placed.Add(now.Categories.Contains(parts[i].Category) ? parts[i].Category : "");
            }

            Make(moved[0] is string ? ChangeKind.MoveCategory : ChangeKind.MoveSlot, now);
        }

        /// <summary>Adds a category after the others, named as <paramref name="texts" /> names a new one, counting on from how many there are.</summary>
        public void Add(Texts texts)
        {
            var now = SlotArrangement.Of(_system);
            var number = now.Categories.Count + 1;
            var name = Named(texts, number);
            while (now.Categories.Contains(name))
                name = Named(texts, ++number);

            now.Categories.Add(name);
            Make(ChangeKind.AddCategory, now);
        }

        /// <summary>Whether a category may be called <paramref name="name" /> in place of <paramref name="was" />: something, and no other category's name.</summary>
        public bool Accepts(string was, string name)
        {
            var wanted = name.Trim();
            return wanted.Length > 0 && (wanted == was || !_system.Categories.Contains(wanted));
        }

        /// <summary>Calls the category <paramref name="was" /> <paramref name="name" />, trimmed, where it may be called that, and its slots stay under it.</summary>
        public void Rename(string was, string name)
        {
            var wanted = name.Trim();
            if (wanted == was || !Accepts(was, wanted))
                return;

            var now = SlotArrangement.Of(_system);
            var at = now.Categories.IndexOf(was);
            if (at < 0)
                return;

            now.Categories[at] = wanted;
            for (var i = 0; i < now.Placed.Count; i++)
            {
                if (now.Placed[i] == was)
                    now.Placed[i] = wanted;
            }

            Make(ChangeKind.RenameCategory, now);
        }

        /// <summary>Removes the category <paramref name="name" />, leaving its slots under none.</summary>
        public void Remove(string name)
        {
            var now = SlotArrangement.Of(_system);
            if (!now.Categories.Remove(name))
                return;

            for (var i = 0; i < now.Placed.Count; i++)
            {
                if (now.Placed[i] == name)
                    now.Placed[i] = "";
            }

            Make(ChangeKind.RemoveCategory, now);
        }

        static string Named(Texts texts, int number)
        {
            return Template.Fill(texts.CategoryDefault, "number", number.ToString(CultureInfo.InvariantCulture));
        }

        void Make(ChangeKind change, SlotArrangement now)
        {
            _editor.Do(_system, new ArrangeSlots(change, SlotArrangement.Of(_system), now));
        }
    }
}
