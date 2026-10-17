using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Run = Sprocket.Host.SpaceEngineers.Sequences.Run;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>How a trigger is made, changed or copied: the questions asked, one page at a time, and the edit they come to.</summary>
    public sealed class TriggerQuestions
    {
        readonly SprocketEditor _editor;
        readonly List<SystemEdit> _starting = new List<SystemEdit>();
        readonly List<Trigger> _copies = new List<Trigger>();
        Trigger _replaced;
        int _replacedAt;

        /// <summary>The triggers made in <paramref name="editor" />.</summary>
        public TriggerQuestions(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>Asks the questions a new sequence needs, one screen at a time.</summary>
        public void StartSequence(AutomationSystem system, Trigger trigger)
        {
            _starting.Clear();
            _replaced = null;

            var origin = _editor.Depth;

            var aim = new OutcomeColumn(_editor, system, null);

            var moment = trigger as MomentTrigger;
            if (moment != null)
            {
                var picker = new PickPage(_editor, "When something happens", true,
                    new List<long>(), moment.EventId, ReachKind.Native,
                    answer =>
                    {
                        MomentTrigger made = Starter(system, answer, null, null);
                        _editor.Claims.Unless(system, made, answer, null, taken =>
                        {
                            if (_editor.Claims.Shares(taken, system))
                            {
                                made = SignalTrigger.Named(taken.Trigger.Part);
                                _replaced = taken.Trigger;
                                _replacedAt = system.IndexOfTrigger(taken.Trigger);
                                system.ReplaceTrigger(taken.Trigger, null);
                            }
                            else if (taken != null)
                            {
                                Starting(system, _editor.Claims.TakingOver(null, taken));
                            }

                            var answering = Answering(system, made, answer, null);
                            if (answering != null)
                                Starting(system, answering);

                            _editor.Program.Edited();
                            _editor.Program.Rewire();

                            Aimed(system, made, aim, origin, true, null);
                        }, () => { });
                    },
                    true, aim: aim);

                _editor.Push(picker);
                return;
            }

            var every = trigger as EveryTrigger;
            if (every == null)
                return;

            var timer = new TimerPage(_editor, every.Interval, aim, interval =>
            {
                every.Interval = interval;
                Aimed(system, every, aim, origin, true, null);
            });

            _editor.Push(timer);
        }

        /// <summary>Makes <paramref name="edit" /> now, and keeps it for the step that adds the trigger being made.</summary>
        void Starting(AutomationSystem system, SystemEdit edit)
        {
            edit.Apply(system);
            _starting.Add(edit);
        }


        /// <summary>The trigger that starts on what <paramref name="answer" /> describes.</summary>
        public MomentTrigger Starter(AutomationSystem system, PickAnswer answer, SignalTrigger keeping, IList<string> replacing)
        {
            if (answer.Form == ReachKind.Value)
            {
                var given = answer.Arguments;
                var when = new WhenTrigger(answer.Chosen)
                {
                    Provider = Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, ReachKind.Value),
                    Quantifier = answer.Quantifier,
                    Relation = given.Count > 0 ? ComparisonQuestion.Read(given[0]) : Relation.Is,
                    Value = given.Count > 1 ? given[1] : Literal.Of(0)
                };

                var sources = _editor.Picks.Named(system, answer.Targets, replacing);
                foreach (var source in sources)
                    when.Sources.Add(source);

                return when;
            }

            MomentTrigger made = Vocabulary.Starter(
                Vocabulary.EventsOf(_editor.World, answer.Targets), answer.Chosen, answer.Quantifier == Quantifier.All);

            var signal = made as SignalTrigger;
            if (signal == null || signal.IsWatched)
            {
                var named = _editor.Picks.Named(system, answer.Targets, replacing);
                foreach (var name in named)
                    made.Sources.Add(name);

                return made;
            }

            if (keeping != null && system.IsBlueprint)
                return SignalTrigger.Named(keeping.Part);

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
                return made;

            if (!system.IsBlueprint)
                return SignalTrigger.Named(SignalName(declared[0], at[0]));

            if (_editor.StandIn(system) == null)
                return made;

            var called = declared.Count == 1 ? SlotsFromPicks.Called(first) : first.Kind.DisplayName;
            return SignalTrigger.Named(system.Free(called));
        }

        /// <summary>The name of the variable that holds slot <paramref name="at" /> on the block <paramref name="block" />.</summary>
        string SignalName(string block, ToolbarSlot at)
        {
            var held = _editor.Program.FindSignalRefOnly(block, at);
            return held != null
                ? held.Name
                : _editor.Program.Free($"{block} {at.Slot.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        /// <summary>The edit that answers the signal part <paramref name="made" /> names with the buttons <paramref name="answer" /> picked, around <paramref name="then" />, or null.</summary>
        SystemEdit Answering(AutomationSystem system, MomentTrigger made, PickAnswer answer, SystemEdit then)
        {
            var required = made as SignalTrigger;
            if (required == null || required.IsWatched)
                return null;

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
                return null;

            if (!system.IsBlueprint)
            {
                _editor.Program.DeclareSignal(declared[0], at[0], required.Part);
                return then;
            }

            var instance = _editor.StandIn(system);
            if (instance == null)
                return null;

            return new AnswerSignal(then, system, instance, required.Part, Part.Nearest(new[] { first.Kind.Lineage }), declared, at);
        }

        /// <summary>The first block <paramref name="answer" /> picked, or null, with each picked block's declared name and toolbar slot added to the lists.</summary>
        public GridEntity Picked(PickAnswer answer, List<string> declared, List<ToolbarSlot> at)
        {
            var pressed = _editor.Claims.Pressed(answer);
            GridEntity first = null;
            for (var i = 0; i < answer.Targets.Count; i++)
            {
                GridEntity entity = _editor.World.FindEntity(answer.Targets[i]);
                if (entity == null)
                    continue;

                if (first == null)
                    first = entity;

                declared.Add(_editor.Program.Declare(_editor.World, entity).Name);
                at.Add(pressed[i]);
            }

            return first;
        }

        /// <summary>Carries out what a trigger was chosen to do, with the change to what starts it where there is one.</summary>
        void Aimed(AutomationSystem system, Trigger trigger, OutcomeColumn aim, int origin, bool fresh,
            SystemEdit start)
        {
            if (aim.Outcome == Outcome.One)
            {
                DoOneThing(system, trigger, origin, fresh, start, aim);
                return;
            }

            _editor.UnwindTo(origin);

            if (!fresh && !aim.Moved)
            {
                if (start != null)
                    _editor.Do(system, start);

                return;
            }

            if (aim.Outcome == Outcome.New)
            {
                RunNew(system, trigger, origin, fresh, start);
                return;
            }

            var sequence = system.FindSequence(aim.Sequence);
            if (sequence != null)
                Points(system, trigger, new Run(sequence.Number, aim.Stops), fresh, null, start);
            else if (start != null)
                _editor.Do(system, start);
        }

        /// <summary>Makes <paramref name="trigger" /> do <paramref name="does" />: added to <paramref name="system" /> where it is <paramref name="fresh" />, and otherwise changed as one edit with <paramref name="start" />.</summary>
        void Points(AutomationSystem system, Trigger trigger, Step does, bool fresh, Sequence made, SystemEdit start)
        {
            if (fresh)
            {
                trigger.Does = does;
                _editor.Do(system, new AddTrigger(_editor.Program, trigger, made, _starting, _replaced, _replacedAt));
                _starting.Clear();
                _replaced = null;
                _editor.Invalidate();
                return;
            }

            var point = new PointTrigger(_editor.Program, trigger, does, made);
            _editor.Do(system, start == null ? point : (SystemEdit)new Retrigger(start, point));
        }

        /// <summary>Makes the sequence this trigger runs, and opens it.</summary>
        void RunNew(AutomationSystem system, Trigger trigger, int origin, bool fresh, SystemEdit start)
        {
            _editor.UnwindTo(origin);

            var sequence = _editor.Program.AddSequence(system);
            sequence.Name = _editor.Name(sequence);

            Points(system, trigger, new Run(sequence.Number), fresh, sequence, start);

            _editor.PushNamed(new SequencePage(_editor, sequence));
        }

        /// <summary>Asks for the one thing the trigger does, on the page after its pick, and makes it do that.</summary>
        void DoOneThing(AutomationSystem system, Trigger trigger, int origin, bool fresh,
            SystemEdit start, OutcomeColumn aim)
        {
            aim.Finish = answer =>
            {
                var act = new Act(answer.Chosen);
                act.Form = answer.Form;
                act.Provider = Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, answer.Form);
                var named = _editor.Picks.Named(system, answer.Targets, fresh || trigger.Acts == null ? null : trigger.Acts.Targets);
                foreach (var name in named)
                    act.Targets.Add(name);

                for (var i = 0; i < answer.Arguments.Count; i++)
                    act.Arguments.Add(answer.Arguments[i]);

                _editor.UnwindTo(origin);
                Points(system, trigger, act, fresh, null, start);
            };

            var only = fresh ? null : trigger.Acts;
            if (only != null)
            {
                if (aim.Following == null)
                {
                    aim.Following = new PickPage(_editor, _editor.Texts.PickSingleTask, false, _editor.Blocks(system, only.Targets),
                        only.ActionId, only.Form, answer => aim.Finish(answer),
                        arguments: only.Arguments, opens: aim.Opens, after: aim.Pick);
                }

                aim.Following.Arrived(aim.Straight);
                aim.Straight = false;
                _editor.Push(aim.Following);
                return;
            }

            if (aim.Following == null)
            {
                aim.Following = new PickPage(_editor, _editor.Texts.PickSingleTask, false, new List<long>(), "", ReachKind.Native,
                    answer => aim.Finish(answer), opens: aim.Opens, after: aim.Pick);
            }

            aim.Following.Arrived(aim.Straight);
            aim.Straight = false;
            _editor.Push(aim.Following);
        }

        /// <summary>Takes a trigger out of a system.</summary>
        public void Forget(AutomationSystem system, Trigger trigger)
        {
            _editor.Do(system, new ForgetTrigger(system, trigger));
        }

        /// <summary>Opens the picker a trigger was made in, on what starts it and what it does now, and puts the answer in its place.</summary>
        public void ChangeTrigger(AutomationSystem system, Trigger trigger, OpeningColumn opens, bool action)
        {
            var origin = _editor.Depth;
            var aim = new OutcomeColumn(_editor, system, trigger);
            aim.Opens = action ? opens : OpeningColumn.Blocks;
            aim.Straight = action;

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                var rate = every.Interval;
                var timer = new TimerPage(_editor, rate, aim, interval =>
                    Aimed(system, every, aim, origin, false, interval == rate ? null : new SetCadence(every, rate, interval)));

                _editor.Push(timer);
                if (action)
                    timer.GoOn();

                return;
            }

            var moment = trigger as MomentTrigger;
            if (moment == null)
                return;

            var was = moment as WhenTrigger;

            var compares = was != null && Vocabulary.Compares(_editor.Runner.Bindings, was, SystemInstance.Through(_editor.StandIn(system), was.Sources));
            var picker = new PickPage(_editor, "When something happens", true,
                _editor.Blocks(system, moment.Sources), compares ? was.EventId : Vocabulary.EventOf(_editor.Program, _editor.Runner.Bindings, system, moment, _editor.StandIn(system)),
                compares ? ReachKind.Value : ReachKind.Native,
                answer =>
                {
                    _copies.Remove(moment);

                    var keeping = moment as SignalTrigger;
                    if (keeping != null && keeping.IsWritten)
                        keeping = null;

                    MomentTrigger made = Starter(system, answer, keeping, moment.Sources);
                    _editor.Claims.Unless(system, made, answer, moment, taken =>
                    {
                        if (_editor.Claims.Shares(taken, system))
                        {
                            var taking = SignalTrigger.Named(taken.Trigger.Part);
                            var moved = new ChangeTrigger(moment, taking);
                            Aimed(system, taking, aim, origin, false,
                                new TakeOverTrigger(Answering(system, taking, answer, moved) ?? moved, system, taken.Trigger));
                            return;
                        }

                        var answered = made as SignalTrigger;
                        if (keeping != null && answered != null && !answered.IsWritten && system.IsBlueprint)
                        {
                            var refill = Answering(system, made, answer, null);
                            if (refill != null)
                                Aimed(system, moment, aim, origin, false, taken == null ? refill : _editor.Claims.TakingOver(refill, taken));
                            else
                                Aimed(system, moment, aim, origin, false, null);

                            return;
                        }

                        var change = new ChangeTrigger(moment, made);
                        if (!change.Changes && taken == null && !_editor.Picks.Reslots(system))
                        {
                            Aimed(system, moment, aim, origin, false, null);
                            return;
                        }

                        var edit = Answering(system, made, answer, change) ?? change;
                        Aimed(system, made, aim, origin, false, taken == null ? edit : _editor.Claims.TakingOver(edit, taken));
                    }, () => _editor.UnwindTo(origin));
                },
                true, was?.Quantifier ?? Quantifier.Any, compares ? new[] { ComparisonQuestion.Of(was.Relation), was.Value } : null,
                opens: action ? OpeningColumn.Nothing : opens, aim: aim);

            _editor.Push(picker);
            if (action)
                picker.GoOn();
        }

        /// <summary>Adds a copy of <paramref name="trigger" /> to <paramref name="system" />, after its last trigger, doing what it does.</summary>
        public void CopyTrigger(AutomationSystem system, Trigger trigger)
        {
            var signal = trigger as SignalTrigger;
            var shown = system.IsBlueprint ? _editor.StandIn(system) : null;
            if (signal == null || signal.IsWritten || shown == null)
            {
                _editor.Do(system, new AddTrigger(_editor.Program, Automation.Copied(trigger, null), null, new List<SystemEdit>(), null, -1,
                    ChangeKind.CopyTrigger));

                return;
            }

            var part = new Part(FreeSlotName(system));
            part.IsSignal = true;
            part.Expects = Expected(system, signal);

            _editor.Picks.Make(system, shown, part);

            var copy = Automation.Copied(trigger, part.Id);
            _editor.Do(system, new AddTrigger(_editor.Program, copy, null, new List<SystemEdit>(), null, -1, ChangeKind.CopyTrigger));
            _copies.Add(copy);
        }

        /// <summary>Whether <paramref name="trigger" /> is a button trigger's copy that has no start picked yet.</summary>
        public bool Copied(Trigger trigger)
        {
            return _copies.Contains(trigger);
        }

        /// <summary>The first slot name of the numbered kind that <paramref name="system" /> does not use, counting on from how many slots it has.</summary>
        string FreeSlotName(AutomationSystem system)
        {
            for (var number = system.Parts.Count + 1;; number++)
            {
                var name = Template.Fill(_editor.Texts.SlotNumbered, "number", number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (system.FindPart(name) == null)
                    return name;
            }
        }

        /// <summary>The nearest type every slot <paramref name="trigger" /> starts from expects, or empty for any block.</summary>
        string Expected(AutomationSystem system, MomentTrigger trigger)
        {
            var lineages = new List<IList<string>>();
            for (var i = 0; i < trigger.Sources.Count; i++)
            {
                var part = system.FindPart(trigger.Sources[i]);
                if (part == null)
                    continue;

                if (part.Expects.Length == 0)
                    return "";

                lineages.Add(Lineage(part.Expects));
            }

            return Part.Nearest(lineages);
        }

        /// <summary>The type <paramref name="type" /> and each type the game builds it on, nearest first.</summary>
        IList<string> Lineage(string type)
        {
            var entities = _editor.World.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                var lineage = entities[i].Kind.Lineage;
                var at = lineage.IndexOf(type);
                if (at >= 0)
                {
                    var from = new List<string>();
                    for (var k = at; k < lineage.Count; k++)
                        from.Add(lineage[k]);

                    return from;
                }
            }

            return new List<string> { type };
        }
    }
}
