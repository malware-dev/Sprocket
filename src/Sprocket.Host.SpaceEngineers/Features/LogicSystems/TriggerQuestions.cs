using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
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

            if (trigger is MomentTrigger || trigger is StateTrigger)
            {
                Watching(system, trigger, true, aim, origin, OpeningColumn.Blocks, false);
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


        /// <summary>
        /// Hands <paramref name="started" /> the trigger that starts on what <paramref name="answer" /> describes, in place of what
        /// starts on <paramref name="replacing" /> or null for a new trigger, keeping the signal slot of <paramref name="keeping" />
        /// where the picks are its to change; once asked, where the picks would change a slot something else uses.
        /// </summary>
        public void Starter(AutomationSystem system, PickAnswer answer, SignalTrigger keeping, IList<string> replacing,
            Action<MomentTrigger> started)
        {
            if (PicksSignal(system, answer))
            {
                started(SignalTrigger.Named(answer.Slots[0]));
                return;
            }

            if (answer.Form == ReachKind.Value)
            {
                var given = answer.Arguments;
                var provider = Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, ReachKind.Value);
                var taken = ComparisonQuestion.Taken(EntityAction.Value(Vocabulary.ActionsOf(_editor.World, answer.Targets), provider, answer.Chosen));
                var when = new WhenTrigger(answer.Chosen)
                {
                    Provider = provider,
                    Quantifier = answer.Quantifier,
                    Relation = given.Count > taken ? ComparisonQuestion.Read(given[taken]) : Relation.Is,
                    Value = given.Count > taken + 1 ? given[taken + 1] : Literal.Of(0)
                };

                for (var i = 0; i < taken && i < given.Count; i++)
                    when.Arguments.Add(given[i]);

                _editor.Picks.Name(system, answer, true, replacing, sources =>
                {
                    foreach (var source in sources)
                        when.Sources.Add(source);

                    started(when);
                });
                return;
            }

            MomentTrigger made = Vocabulary.Starter(
                Vocabulary.EventsOf(_editor.World, answer.Targets), answer.Chosen, answer.Quantifier == Quantifier.All);

            var signal = made as SignalTrigger;
            if (signal == null || signal.IsWatched)
            {
                _editor.Picks.Name(system, answer, true, replacing, named =>
                {
                    foreach (var name in named)
                        made.Sources.Add(name);

                    started(made);
                });
                return;
            }

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
            {
                started(made);
                return;
            }

            if (!system.IsBlueprint)
            {
                started(SignalTrigger.Named(SignalName(declared[0], at[0])));
                return;
            }

            if (_editor.StandIn(system) == null)
            {
                started(made);
                return;
            }

            _editor.Picks.NameSignal(system, answer.Targets, at[0], keeping?.Part ?? "", answer.SlotName, name => started(SignalTrigger.Named(name)));
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

            return new AnswerSignal(then, system, instance, required.Part, declared, at);
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
            var only = fresh ? null : trigger.Acts;
            aim.Finish = answer => _editor.Picks.Name(system, answer, false, only?.Targets, named =>
            {
                var act = new Act(answer.Chosen);
                act.Form = answer.Form;
                act.Provider = Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, answer.Form);
                foreach (var name in named)
                    act.Targets.Add(name);

                for (var i = 0; i < answer.Arguments.Count; i++)
                    act.Arguments.Add(answer.Arguments[i]);

                _editor.UnwindTo(origin);
                Points(system, trigger, act, fresh, null, start);
            });

            if (only != null)
            {
                if (aim.Following == null)
                {
                    aim.Following = new PickPage(_editor, _editor.Texts.PickSingleTask, false, _editor.Blocks(system, only.Targets),
                        only.ActionId, only.Form, answer => aim.Finish(answer),
                        arguments: only.Arguments, opens: aim.Opens, after: aim.Pick) { Slotted = system, Replacing = only.Targets };
                }

                aim.Following.Arrived(aim.Straight);
                aim.Straight = false;
                _editor.Push(aim.Following);
                return;
            }

            if (aim.Following == null)
            {
                aim.Following = new PickPage(_editor, _editor.Texts.PickSingleTask, false, new List<long>(), "", ReachKind.Native,
                    answer => aim.Finish(answer), opens: aim.Opens, after: aim.Pick) { Slotted = system };
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

            if (trigger is MomentTrigger || trigger is StateTrigger)
                Watching(system, trigger, false, aim, origin, opens, action);
        }

        /// <summary>
        /// Opens the selector of what starts <paramref name="was" />, a new trigger where <paramref name="fresh" />: on blocks, or
        /// on one of the logic system's states where it watches one, its first column's heading switching between those, and the
        /// slots the logic system already has; going straight on to what it does where <paramref name="action" /> says so.
        /// </summary>
        void Watching(AutomationSystem system, Trigger was, bool fresh, OutcomeColumn aim, int origin, OpeningColumn opens, bool action)
        {
            var kept = was as SignalTrigger;
            var keeps = !fresh && kept != null && !kept.IsWritten && system.IsBlueprint ? kept.Part : "";
            var source = new SourceSwitch(_editor,
                (choice, slots) =>
                {
                    var page = OnBlocks(system, was, fresh, aim, origin, opens, action, slots);
                    page.Source = choice;
                    page.Slotted = system;
                    page.ListsSlots = slots;
                    page.Keeps = keeps;
                    page.Replacing = fresh ? null : (was as MomentTrigger)?.Sources;
                    return page;
                },
                choice =>
                {
                    var page = OnState(system, was, fresh, aim, origin);
                    page.Source = choice;
                    return page;
                });

            var opened = source.Open(was is StateTrigger ? PickSource.States : PickSource.Blocks);
            _editor.Push(opened);
            if (!action)
                return;

            var picked = opened as PickPage;
            if (picked != null)
                picked.GoOn();

            var stated = opened as StatePage;
            if (stated != null)
                stated.GoOn();
        }

        /// <summary>The selector of a state <paramref name="was" /> is to start on, as a trigger made new where <paramref name="fresh" />, or changed in place.</summary>
        StatePage OnState(AutomationSystem system, Trigger was, bool fresh, OutcomeColumn aim, int origin)
        {
            var watch = was as StateTrigger;
            return new StatePage(_editor, system, SentenceText.WhenWord, false, watch?.Test, aim, (test, made) =>
            {
                if (fresh)
                {
                    var starting = watch ?? new StateTrigger(test);
                    starting.Test = test;
                    if (made != null && made.Type != null)
                        Starting(system, new AddType(made.Type, system.Types.Count));

                    if (made != null)
                        Starting(system, new AddState(made.State, system.States.Count));

                    Aimed(system, starting, aim, origin, true, null);
                    return;
                }

                if (watch != null)
                {
                    var change = new SetWatch(watch, watch.Test, test);
                    var start = made != null ? WithMade.State(system, made.State, made.Type, change) : change.Changes ? (SystemEdit)change : null;
                    Aimed(system, watch, aim, origin, false, start);
                    return;
                }

                _copies.Remove(was);
                var now = new StateTrigger(test);
                var swap = new ChangeTrigger(was, now);
                Aimed(system, now, aim, origin, false, made != null ? WithMade.State(system, made.State, made.Type, swap) : swap);
            });
        }

        /// <summary>
        /// The selector of the blocks and moment <paramref name="was" /> is to start on, as a trigger made new where
        /// <paramref name="fresh" />, or changed in place; opened on no blocks where it lists the <paramref name="slots" /> already made.
        /// </summary>
        PickPage OnBlocks(AutomationSystem system, Trigger was, bool fresh, OutcomeColumn aim, int origin, OpeningColumn opens, bool action,
            bool slots)
        {
            var moment = was as MomentTrigger;
            if (fresh)
            {
                return new PickPage(_editor, "When something happens", true,
                    new List<long>(), moment?.EventId ?? "", ReachKind.Native,
                    answer => Starter(system, answer, null, null, started =>
                    {
                        var made = started;
                        if (PicksSignal(system, answer))
                        {
                            Aimed(system, made, aim, origin, true, null);
                            return;
                        }

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
                    }),
                    true, aim: aim);
            }

            var when = moment as WhenTrigger;
            var compares = when != null && Vocabulary.Compares(when, _editor.SlotBlocks.Events(system, when.Sources));
            return new PickPage(_editor, "When something happens", true,
                moment == null || slots ? new List<long>() : _editor.Blocks(system, moment.Sources),
                moment == null ? "" : compares ? when.EventId : Vocabulary.EventOf(_editor.Program, _editor.Runner.Bindings, system, moment, _editor.StandIn(system)),
                compares ? ReachKind.Value : ReachKind.Native,
                answer =>
                {
                    _copies.Remove(was);

                    var keeping = moment as SignalTrigger;
                    if (keeping != null && keeping.IsWritten)
                        keeping = null;

                    Starter(system, answer, keeping, moment?.Sources, made =>
                    {
                        if (PicksSignal(system, answer))
                        {
                            var swap = new ChangeTrigger(was, made);
                            Aimed(system, swap.Changes ? made : was, aim, origin, false, swap.Changes ? swap : null);
                            return;
                        }

                        Claimed(system, was, made, answer, keeping, aim, origin);
                    });
                },
                true, when?.Quantifier ?? Quantifier.Any, compares ? ComparisonQuestion.Opening(when.Arguments, when.Relation, when.Value) : null,
                opens: action ? OpeningColumn.Nothing : opens, aim: aim);
        }

        /// <summary>Whether <paramref name="answer" /> picked a signal slot of <paramref name="system" /> by name, which is the whole of what starts the trigger.</summary>
        static bool PicksSignal(AutomationSystem system, PickAnswer answer)
        {
            var part = answer.Slots.Count == 1 ? system.FindPart(answer.Slots[0]) : null;
            return part != null && part.IsSignal;
        }

        /// <summary>
        /// Changes <paramref name="was" /> to start as <paramref name="made" /> does, on the buttons <paramref name="answer" />
        /// picked, keeping the signal slot of <paramref name="keeping" /> where the picks change it; asking first where another
        /// trigger has a button picked.
        /// </summary>
        void Claimed(AutomationSystem system, Trigger was, MomentTrigger made, PickAnswer answer, SignalTrigger keeping, OutcomeColumn aim,
            int origin)
        {
            _editor.Claims.Unless(system, made, answer, was, taken =>
            {
                if (_editor.Claims.Shares(taken, system))
                {
                    var taking = SignalTrigger.Named(taken.Trigger.Part);
                    var moved = new ChangeTrigger(was, taking);
                    Aimed(system, taking, aim, origin, false,
                        new TakeOverTrigger(Answering(system, taking, answer, moved) ?? moved, system, taken.Trigger));
                    return;
                }

                var answered = made as SignalTrigger;
                if (keeping != null && answered != null && !answered.IsWritten && system.IsBlueprint && answered.Part == keeping.Part)
                {
                    var refill = Answering(system, made, answer, null);
                    if (refill != null)
                        Aimed(system, was, aim, origin, false, taken == null ? refill : _editor.Claims.TakingOver(refill, taken));
                    else
                        Aimed(system, was, aim, origin, false, null);

                    return;
                }

                var change = new ChangeTrigger(was, made);
                if (!change.Changes && taken == null && !_editor.Picks.Reslots(system))
                {
                    Aimed(system, was, aim, origin, false, null);
                    return;
                }

                var edit = Answering(system, made, answer, change) ?? change;
                Aimed(system, made, aim, origin, false, taken == null ? edit : _editor.Claims.TakingOver(edit, taken));
            }, () => _editor.UnwindTo(origin));
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

            var part = new Part(_editor.Picks.NewSlotName(system));
            part.IsSignal = true;

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
    }
}
