using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>
    /// Changes how a list of a logic system's things stands on its screen, such as its slots on the installation panel or its
    /// sequences on the Sequences tab, each change one step back: one of them or a category moved, or a category added,
    /// renamed or removed.
    /// </summary>
    /// <remarks>
    /// Works on the rows as the screen lists them with every category headed: the thing itself, and a
    /// <see cref="CategoryHeading" /> before the run under a category, or before none.
    /// </remarks>
    public sealed class Arranger
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly Func<AutomationSystem, ICategorizedList> _list;
        readonly ChangeKind _moves;
        readonly ChangeKind _movesCategory;

        /// <summary>
        /// Changes the list <paramref name="list" /> finds in <paramref name="system" />, through <paramref name="editor" />: one
        /// of them moved is offered back as <paramref name="moves" />, and a category moved with what is under it as
        /// <paramref name="movesCategory" />.
        /// </summary>
        public Arranger(SprocketEditor editor, AutomationSystem system, Func<AutomationSystem, ICategorizedList> list, ChangeKind moves,
            ChangeKind movesCategory)
        {
            _editor = editor;
            _system = system;
            _list = list;
            _moves = moves;
            _movesCategory = movesCategory;
        }

        /// <summary>How many rows after the one at <paramref name="index" /> of <paramref name="rows" /> go with it: a category's run and its end, and none for one thing.</summary>
        public static int Carries(IList<object> rows, int index)
        {
            var heading = rows[index] as CategoryHeading;
            if (heading == null)
                return 0;

            var count = 0;
            for (var i = index + 1; heading.Empty == null && i < rows.Count; i++)
            {
                var item = rows[i] as ICategorized;
                if (item == null || item.Category != heading.Name)
                    break;

                count++;
            }

            var end = index + count + 1 < rows.Count ? rows[index + count + 1] as CategoryEnd : null;
            return end != null && end.Heading.Equals(heading) ? count + 1 : count;
        }

        /// <summary>
        /// Where the row at <paramref name="index" /> of <paramref name="rows" /> may land, each as how many of the rows it does
        /// not carry stand above it: one thing anywhere, null, and a category anywhere but inside another's run.
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
        /// Whether the thing at <paramref name="index" /> of <paramref name="rows" />, landing under <paramref name="to" /> of the
        /// others, stands at the end of a category and may go into it or stay out of it.
        /// </summary>
        public static bool Nests(IList<object> rows, int index, int to)
        {
            if (!(rows[index] is ICategorized))
                return false;

            bool inside;
            return Around(Rest(rows, index), to, out inside).Length > 0 && !inside;
        }

        /// <summary>Whether the row at <paramref name="index" /> of <paramref name="rows" /> stands under a category.</summary>
        public static bool StandsIn(IList<object> rows, int index)
        {
            var item = rows[index] as ICategorized;
            return item != null && item.Category.Length > 0;
        }

        /// <summary>
        /// Moves the row at <paramref name="from" /> of <paramref name="rows" />, with what it carries, to stand under
        /// <paramref name="to" /> of the others; one thing landing at the end of a category goes into it where
        /// <paramref name="into" />.
        /// </summary>
        public void Move(IList<object> rows, int from, int to, bool into)
        {
            var span = Carries(rows, from);
            var rest = Rest(rows, from);
            var moved = new List<object>();
            for (var i = from; i <= from + span; i++)
                moved.Add(rows[i]);

            var one = moved[0] as ICategorized;
            var category = "";
            if (one != null)
            {
                bool inside;
                var around = Around(rest, to, out inside);
                category = inside || into ? around : "";
            }

            var final = new List<object>(rest);
            final.InsertRange(to, moved);

            var now = Arranged(final, one, category);

            var thing = one != null ? (object)one : ((CategoryHeading)moved[0]).Name;
            Make(one != null ? _moves : _movesCategory, now, thing, null);
        }

        /// <summary>
        /// How the list stands with <paramref name="made" />, just added after everything else, put at the end of the category
        /// whose <see cref="CategoryEnd" /> stands at <paramref name="end" /> of <paramref name="rows" />, and under it.
        /// </summary>
        public Arrangement Placed(IList<object> rows, int end, ICategorized made)
        {
            var final = new List<object>(rows);
            final.Insert(end, made);
            return Arranged(final, made, ((CategoryEnd)rows[end]).Name);
        }

        /// <summary>
        /// How the list stands as <paramref name="final" /> lists it, with <paramref name="one" />, where there is one, under
        /// <paramref name="category" />; those the screen does not list stay as they were, after the ones it does.
        /// </summary>
        Arrangement Arranged(List<object> final, ICategorized one, string category)
        {
            var now = new Arrangement();
            for (var i = 0; i < final.Count; i++)
            {
                var item = final[i] as ICategorized;
                if (item != null)
                {
                    now.Order.Add(item);
                    now.Placed.Add(item == one ? category : item.Category);
                    now.Held.Add(item == one && item.Category != category ? Held(category) : Arrangement.HeldOf(item));
                    continue;
                }

                if (final[i] is CategoryEnd)
                    continue;

                // A heading whose run has all left it, or that one moved has not just filled, stands empty where it is.
                var heading = (CategoryHeading)final[i];
                var next = i + 1 < final.Count ? final[i + 1] as ICategorized : null;
                var filled = next != null && (next == one ? category : next.Category) == heading.Name;
                if (!filled)
                    now.Empty.Add(heading.Empty != null && heading.Empty.Above == Below(final, i) ? heading.Empty : new EmptyCategory(heading.Name, Below(final, i), OneAtATime(heading.Name)));
            }

            // Those the screen does not list stay as they were, after the ones it does.
            var items = _list(_system).Items;
            for (var i = 0; i < items.Count; i++)
            {
                if (now.Order.Contains(items[i]))
                    continue;

                now.Order.Add(items[i]);
                now.Placed.Add(items[i].Category);
                now.Held.Add(Arrangement.HeldOf(items[i]));
            }

            return now;
        }

        /// <summary>Adds an empty category under everything, named as <paramref name="texts" /> names a new one, counting on from how many there are, and gives back its name.</summary>
        public string Add(Texts texts)
        {
            var now = Arrangement.Of(_list(_system));
            var names = Names();
            var number = names.Count + 1;
            var name = Named(texts, number);
            while (names.Contains(name))
                name = Named(texts, ++number);

            now.Empty.Add(new EmptyCategory(name, null));
            Make(ChangeKind.AddCategory, now, name, null);
            return name;
        }

        /// <summary>Whether a category may be called <paramref name="name" />: anything but nothing.</summary>
        public static bool Accepts(string name)
        {
            return name.Trim().Length > 0;
        }

        /// <summary>Calls the category <paramref name="heading" /> heads <paramref name="name" />, trimmed, where it may be called that, and what is under it stays under it.</summary>
        public void Rename(CategoryHeading heading, string name)
        {
            Settle(heading, name, OneAtATime(heading.Name));
        }

        /// <summary>
        /// Calls the category <paramref name="heading" /> heads <paramref name="name" />, trimmed, where it may be called that, and
        /// has what is under it run one at a time where <paramref name="oneAtATime" />, as one change.
        /// </summary>
        public void Settle(CategoryHeading heading, string name, bool oneAtATime)
        {
            var wanted = name.Trim();
            var renames = wanted != heading.Name && Accepts(wanted);
            var holds = OneAtATime(heading.Name) != oneAtATime;
            if (!renames && !holds)
                return;

            var called = renames ? wanted : heading.Name;
            var now = Arrangement.Of(_list(_system));
            if (heading.Empty != null)
            {
                var at = now.Empty.IndexOf(heading.Empty);
                if (at < 0)
                    return;

                now.Empty[at] = new EmptyCategory(called, heading.Empty.Above, oneAtATime);
            }
            else
            {
                for (var i = Run(now, heading); i >= 0 && i < now.Order.Count && now.Placed[i] == heading.Name; i++)
                {
                    now.Placed[i] = called;
                    if (holds && now.Order[i] is IExclusive)
                        now.Held[i] = oneAtATime ? called : "";
                    else if (now.Held[i] == heading.Name)
                        now.Held[i] = called;
                }
            }

            if (renames)
                Make(ChangeKind.RenameCategory, now, called, heading.Name);
            else
                Make(ChangeKind.CategoryOneAtATime, now, called, null);
        }

        /// <summary>Removes the category <paramref name="heading" /> heads, leaving what was under it under none.</summary>
        public void Remove(CategoryHeading heading)
        {
            var now = Arrangement.Of(_list(_system));
            if (heading.Empty != null)
            {
                if (!now.Empty.Remove(heading.Empty))
                    return;
            }
            else
            {
                for (var i = Run(now, heading); i >= 0 && i < now.Order.Count && now.Placed[i] == heading.Name; i++)
                {
                    now.Placed[i] = "";
                    if (now.Held[i] == heading.Name)
                        now.Held[i] = "";
                }
            }

            Make(ChangeKind.RemoveCategory, now, heading.Name, null);
        }

        /// <summary>
        /// Whether what is under the category called <paramref name="category" /> runs one at a time: everything under it names
        /// the category as its id, or, under an empty one, will.
        /// </summary>
        public bool OneAtATime(string category)
        {
            if (category.Length == 0)
                return false;

            var list = _list(_system);
            var any = false;
            foreach (var item in list.Items)
            {
                if (item.Category != category)
                    continue;

                if (Arrangement.HeldOf(item) != category)
                    return false;

                any = true;
            }

            if (any)
                return true;

            foreach (var empty in list.Empty)
            {
                if (empty.Name == category)
                    return empty.OneAtATime;
            }

            return false;
        }

        /// <summary>The id something put under <paramref name="category" /> runs one at a time with: the category where it runs one at a time, empty where not.</summary>
        string Held(string category)
        {
            return OneAtATime(category) ? category : "";
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
        /// or empty for none; <paramref name="inside" /> says whether it stands inside, with one of that category under it.
        /// </summary>
        static string Around(IList<object> rest, int to, out bool inside)
        {
            // Just above a category's end is inside it, and just below it is outside, whatever stands around.
            var end = to < rest.Count ? rest[to] as CategoryEnd : null;
            if (end != null)
            {
                inside = true;
                return end.Name;
            }

            if (to > 0 && rest[to - 1] is CategoryEnd)
            {
                inside = false;
                return "";
            }

            var above = to > 0 ? rest[to - 1] : null;
            var below = to < rest.Count ? rest[to] as ICategorized : null;
            var heading = above as CategoryHeading;
            var category = heading != null ? heading.Name : above is ICategorized ? ((ICategorized)above).Category : "";
            inside = category.Length > 0 && below != null && below.Category == category && (heading == null || heading.Empty == null);
            return category;
        }

        /// <summary>The first thing under the row at <paramref name="index" /> of <paramref name="rows" />, or null where none is.</summary>
        static ICategorized Below(IList<object> rows, int index)
        {
            for (var i = index + 1; i < rows.Count; i++)
            {
                var item = rows[i] as ICategorized;
                if (item != null)
                    return item;
            }

            return null;
        }

        /// <summary>Where the run <paramref name="heading" /> heads starts in <paramref name="now" />, or -1.</summary>
        static int Run(Arrangement now, CategoryHeading heading)
        {
            return now.Order.IndexOf(heading.First);
        }

        /// <summary>Every name a category in the list has, each once.</summary>
        List<string> Names()
        {
            var list = _list(_system);
            var names = new List<string>();
            foreach (var item in list.Items)
            {
                if (item.Category.Length > 0 && !names.Contains(item.Category))
                    names.Add(item.Category);
            }

            foreach (var empty in list.Empty)
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

        void Make(ChangeKind change, Arrangement now, object moved, string renamed)
        {
            _editor.Do(_system, new Rearrange(change, _list, Arrangement.Of(_list(_system)), now, moved, renamed));
        }
    }
}
