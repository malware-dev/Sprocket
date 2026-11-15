using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Writing;
using System;
using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>
    /// The arguments a pick asks for what it has chosen: the fields, what they open holding, what has been typed into them,
    /// and the column beside the stages they are given in.
    /// </summary>
    public class PickArguments
    {
        readonly SprocketEditor _editor;
        readonly EditorPage _page;
        readonly PickChoices _picked;
        readonly PickChoices _arrived;
        readonly Func<bool> _complete;
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly bool _opensHere;
        readonly List<Literal> _arrivedValues = new List<Literal>();
        readonly List<string> _filled = new List<string>();
        readonly List<Field> _nothing = new List<Field>();
        readonly List<Field> _shownFields = new List<Field>();

        // Whether the values last opened show what the block holds now rather than what anybody gave.
        bool _offering;
        string _entered;
        string _shown;
        bool _openedArguments;
        int _worded;

        /// <summary>
        /// The arguments asked on <paramref name="page" /> for what <paramref name="picked" /> holds, which opened as
        /// <paramref name="arrived" /> holds it, given <paramref name="arguments" /> or none; they are asked only once
        /// <paramref name="complete" /> says the pick can be finished, and take the keyboard once where <paramref name="opensHere" />.
        /// </summary>
        public PickArguments(SprocketEditor editor, EditorPage page, PickChoices picked, PickChoices arrived, IList<Literal> arguments,
            Func<bool> complete, bool forEvents, bool forTests, bool opensHere)
        {
            _editor = editor;
            _page = page;
            _picked = picked;
            _arrived = arrived;
            _complete = complete;
            _forEvents = forEvents;
            _forTests = forTests;
            _opensHere = opensHere;
            Target = new TargetSelection(editor, page, picked);
            if (arguments == null)
                return;

            for (var i = 0; i < arguments.Count; i++)
                _arrivedValues.Add(arguments[i]);
        }

        /// <summary>Gives the step the single argument <paramref name="value" /> in place of any it opened with.</summary>
        public void Arrive(Literal value)
        {
            _arrivedValues.Clear();
            _arrivedValues.Add(value);
        }

        /// <summary>The slots a field that takes slots names, chosen on a selector of their own.</summary>
        public TargetSelection Target { get; }

        /// <summary>What each value the chosen action takes wants, one field each, and empty where it takes none.</summary>
        public List<Field> Wanted()
        {
            if (!_complete())
                return _nothing;

            var selection = _picked.Blocks;

            // A number watched to start something is compared as a check compares it.
            if (!_forTests && !(_forEvents && _picked.Form == ReachKind.Value))
                return _editor.Fields(selection, _picked.Member, _picked.Form);

            var value = _picked.Member;
            string state;
            var stated = _forTests && ComparisonQuestion.Split(_picked.Member, out value, out state);
            return ComparisonQuestion.Asked(Vocabulary.ValueOf(_editor.World, selection, value), Vocabulary.EventsOf(_editor.World, selection), stated);
        }

        /// <summary>What the values open holding, where this opened on something that has values.</summary>
        /// <remarks>
        /// The step's own values only while its own action is chosen.
        /// </remarks>
        public IList<string> Filled
        {
            get
            {
                _filled.Clear();
                _offering = false;
                if (_arrivedValues.Count > 0 && _picked.Member == _arrived.Member && _picked.Form == _arrived.Form)
                {
                    ColorChannels.Shown(_arrivedValues, Wanted(), _filled);
                    return _filled;
                }

                if (_forTests || _forEvents || _picked.Blocks.Count == 0)
                    return _filled;

                // A value being set opens on what the block holds now, as the terminal's own control does.
                if (_picked.Form == ReachKind.Value)
                {
                    var current = Vocabulary.Current(_editor.World, _picked.Blocks, _picked.Member, _picked.Facet);
                    if (current.HasValue)
                    {
                        ColorChannels.Shown(new[] { Opaque(current.Value) }, Wanted(), _filled);
                        _offering = true;
                    }
                }
                else
                    ColorChannels.Shown(Opening(Wanted()), Wanted(), _filled);

                return _filled;
            }
        }

        /// <summary>Whether the values hold anything other than what the step was given.</summary>
        /// <remarks>By value rather than by spelling: a step given 5 opens showing 5.0, which is the same value and no change.</remarks>
        public bool Moved
        {
            get
            {
                if (_entered == null)
                    return false;

                var entered = new List<Literal>();
                ValueLine.Read(_entered, entered);
                if (entered.Count != _arrivedValues.Count)
                    return true;

                for (var i = 0; i < entered.Count; i++)
                {
                    if (!Same(entered[i], _arrivedValues[i]))
                        return true;
                }

                return false;
            }
        }

        /// <summary>The arguments <paramref name="said" /> gives, written as a line of values, each one for a list of names given as that name.</summary>
        /// <remarks>The answer comes as a line of text, which carries a chosen word as a bare word, and a bare word reads as text.</remarks>
        public List<Literal> Read(string said)
        {
            var fields = Wanted();
            var arguments = FieldSet.Read(said, fields);
            var at = 0;
            for (var f = 0; f < fields.Count && at < arguments.Count; f++)
            {
                if (fields[f].TakesSlots)
                    arguments[at] = Literal.Slots(Target.Slots(arguments[at].Said));

                at += fields[f].Covers;
            }

            return arguments;
        }

        /// <summary>
        /// Says what the chosen thing takes in <paramref name="strip" />, and what it opens holding, where either has changed; and
        /// puts the values beside the stages once something that can be finished is chosen. <paramref name="what" /> is the
        /// column of what is done, or null where it is not made; <paramref name="does" /> the column of what a trigger does, or
        /// null. <paramref name="chosenName" /> is what the chosen thing is listed as, <paramref name="title" /> heads the page
        /// long text is written on, <paramref name="about" /> says what it is, and <paramref name="finish" /> finishes the pick.
        /// </summary>
        public void Fill(PickStrip strip, StageColumn what, StageColumn does, string chosenName, string about, Func<string> title, ICommand finish)
        {
            strip.ArgumentsIn = _forEvents ? does ?? what : null;

            var shown = _complete() ? $"{_picked.Form} {_picked.Member}" : "";
            strip.ShowsArguments = what != null && shown.Length != 0;

            // The fields are compared too, since they change with the blocks: a hinge picked beside a rotor turns without a
            // direction.
            var fields = Wanted();
            Explained(fields);

            // Values that open on what the block holds are each facet's own, read off the one chosen.
            if (shown.Length != 0 && _picked.Facet.Length != 0 && OpensOnTheBlock(fields))
                shown = $"{shown} {_picked.Facet}";
            // Words read again in another language since they were shown are shown again.
            if (_shown == shown && Same(_shownFields, fields) && _worded == _editor.Rewordings)
                return;

            _shown = shown;
            _worded = _editor.Rewordings;
            _shownFields.Clear();
            _shownFields.AddRange(fields);

            // Read once, where a color is asked for: these are the colors this player paints with, only ever read.
            var colors = new List<Hsv>();
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Edits != FieldEditor.Color)
                    continue;

                Player.Colors(colors);
                break;
            }

            var made = strip.Arguments == null;
            var arguments = strip.Restate(shown, chosenName, fields, Filled, new ColorOffer(colors, _editor.Viewer, _editor.Preferred));
            Target.Reads(arguments.Form);
            if (_offering)
                arguments.Form.Offer();
            arguments.Describe(about);
            if (made)
            {
                _editor.Codex.Explain(arguments.About);
                arguments.Form.Edited += delegate
                {
                    if (_editor.Current != _page)
                        return;

                    _entered = arguments.Form.Answer;
                    arguments.Why = arguments.Form.Wrong;
                };

                arguments.Form.Submitted += delegate
                {
                    if (_editor.Current == _page && finish != null)
                        finish.Execute();
                };

                arguments.Form.Selects += field => Target.Select(field, null);
                arguments.Form.Opens += lines =>
                {
                    if (_editor.Current == _page)
                        _editor.Push(new TextPage(_editor, title(), lines.Text, lines.Longest, lines.Give));
                };
            }

            _entered = arguments.Form.Answer;

            // Nowhere to type is nowhere to keep the keyboard. Somebody who came for the values gets it in them, once.
            if (_opensHere && !_openedArguments && arguments.Form.Count > 0)
            {
                _openedArguments = true;
                arguments.Form.Take(_editor.Input);
            }
        }

        // The codex's own words for each option of the first block picked.
        void Explained(List<Field> fields)
        {
            var block = _picked.Blocks.Count == 0 ? null : _editor.World.FindEntity(_picked.Blocks[0]);
            if (block == null)
                return;

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                field.About = _editor.Codex.Option(block.Kind, field.Name);
                fields[i] = field;
            }
        }

        /// <summary>Whether any of <paramref name="fields" /> opens on what the block holds.</summary>
        static bool OpensOnTheBlock(IList<Field> fields)
        {
            foreach (var field in fields)
            {
                if (field.OpensOn != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// What a new step's <paramref name="fields" /> open holding: what the block holds now in the value each opens on, and
        /// nothing in the rest.
        /// </summary>
        List<Literal> Opening(IList<Field> fields)
        {
            var opened = new List<Literal>();
            foreach (var field in fields)
            {
                var current = field.OpensOn == null ? null : Vocabulary.Current(_editor.World, _picked.Blocks, field.OpensOn, _picked.Facet);
                for (var i = 0; i < field.Covers; i++)
                    opened.Add(current.HasValue && field.Covers == 1 ? Opaque(current.Value) : Literal.Of(""));
            }

            return opened;
        }

        static bool Same(IList<Field> held, IList<Field> fields)
        {
            if (held.Count != fields.Count)
                return false;

            for (var i = 0; i < held.Count; i++)
            {
                if (held[i].Name != fields[i].Name || held[i].Least != fields[i].Least
                    || held[i].Most != fields[i].Most || held[i].Edits != fields[i].Edits
                    || held[i].Entry != fields[i].Entry || held[i].Covers != fields[i].Covers)
                {
                    return false;
                }
            }

            return true;
        }

        static bool Same(Literal a, Literal b)
        {
            if (a.Kind == ValueKind.Slots || b.Kind == ValueKind.Slots)
                return a.Said == b.Said;

            var numbers = (a.Kind == ValueKind.Integer || a.Kind == ValueKind.Real)
                && (b.Kind == ValueKind.Integer || b.Kind == ValueKind.Real);

            return numbers ? a.Number == b.Number : a.Kind == b.Kind && a.Said == b.Said;
        }

        /// <summary><paramref name="value" /> as a step would give it: a color fully opaque, whatever the block reads back.</summary>
        static Literal Opaque(Literal value)
        {
            return value.Kind == ValueKind.Color ? Literal.Color(value.Red, value.Green, value.Blue, 255) : value;
        }
    }
}
