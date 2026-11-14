using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Features.Triggers.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers
{
    /// <summary>
    /// The work of a logic system's Triggers tab: a line for each trigger, worded as it reads, each part pressed to change what
    /// starts it or what it does, and the panel its rate is given in.
    /// </summary>
    public sealed class TriggersTab
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly KeyedChildren<Trigger, TriggerRow> _rows;
        readonly Func<CommandKind, Action, ICommand> _changes;
        readonly Func<string> _locked;

        LengthQuestion _rate;

        /// <summary>
        /// The Triggers tab of <paramref name="system" />, its lines in <paramref name="list" />, its changes made through commands
        /// <paramref name="changes" /> makes, and <paramref name="locked" /> saying why nothing can be changed where it cannot.
        /// </summary>
        public TriggersTab(SprocketEditor editor, AutomationSystem system, VStack list, Func<CommandKind, Action, ICommand> changes, Func<string> locked)
        {
            _editor = editor;
            _system = system;
            _changes = changes;
            _locked = locked;
            _rows = new KeyedChildren<Trigger, TriggerRow>(list.Children, delegate { return new TriggerRow(); });
        }

        /// <summary>The line of <paramref name="trigger" />.</summary>
        public TriggerRow Row(Trigger trigger) => _rows[trigger];

        /// <summary>Shows a line for each of <paramref name="triggers" />, in order, worded as each reads.</summary>
        /// <remarks>Walks each trigger's blocks for their names, so it runs only when the page is built.</remarks>
        public void Arrange(List<Trigger> triggers)
        {
            _rows.Show(triggers);
            foreach (var trigger in triggers)
                Word(_rows[trigger], trigger);
        }

        /// <summary>The panel a timer's rate is being given in, or null while none is open.</summary>
        public DurationPanel Rate => _rate != null && _rate.IsOpen ? _rate.Panel : null;

        /// <summary>Keeps the rate's panel under the part it was opened from.</summary>
        /// <remarks>Called every tick.</remarks>
        public void Hang()
        {
            if (_rate != null)
                _rate.Hang();
        }


        /// <summary>Opens the panel the rate of <paramref name="every" /> is given in, which changes it as it closes.</summary>
        void OpenRate(EveryTrigger every)
        {
            if (_rate != null)
                _rate.Finish(false);

            _rate = new LengthQuestion(_editor, every.Interval, true, () => RateChip(every),
                (interval, cancelled) =>
                {
                    if (!cancelled && interval.HasValue && interval.Value != every.Interval && _system.IndexOfTrigger(every) >= 0)
                        _editor.Do(_system, new SetCadence(every, every.Interval, interval.Value));
                });
            _rate.Hang();
        }

        /// <summary>The chip of <paramref name="every" />'s rate, or null where its row does not show one.</summary>
        Element RateChip(EveryTrigger every)
        {
            var row = _rows[every];
            if (row == null)
                return null;

            foreach (var start in row.Starts)
            {
                if (start is DurationSlot)
                    return row.ChipOf(start);
            }

            return null;
        }

        /// <summary>Words <paramref name="row" /> as <paramref name="trigger" /> reads, each part pressed to change it while the system may be changed.</summary>
        void Word(TriggerRow row, Trigger trigger)
        {
            row.Kind = Kind(trigger);
            row.Starts.Clear();
            row.Does.Clear();

            var moment = trigger as MomentTrigger;
            var watching = moment as WhenTrigger;
            if (watching != null && Vocabulary.Compares(watching, _editor.SlotBlocks.Events(_system, watching.Sources)))
            {
                Compared(row, watching);
            }
            else if (moment != null)
            {
                var all = watching != null && watching.Quantifier != Quantifier.Any;
                var blocks = _editor.Blocks(_system, moment.Sources);
                var slotted = _system.IsBlueprint;
                if (watching != null && slotted)
                    row.Starts.Add(Reading(watching, TestShape.Of(watching.Quantifier, false, true, true), false));

                row.Starts.Add(new EntitiesSlot(blocks, moment.Sources, all, MarkText.OfSlots(_system, moment.Sources), Starts(trigger, OpeningColumn.Blocks)));
                row.Starts.Add(new EventSlot(Happening(moment), Starts(trigger, OpeningColumn.What), all && (slotted || blocks.Count > 1)));
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                row.Starts.Add(new DurationSlot(every.Interval, _changes(CommandKind.OpenTrigger, () => OpenRate(every))));

            var watch = trigger as StateTrigger;
            if (watch != null)
                Watched(row, watch);

            Doing(row, trigger);

            row.Deletes = _changes(CommandKind.DeleteTrigger, () => _editor.Starts.Forget(_system, trigger));
            row.Copies = _changes(CommandKind.DuplicateTrigger, () => _editor.Starts.CopyTrigger(_system, trigger));
            row.Locked = _locked();
        }

        /// <summary>The kind of line <paramref name="trigger" /> is worded as.</summary>
        static TriggerKind Kind(Trigger trigger)
        {
            if (trigger is EveryTrigger)
                return TriggerKind.Every;

            return trigger is WhenTrigger || trigger is StateTrigger ? TriggerKind.When : TriggerKind.Signal;
        }

        /// <summary>Gives <paramref name="row" /> the parts of a when on one of the system's states: the state, how it compares, and with what.</summary>
        void Watched(TriggerRow row, StateTrigger watch)
        {
            var test = watch.Test;
            var opens = Starts(watch, OpeningColumn.Blocks);
            row.Starts.Add(new SystemStateSlot(test.State, opens));
            row.Starts.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(test.Relation) }, ComparisonQuestion.StateFields(),
                _changes(CommandKind.OpenTrigger, () => RelateWatch(watch)), ArgumentRole.Compared));
            row.Starts.Add(StateValueQuestion.Part(_system, test.State, test.Value, test.Other, opens, ArgumentRole.Checked));
        }

        /// <summary>Offers is and is not in a menu under the part pressed, and changes what <paramref name="watch" /> starts on to the one chosen.</summary>
        void RelateWatch(StateTrigger watch)
        {
            _editor.AskHere("");
            foreach (var related in new[] { Relation.Is, Relation.IsNot })
            {
                var relation = related;
                _editor.Answer(ComparisonQuestion.Said(relation), delegate
                {
                    var was = watch.Test;
                    if (relation == was.Relation)
                        return;

                    var now = was.Copy();
                    now.Relation = relation;
                    _editor.Do(_system, new SetWatch(watch, was, now));
                }, relation == watch.Test.Relation);
            }
        }

        /// <summary>Gives <paramref name="row" /> the parts of a when on a number.</summary>
        void Compared(TriggerRow row, WhenTrigger when)
        {
            var blocks = _editor.Blocks(_system, when.Sources);
            var value = when.Names(_editor.SlotBlocks.Actions(_system, when.Sources));

            // A line naming slots says how it reads them whatever they hold; one naming blocks, only where it names several.
            var slotted = _system.IsBlueprint;
            var shape = TestShape.Of(when.Quantifier, true, slotted || blocks.Count > 1, slotted, ComparisonQuestion.Join(value, when.Arguments));
            var reading = shape.IsRead ? Reading(when, shape, true) : null;
            var parameters = ComparisonQuestion.Parameters(value);
            var taking = !ComparisonQuestion.SaysAny(parameters, when.Arguments) ? null : new ArgumentsSlot(when.Arguments, parameters, Starts(when, OpeningColumn.Arguments), ArgumentRole.ReadWith);
            shape.Lay(row.Starts, reading, new ActionSlot(value, Starts(when, OpeningColumn.What)),
                new EntitiesSlot(blocks, when.Sources, shape.JoinsAll, MarkText.OfSlots(_system, when.Sources), Starts(when, OpeningColumn.Blocks)), taking);
            if (value == null)
                return;

            var fields = ComparisonQuestion.Fields(value, _editor.SlotBlocks.Events(_system, when.Sources), when.Arguments);
            var plural = shape.IsPlural;
            row.Starts.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(when.Relation) }, fields.GetRange(0, 1),
                _changes(CommandKind.OpenTrigger, () => Relate(when, fields[0], plural)), ArgumentRole.Compared, plural));
            row.Starts.Add(new ArgumentsSlot(new[] { when.Value }, fields.GetRange(1, fields.Count - 1), Starts(when, OpeningColumn.Arguments), ArgumentRole.Checked));
        }

        /// <summary>Offers the comparisons <paramref name="relation" /> takes in a menu under the part pressed, and changes <paramref name="when" /> to the one chosen.</summary>
        void Relate(WhenTrigger when, Field relation, bool plural)
        {
            _editor.AskHere("");
            for (var i = 0; i < relation.Words.Count; i++)
            {
                var related = ComparisonQuestion.Read(Literal.Name(relation.Means[i]));
                var said = plural ? SentenceText.Plural(relation.Words[i]) : relation.Words[i];
                _editor.Answer(said, delegate
                {
                    if (related == when.Relation)
                        return;

                    var now = when.Copy();
                    now.Relation = related;
                    _editor.Do(_system, new ChangeTrigger(when, now));
                }, related == when.Relation);
            }
        }

        /// <summary>
        /// The part saying how <paramref name="when" /> reads its blocks, in the words <paramref name="shape" /> says it with,
        /// pressed to choose another way, a total among them where it <paramref name="compares" /> a number.
        /// </summary>
        SentenceSlot Reading(WhenTrigger when, TestShape shape, bool compares)
        {
            var said = shape.Reading;
            return new ArgumentsSlot(new[] { Literal.Name(said) }, new[] { Field.Among("reading", new[] { said }, new[] { said }) },
                _changes(CommandKind.OpenTrigger, () => Quantify(when, compares)), ArgumentRole.Read);
        }

        /// <summary>
        /// Offers the ways <paramref name="when" /> can read its blocks in a menu under the part pressed, in the words the picker
        /// offers them in, a total among them where it <paramref name="compares" /> a number, and changes it to the one chosen.
        /// </summary>
        void Quantify(WhenTrigger when, bool compares)
        {
            _editor.AskHere("");
            for (var i = 0; i < SentenceText.Readings.Count; i++)
            {
                var reading = SentenceText.Readings[i];
                if (!compares && SentenceText.Totals(reading))
                    continue;

                _editor.Answer(SentenceText.Quantified(reading), delegate
                {
                    if (reading == when.Quantifier)
                        return;

                    var now = when.Copy();
                    now.Quantifier = reading;
                    _editor.Do(_system, new ChangeTrigger(when, now));
                }, reading == when.Quantifier);
            }
        }

        /// <summary>What happening <paramref name="moment" /> starts on, among what its blocks announce.</summary>
        EntityEvent Happening(MomentTrigger moment)
        {
            var events = _editor.SlotBlocks.Events(_system, moment.Sources);
            var eventId = Vocabulary.EventOf(_editor.Program, _editor.Runner.Bindings, _system, moment, _editor.StandIn(_system));
            return Vocabulary.Find(events, eventId);
        }

        /// <summary>Gives <paramref name="row" /> the parts of what <paramref name="trigger" /> does: a state it sets, a sequence it runs or stops, or an action.</summary>
        void Doing(TriggerRow row, Trigger trigger)
        {
            var set = trigger.Sets;
            if (set != null)
            {
                var opens = _changes(CommandKind.TriggerTarget, () => _editor.Starts.ChangeTrigger(_system, trigger, OpeningColumn.Nothing, false));
                row.DoesLead = SentenceText.Then(row.Kind, false, false, true);
                row.Does.Add(new SystemStateSlot(set.State, opens));
                row.Does.Add(StateValueQuestion.Part(_system, set.State, set.Value, set.From, opens, ArgumentRole.NewValue));
                return;
            }

            var act = trigger.Acts;
            if (act == null)
            {
                var sequence = _system.FindSequence(trigger.Runs);
                row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, true, false);
                row.Does.Add(new SequenceSlot(trigger.Runs, sequence == null || !sequence.HasName ? "" : sequence.Name,
                    _changes(CommandKind.TriggerTarget, () => _editor.Starts.ChangeTrigger(_system, trigger, OpeningColumn.Nothing, false))));

                return;
            }

            var chosen = act.Names(_editor.SlotBlocks.Actions(_system, act.Targets));

            var verb = act.Sets ? SentenceText.Switching(chosen, act.Arguments) : "";
            row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops, false, act.Sets && verb.Length == 0);
            if (verb.Length != 0)
                row.Does.Add(ArgumentsSlot.Switching(verb, Does(trigger, OpeningColumn.Arguments)));

            row.Does.Add(new ActionSlot(chosen, Does(trigger, OpeningColumn.What)));
            var blocks = _editor.Blocks(_system, act.Targets);
            var entities = new EntitiesSlot(blocks, act.Targets, true, MarkText.OfSlots(_system, act.Targets), Does(trigger, OpeningColumn.Blocks))
            {
                Screens = _editor.SlotBlocks.Screens(_system, act.Targets, blocks)
            };
            if (verb.Length != 0 || chosen == null || chosen.Wants == 0)
            {
                row.Does.Add(entities);
                return;
            }

            var fields = new List<Field>();
            chosen.Fields(fields);
            ArgumentsSlot.Lay(row.Does, entities, chosen.Join, act.Arguments, fields, Does(trigger, OpeningColumn.Arguments),
                act.Sets ? ArgumentRole.NewValue : ArgumentRole.Options,
                names => new EntitiesSlot(_editor.Blocks(_system, names), names, true, MarkText.OfSlots(_system, names), Does(trigger, OpeningColumn.Target)));
        }

        /// <summary>Opens what <paramref name="trigger" /> does at <paramref name="opens" />, while the system may be changed.</summary>
        ICommand Does(Trigger trigger, OpeningColumn opens)
        {
            return _changes(CommandKind.TriggerTarget, () => _editor.Starts.ChangeTrigger(_system, trigger, opens, true));
        }

        /// <summary>Opens what starts <paramref name="trigger" /> at <paramref name="opens" />, while the system may be changed.</summary>
        ICommand Starts(Trigger trigger, OpeningColumn opens)
        {
            return _changes(CommandKind.OpenTrigger, () => _editor.Starts.ChangeTrigger(_system, trigger, opens, false));
        }
    }
}
