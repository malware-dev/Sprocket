using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>The slots a value of a pick names, chosen on a selector of their own: what is selected there, kept until the pick finishes and the slots are made.</summary>
    public sealed class TargetSelection
    {
        readonly SprocketEditor _editor;
        readonly EditorPage _page;
        readonly PickChoices _picked;
        PickAnswer _chosen;
        IList<string> _replaces;

        /// <summary>The selection asked from <paramref name="page" /> for a value of what <paramref name="picked" /> holds.</summary>
        public TargetSelection(SprocketEditor editor, EditorPage page, PickChoices picked)
        {
            _editor = editor;
            _page = page;
            _picked = picked;
        }

        /// <summary>What the slot the pick itself is about to make is called, or empty where it makes none.</summary>
        public Func<string> SourceName { get; set; }

        /// <summary>The logic system whose slots are offered, or null where none are.</summary>
        public AutomationSystem System { get; set; }

        /// <summary>Whether something has been selected, waiting for the pick to finish.</summary>
        public bool IsChosen => _chosen != null;

        /// <summary>The slots the value comes to: what was selected, made where no slot holds it, or otherwise those <paramref name="said" /> names.</summary>
        public List<string> Slots(string said)
        {
            return _chosen != null ? Made() : Named(said);
        }

        /// <summary>Has every field in <paramref name="form" /> that takes slots read its names as the slots of the logic system, a name with a comma in it whole.</summary>
        public void Reads(FieldSet form)
        {
            foreach (var field in form.Fields)
                (field as SlotsField)?.ReadNamesBy(Named);
        }

        /// <summary>Opens the selector of the first field in <paramref name="strip" /> that takes slots, which finishes the pick through <paramref name="finish" />; whether there is one.</summary>
        public bool SelectFirst(PickStrip strip, ICommand finish)
        {
            if (strip.Arguments == null)
                return false;

            foreach (var field in strip.Arguments.Form.Fields)
            {
                var slots = field as SlotsField;
                if (slots == null)
                    continue;

                Select(slots, finish);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens the selector for what <paramref name="field" /> names, of blocks or of the slots the logic system has, on what
        /// it holds. What is chosen is only remembered, by the names of its blocks or slots, until the pick is finished, which
        /// <paramref name="finish" /> does at once where there is one.
        /// </summary>
        public void Select(SlotsField field, ICommand finish)
        {
            if (_editor.Current != _page || System == null)
                return;

            var member = _picked.Member;
            var form = _picked.Form;
            var type = Vocabulary.TypeOf(_editor.World, _picked.Blocks, member);
            _replaces = _chosen != null || field.Text.Length == 0 ? _replaces : Named(field.Text);
            var held = _chosen != null ? _chosen.Targets : _replaces == null ? new List<long>() : _editor.Blocks(System, _replaces);
            var facet = _replaces == null ? "" : _editor.Picks.Facet(System, _replaces);
            var avoiding = SourceName == null ? "" : SourceName();
            var source = new SourceSwitch(_editor, slots => new PickPage(_editor, field.Label, slots ? new List<long>() : held, member, form, facet,
                block => type != null && block.Kind.Facets(type).Count > 0, answer =>
                {
                    _chosen = answer;
                    _editor.Pop();
                    field.Give(Called(answer));
                    finish?.Execute();
                })
            {
                Slotted = System,
                ListsSlots = slots,
                Replacing = _replaces,
                Avoiding = avoiding
            }, null);
            source.Ask(null);
        }

        /// <summary>The slots <paramref name="said" /> names, comma by comma.</summary>
        /// <remarks>A slot's own name may hold a comma, so the longest run of words naming a slot of the logic system is taken as one.</remarks>
        List<string> Named(string said)
        {
            var slots = new List<string>();
            var words = said.Split(',');
            for (var first = 0; first < words.Length; first++)
            {
                for (var last = words.Length - 1; last >= first; last--)
                {
                    var name = string.Join(",", words, first, last - first + 1).Trim();
                    if (last != first && System?.FindPart(name) == null)
                        continue;

                    if (name.Length != 0)
                        slots.Add(name);

                    first = last;
                    break;
                }
            }

            return slots;
        }

        /// <summary>What <paramref name="answer" /> chose, as a person reads it: its slots by name, or its blocks by theirs.</summary>
        string Called(PickAnswer answer)
        {
            if (answer.Slots.Count > 0)
                return string.Join(", ", answer.Slots);

            var names = new List<string>();
            foreach (var id in answer.Targets)
            {
                var entity = _editor.World.FindEntity(id);
                if (entity != null)
                    names.Add(entity.Name);
            }

            return string.Join(", ", names);
        }

        /// <summary>The slots what was selected comes to, made where none holds it; numbered after the slot the pick itself is about to make.</summary>
        List<string> Made()
        {
            var answer = _chosen;
            _chosen = null;

            var source = SourceName == null ? "" : SourceName();
            if (answer.Slots.Count == 0 && answer.SlotName == source)
            {
                var type = Vocabulary.TypeOf(_editor.World, answer.Targets, answer.Key);
                answer.SlotName = _editor.Picks.NewSlotName(System, "", false, type, source);
            }

            var slots = new List<string>();
            _editor.Picks.Name(System, answer, false, _replaces, slots.AddRange);
            return slots;
        }
    }
}
