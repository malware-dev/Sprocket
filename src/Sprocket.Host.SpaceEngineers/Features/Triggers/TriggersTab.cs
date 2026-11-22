using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Features.Triggers.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
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
        readonly KeyedChildren<object, Element> _rows;
        readonly List<object> _keys = new List<object>();
        readonly CategoryWork _categories;
        readonly Func<CommandKind, Action, ICommand> _changes;
        readonly Func<string> _locked;

        LengthQuestion _rate;
        ValuesQuestion _values;

        /// <summary>
        /// The Triggers tab of <paramref name="system" />, its lines and their categories in <paramref name="list" />, its changes
        /// made through commands <paramref name="changes" /> makes, and <paramref name="locked" /> saying why nothing can be changed
        /// where it cannot.
        /// </summary>
        public TriggersTab(SprocketEditor editor, AutomationSystem system, CategoryList list, Func<CommandKind, Action, ICommand> changes, Func<string> locked)
        {
            _editor = editor;
            _system = system;
            _changes = changes;
            _locked = locked;
            _rows = new KeyedChildren<object, Element>(list.Children, Made);
            _categories = new CategoryWork(editor,
                new Arranger(editor, system, owner => owner.TriggerList, ChangeKind.MoveTrigger, ChangeKind.MoveTriggerCategory), changes, false);
            list.FnAllowedDropIndexes = index => Arranger.Landings(_keys, index);
            list.FnCanIndent = (from, to) => Arranger.Nests(_keys, from, to);
            list.FnIsIndented = index => Arranger.StandsIn(_keys, index);
            list.Reordered += moved =>
            {
                var move = changes(CommandKind.MoveTrigger, () => _categories.Arranger.Move(_keys, moved.From, moved.To, moved.IsIndented));
                if (move != null)
                    move.Execute();
            };
        }

        /// <summary>The row <paramref name="key" /> is shown as: a trigger's line, a category's heading or its end.</summary>
        static Element Made(object key)
        {
            if (key is Trigger)
                return new TriggerRow();

            return key is CategoryEnd ? (Element)new CategoryEndRow() : new CategoryRow();
        }

        /// <summary>Adds an empty category under every trigger, whose name opens for typing once its row stands.</summary>
        public void AddCategory()
        {
            _categories.Add();
        }

        /// <summary>The line of <paramref name="trigger" />.</summary>
        public TriggerRow Row(Trigger trigger) => _rows[trigger] as TriggerRow;

        /// <summary>
        /// Shows a line for each trigger, in order under their categories, worded as each reads in <paramref name="texts" />; moved,
        /// and the categories changed, where <paramref name="editable" />, and why not in <paramref name="locked" /> otherwise.
        /// </summary>
        /// <remarks>Walks each trigger's blocks for their names, so it runs only when the page is built.</remarks>
        public void Arrange(Texts texts, bool editable, string locked)
        {
            CategoryList.Listed(_system.TriggerList, _keys, true, editable);
            _rows.Show(_keys);
            for (var i = 0; i < _keys.Count; i++)
            {
                var trigger = _keys[i] as Trigger;
                if (trigger != null)
                {
                    var row = (TriggerRow)_rows[trigger];
                    row.Arranged = true;
                    row.Indented = trigger.Category.Length > 0;
                    row.Grips(editable, editable ? texts.Codex.TriggerDrag : locked);
                    Word(row, trigger);
                    Readable(row.Starts);
                    Readable(row.Does);
                    continue;
                }

                var heading = _keys[i] as CategoryHeading;
                if (heading != null)
                {
                    _categories.Heading(texts, (CategoryRow)_rows[heading], heading, Arranger.Carries(_keys, i), editable, locked,
                        texts.Codex.CategoryDragTriggers, texts.Codex.CategoryRemoveTriggers);
                    continue;
                }

                var end = _keys[i] as CategoryEnd;
                if (end != null)
                    End(texts, (CategoryEndRow)_rows[end], end, i);
            }
        }

        /// <summary>
        /// The ways a new trigger can start, in <paramref name="texts" />, each placed by <paramref name="placed" /> once made, or
        /// put under every other where it is null.
        /// </summary>
        public AddOption Adds(Texts texts, Func<Trigger, Arrangement> placed)
        {
            return new AddOption(texts.SystemTrigger, null, new[]
            {
                new AddOption(SentenceText.WhenWord, texts.Codex.SystemWhenAbout,
                    _changes(CommandKind.StartWhen, () => _editor.Starts.StartSequence(_system, new WhenTrigger(""), placed))),
                new AddOption(SentenceText.EveryWord, texts.Codex.SystemEveryAbout,
                    _changes(CommandKind.StartEvery, () => _editor.Starts.StartSequence(_system, new EveryTrigger(_editor.NewLength), placed)))
            });
        }

        /// <summary>Shows the end <paramref name="end" /> of a category, at <paramref name="at" /> of the rows, as the way to add a trigger into the category.</summary>
        void End(Texts texts, CategoryEndRow row, CategoryEnd end, int at)
        {
            row.Category = end.Name;
            row.Adds.Show(Adds(texts, made => _categories.Arranger.Placed(_keys, at, made)));
        }

        /// <summary>The panel a timer's rate is being given in, or null while none is open.</summary>
        public DurationPanel Rate => _rate != null && _rate.IsOpen ? _rate.Panel : null;

        /// <summary>The panel the values a trigger starts on are being given in, or null while none is open.</summary>
        public ValuesQuestion Giving => _values != null && _values.IsOpen ? _values : null;

        /// <summary>Keeps the rate's panel under the part it was opened from.</summary>
        /// <remarks>Called every tick.</remarks>
        public void Hang()
        {
            if (_rate != null)
                _rate.Hang();

            if (_values != null)
                _values.Hang();
        }

        // Has each part of slots given an expression show it as it reads.
        void Readable(List<SentenceSlot> slots)
        {
            foreach (var slot in slots)
                (slot as ArgumentsSlot)?.ShowExpressions(Shown);
        }

        // How expression reads where it stands in a trigger, or null where there is none.
        string Shown(Expression expression) => expression == null ? null : ExpressionShown.Of(_editor, _system, expression);

        /// <summary>Opens the panel the rate of <paramref name="every" /> is given in, which changes it as it closes.</summary>
        void OpenRate(EveryTrigger every)
        {
            if (_rate != null)
                _rate.Finish(false);

            // A rate that is an expression is changed on the page the expression is built on, which has its words.
            if (every.IntervalExpression != null)
            {
                BuildRate(every);
                return;
            }

            _rate = new LengthQuestion(_editor, _system, every.Interval, every.IntervalSetting, true, () => RateChip(every),
                (interval, setting, cancelled) =>
                {
                    if (cancelled || setting.Length == 0 && !interval.HasValue || _system.IndexOfTrigger(every) < 0)
                        return;

                    var now = setting.Length > 0 || !interval.HasValue ? every.Interval : interval.Value;
                    if (now != every.Interval || setting != every.IntervalSetting)
                        _editor.Do(_system, new SetCadence(every, every.Interval, every.IntervalSetting, now, setting, every.IntervalExpression));
                },
                () =>
                {
                    _rate.Finish(false);
                    BuildRate(every);
                });
            _rate.Hang();
        }

        /// <summary>Opens the page the rate of <paramref name="every" /> is built on as an expression, which changes it as one edit when done.</summary>
        void BuildRate(EveryTrigger every)
        {
            var start = every.IntervalExpression != null ? Literal.Worked(every.IntervalExpression)
                : every.IntervalSetting.Length > 0 ? Literal.Setting(every.IntervalSetting)
                : Literal.Of(every.Interval);
            _editor.Push(new ExpressionPage(_editor, _system, "duration", "", start, built =>
            {
                if (_system.IndexOfTrigger(every) < 0)
                    return;

                var edit = built.IsExpression ? new SetCadence(every, every.Interval, every.IntervalSetting, every.Interval, "", every.IntervalExpression, built.Tree)
                    : built.IsSetting ? new SetCadence(every, every.Interval, every.IntervalSetting, every.Interval, built.Text, every.IntervalExpression)
                    : new SetCadence(every, every.Interval, every.IntervalSetting, built.Length, "", every.IntervalExpression);
                _editor.Do(_system, edit);
            }));
        }

        /// <summary>The chip of <paramref name="every" />'s rate, or null where its row does not show one.</summary>
        Element RateChip(EveryTrigger every)
        {
            var row = Row(every);
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
                row.Starts.Add(new DurationSlot(every.Interval, every.IntervalSetting, Shown(every.IntervalExpression), _changes(CommandKind.OpenTrigger, () => OpenRate(every))));

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
            row.Starts.Add(StateValueQuestion.Part(_system, test.State, test.Value, test.Other,
                _changes(CommandKind.OpenTrigger, () => CompareWatch(watch, () => Chip(watch, ArgumentRole.Checked))), ArgumentRole.Checked));
        }

        /// <summary>
        /// Offers the values the state <paramref name="watch" /> watches can hold in a menu under the part pressed, and changes what
        /// it starts on to the one chosen; the value of another state opens the selector.
        /// </summary>
        void CompareWatch(StateTrigger watch, Func<Element> part)
        {
            var test = watch.Test;
            StateValueQuestion.Offer(_editor, _system, test.State, test.Value, test.Other, part, value =>
            {
                var now = test.Copy();
                now.Value = value;
                now.Other = "";
                _editor.Do(_system, new SetWatch(watch, test, now));
            }, () => _editor.Starts.ChangeTrigger(_system, watch, OpeningColumn.Blocks));
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
            var taking = !ComparisonQuestion.SaysAny(parameters, when.Arguments)
                ? null
                : new ArgumentsSlot(when.Arguments, parameters, _changes(CommandKind.OpenTrigger, () => Compare(when, parameters, ArgumentRole.ReadWith)),
                    ArgumentRole.ReadWith);
            shape.Lay(row.Starts, reading, new ActionSlot(value, Starts(when, OpeningColumn.What)),
                new EntitiesSlot(blocks, when.Sources, shape.JoinsAll, MarkText.OfSlots(_system, when.Sources), Starts(when, OpeningColumn.Blocks)), taking);
            if (value == null)
                return;

            var fields = ComparisonQuestion.Fields(value, _editor.SlotBlocks.Events(_system, when.Sources), when.Arguments);
            var plural = shape.IsPlural;
            row.Starts.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(when.Relation) }, fields.GetRange(0, 1),
                _changes(CommandKind.OpenTrigger, () => Relate(when, fields[0], plural)), ArgumentRole.Compared, plural));
            var checks = fields.GetRange(1, fields.Count - 1);
            row.Starts.Add(new ArgumentsSlot(new[] { when.Value }, checks,
                _changes(CommandKind.OpenTrigger, () => Compare(when, checks, ArgumentRole.Checked)), ArgumentRole.Checked));
        }

        /// <summary>
        /// Opens the panel the values <paramref name="when" /> starts on are given in, asked for by <paramref name="fields" />: what
        /// it compares with where they are <paramref name="holds" /> <see cref="ArgumentRole.Checked" />, what it reads with
        /// otherwise; hung from the part saying them, and changing the trigger as it closes. The selector where a value is chosen
        /// on a screen of its own.
        /// </summary>
        void Compare(WhenTrigger when, List<Field> fields, ArgumentRole holds)
        {
            if (ValuesQuestion.Chooses(fields))
            {
                _editor.Starts.ChangeTrigger(_system, when, OpeningColumn.Arguments);
                return;
            }

            var checks = holds == ArgumentRole.Checked;
            var given = checks ? new[] { when.Value } : when.Arguments;
            Action<string, List<Literal>, bool> answered = (said, held, cancelled) =>
            {
                if (cancelled || _system.IndexOfTrigger(when) < 0)
                    return;

                var now = when.Copy();
                ComparisonQuestion.Give(now.Arguments, FieldSet.Read(said, fields, held), checks, value => now.Value = value);
                _editor.Do(_system, new ChangeTrigger(when, now));
            };
            if (ValuesQuestion.Offer(_editor, fields, given, said => answered(said, new List<Literal>(given), false)))
                return;

            if (_values != null)
                _values.Finish(true);

            _values = new ValuesQuestion(_editor, _system, fields, given, () => Chip(when, holds), answered);
            _values.Hang();
        }

        /// <summary>
        /// The chip of the first part of what starts <paramref name="trigger" /> saying values that are <paramref name="holds" /> to
        /// it; null where its row shows none.
        /// </summary>
        Element Chip(Trigger trigger, ArgumentRole holds)
        {
            var row = Row(trigger);
            if (row == null)
                return null;

            foreach (var part in row.Starts)
            {
                if ((part as ArgumentsSlot)?.Holds == holds)
                    return row.ChipOf(part);
            }

            return null;
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

        /// <summary>Gives <paramref name="row" /> the parts of what <paramref name="trigger" /> does: the sequence it runs or stops.</summary>
        void Doing(TriggerRow row, Trigger trigger)
        {
            var sequence = _system.FindSequence(trigger.Runs);
            row.DoesLead = SentenceText.Then(row.Kind, trigger.Stops);
            row.Does.Add(new SequenceSlot(trigger.Runs, sequence == null || !sequence.HasName ? "" : sequence.Name,
                _changes(CommandKind.TriggerTarget, () => _editor.Starts.ChangeTrigger(_system, trigger, OpeningColumn.Nothing))));
        }

        /// <summary>Opens what starts <paramref name="trigger" /> at <paramref name="opens" />, while the system may be changed.</summary>
        ICommand Starts(Trigger trigger, OpeningColumn opens)
        {
            return _changes(CommandKind.OpenTrigger, () => _editor.Starts.ChangeTrigger(_system, trigger, opens));
        }
    }
}
