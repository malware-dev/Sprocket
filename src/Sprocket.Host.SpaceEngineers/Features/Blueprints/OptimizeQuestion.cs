using System.Collections.Generic;
using System.Text;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>
    /// What Optimize slots asks: the changes that give a blueprint as few slots as its steps and triggers can name, each
    /// naming the other installations that would have to fill it again, made as one step back only once they are approved.
    /// </summary>
    public sealed class OptimizeQuestion
    {
        readonly SprocketEditor _editor;
        readonly List<string> _lines = new List<string>();
        readonly List<string> _names = new List<string>();

        /// <summary>The question as <paramref name="editor" /> asks it.</summary>
        public OptimizeQuestion(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// Works out the changes for <paramref name="system" />, filled as the installation shown fills it, and asks under what
        /// was pressed whether to make them; says so where there is nothing to change.
        /// </summary>
        /// <remarks>Works the slots out from every list the system names, so it is for a press, never for building a screen.</remarks>
        public void Ask(AutomationSystem system)
        {
            var texts = _editor.Texts;
            var reslot = _editor.Picks.Arranged(system);
            if (reslot == null || reslot.IsEmpty)
            {
                _editor.AskHere(texts.SlotOptimizeNothing);
                _editor.Answer(texts.SlotOptimizeOk, delegate { }, true);
                return;
            }

            _editor.AskHere(Said(system, reslot));
            _editor.Answer(texts.SlotOptimizeApply, () => _editor.Do(system, new ChangeSlots(ChangeKind.OptimizeSlots, reslot)));
            _editor.Answer(texts.SlotOptimizeCancel, delegate { }, true);
        }

        /// <summary>The question, as markdown-lite: what it asks, then each change of <paramref name="reslot" /> to <paramref name="system" /> as an item of a list.</summary>
        string Said(AutomationSystem system, Reslot reslot)
        {
            var texts = _editor.Texts;
            _lines.Clear();

            for (var i = 0; i < reslot.Made.Count; i++)
            {
                var made = Template.Fill(texts.SlotOptimizeMakes, "slot", Bold(reslot.Made[i].Id));
                _lines.Add(Template.Fill(made, "blocks", Markdown.Escape(Blocks(reslot.MadeWith(i)))));
            }

            for (var i = 0; i < reslot.Renames; i++)
            {
                var from = reslot.RenamedFrom(i);
                var to = reslot.RenamedTo(i);
                if (RenamedBefore(reslot, i))
                    continue;

                var renamed = Template.Fill(texts.SlotOptimizeRenames, "from", Bold(from));
                _lines.Add(Losing(system, reslot, Template.Fill(renamed, "to", Bold(to)), from, to));
            }

            foreach (var part in reslot.Dropped)
            {
                var one = new[] { part.Id };
                _lines.Add(Losing(system, reslot, Template.Fill(texts.SlotOptimizeRemoves, "slot", Bold(one)), one, null));
            }

            var said = new StringBuilder(texts.SlotOptimizeQuestion);
            said.Append("\n\n");
            foreach (var line in _lines)
                said.Append("- ").Append(line).Append('\n');

            return said.ToString();
        }

        /// <summary>
        /// <paramref name="line" />, followed by which installations other than the one shown would have to fill a slot again
        /// where the steps and triggers naming <paramref name="from" /> name <paramref name="to" />, or where
        /// <paramref name="from" /> is taken out for null: those holding blocks in <paramref name="from" /> that do not fill all of
        /// <paramref name="to" />.
        /// </summary>
        string Losing(AutomationSystem system, Reslot reslot, string line, IList<string> from, IList<string> to)
        {
            _names.Clear();
            foreach (var instance in system.Instances)
            {
                if (instance != reslot.Shown && Holds(instance, from) && (to == null || !Fills(instance, to)))
                    _names.Add(SystemSummary.DescribeInstance(_editor.Texts, _editor.Runner.Bindings, system, instance));
            }

            return _names.Count == 0 ? line : line + " " + Template.Fill(_editor.Texts.SlotOptimizeLoses, "installations", Bold(_names));
        }

        /// <summary>Whether <paramref name="instance" /> holds a block in any of the slots <paramref name="names" />.</summary>
        static bool Holds(SystemInstance instance, IList<string> names)
        {
            foreach (var name in names)
            {
                var filled = instance.FilledWith(name);
                if (filled != null && filled.Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="instance" /> holds a block in every one of the slots <paramref name="names" />.</summary>
        static bool Fills(SystemInstance instance, IList<string> names)
        {
            foreach (var name in names)
            {
                var filled = instance.FilledWith(name);
                if (filled == null || filled.Count == 0)
                    return false;
            }

            return true;
        }

        /// <summary>Whether a list renamed before the one at <paramref name="rename" /> of <paramref name="reslot" /> went from the same slots to the same slots, which says it already.</summary>
        static bool RenamedBefore(Reslot reslot, int rename)
        {
            for (var i = 0; i < rename; i++)
            {
                if (ListComparison.SameAnyOrder(reslot.RenamedFrom(i), reslot.RenamedFrom(rename))
                    && ListComparison.SameAnyOrder(reslot.RenamedTo(i), reslot.RenamedTo(rename)))
                    return true;
            }

            return false;
        }

        /// <summary>The blocks <paramref name="references" /> name, as the construct calls them, separated by commas.</summary>
        string Blocks(IList<string> references)
        {
            var called = new List<string>();
            foreach (var reference in references)
            {
                var block = _editor.Runner.Bindings.Entity(reference);
                called.Add(block != null ? block.Name : _editor.Program.Called(reference, _editor.World.Values));
            }

            return string.Join(", ", called.ToArray());
        }

        /// <summary><paramref name="name" />, in bold.</summary>
        static string Bold(string name) => "**" + Markdown.Escape(name) + "**";

        /// <summary>Each of <paramref name="names" /> in bold, separated by commas.</summary>
        static string Bold(IList<string> names)
        {
            var bold = new string[names.Count];
            for (var i = 0; i < names.Count; i++)
                bold[i] = Bold(names[i]);

            return string.Join(", ", bold);
        }
    }
}
