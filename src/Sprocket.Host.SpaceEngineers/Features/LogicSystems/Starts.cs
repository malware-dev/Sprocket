using System;
using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Blocks;
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
    public sealed class Starts
    {
        readonly SprocketEditor _editor;
        /// <summary>What adding a trigger has settled while its blocks were picked, made already, for the step that adds it to carry.</summary>
        readonly List<SystemEdit> _starting = new List<SystemEdit>();
        /// <summary>The copies of button triggers whose start nobody has picked yet, whose lines say why they have a slot of their own.</summary>
        readonly List<Trigger> _copies = new List<Trigger>();
        /// <summary>The trigger the one being added takes the place of, or null; and where it stood.</summary>
        Trigger _replaced;
        int _replacedAt;

        /// <summary>The triggers made in <paramref name="editor" />.</summary>
        public Starts(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// Asks the questions a new sequence needs, one screen at a time.
        /// </summary>
        /// <remarks>
        /// Answering "when something happens" and then being handed a half-empty
        /// sentence to poke at is a stall, so the next question is asked straight
        /// away: what should it listen to, then what should it do.
        ///
        /// Going back retreats one question, all the way out to where the user
        /// started. It is never repurposed as "finished" - moving on is its own
        /// action, so somebody who genuinely wants to retreat still has somewhere to
        /// press.
        /// </remarks>
        public void StartSequence(AutomationSystem system, Trigger trigger)
        {
            _starting.Clear();
            _replaced = null;

            // Where to unwind to once the questions are answered. The system's screen, where the starters
            // are, is already underneath, so everything the questions pushed is dropped and the finished
            // sequence lands directly on it.
            var origin = _editor.Depth;

            // A new sequence is what a trigger usually does, and one being made does nothing yet.
            var aim = new Aim(_editor, system, null);

            var moment = trigger as MomentTrigger;
            if (moment != null)
            {
                // Nothing picked yet: the sequence is being made, so there is nothing to resolve.
                var picker = new PickPage(_editor, "When something happens", true,
                    new List<long>(), moment.EventId, Nature.Native,
                    answer =>
                    {
                        // Nothing to record: the sequence does not exist yet, so there is no
                        // history for it and nothing to take back to - which is why the trigger
                        // losing the button here loses it outright, the way the bin on its row is
                        // the way back from making one at all.
                        MomentTrigger made = Starter(system, answer, null, null);
                        _editor.Claims.Unless(system, made, answer, null, taken =>
                        {
                            // Settled now, so the questions that follow see it, and recorded, so adding the trigger takes it
                            // all back as one step.
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


        /// <summary>
        /// What somebody has just described, as the trigger that makes it happen.
        /// </summary>
        /// <remarks>
        /// The one place the two arrangements are told apart, and nobody is asked: picking a button means the construct
        /// already announces it and Sprocket wires a slot, picking a door means Sprocket watches for it. See
        /// <see cref="Vocabulary.Starter" />. In a system blueprint a button names a signal slot, new or, picked again for
        /// <paramref name="keeping" />, that trigger's own; the slot is taken and answered once the question of whether
        /// the buttons are free is answered - see <see cref="Answering" />. In a system it names the variable holding the
        /// first button picked, defined at that answer too. Blocks picked again for a trigger watching <paramref name="replacing" />
        /// keep its slot where they are one block of the kind it expects, as a step's do.
        /// </remarks>
        public MomentTrigger Starter(AutomationSystem system, PickAnswer answer, SignalTrigger keeping, IList<string> replacing)
        {
            // A number on the blocks starts it as it comes to compare as asked.
            if (answer.Form == Nature.Value)
            {
                var given = answer.Arguments;
                var when = new WhenTrigger(answer.Chosen)
                {
                    Provider = Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, Nature.Value),
                    Quantifier = answer.Quantifier,
                    Relation = given.Count > 0 ? Comparing.Read(given[0]) : Relation.Is,
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

            var called = declared.Count == 1 ? Picks.Called(first) : first.Kind.DisplayName;
            return SignalTrigger.Named(system.Free(called));
        }

        /// <summary>
        /// What the variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names is called: its
        /// own name where there is one, and the block's with the slot's number after it otherwise.
        /// </summary>
        string SignalName(string block, ToolbarSlot at)
        {
            var held = _editor.Program.FindSignalRefOnly(block, at);
            return held != null
                ? held.Name
                : _editor.Program.Free($"{block} {at.Slot.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// The edit that answers the signal part <paramref name="made" /> names in the blockset shown with the buttons
        /// <paramref name="answer" /> picked, taking the part where it is new, around <paramref name="then" />; null where
        /// <paramref name="made" /> names no part or nothing was picked.
        /// </summary>
        /// <remarks>
        /// Made once the question about the buttons is answered, so leaving them alone leaves no part behind. A system's
        /// trigger is a variable of the program's, defined here for the first button picked, and <paramref name="then" /> is
        /// all there is to do.
        /// </remarks>
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

        /// <summary>The blocks <paramref name="answer" /> picked, as the construct declares them, each with the toolbar slot its press is answered on; the first of them, or null.</summary>
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

        /// <summary>
        /// Carries out what a trigger was chosen to do, with the change to what starts it where there
        /// is one.
        /// </summary>
        /// <remarks>
        /// A trigger being changed that goes on doing what it did has only its start to change, and one
        /// that did nothing takes what the column opened on. What starts it goes in with what it does,
        /// as one edit.
        /// <para>
        /// A single action is chosen on the page after, in the same pick, and nothing is kept until it
        /// is. A trigger being made does not exist until then, so backing out leaves nothing behind.
        /// </para>
        /// </remarks>
        void Aimed(AutomationSystem system, Trigger trigger, Aim aim, int origin, bool fresh,
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

        /// <summary>
        /// Where every choice of what a trigger does lands.
        /// </summary>
        /// <remarks>
        /// A trigger being made is not yet in the system, and nothing has been done that could be
        /// taken back - the way back from making one is the bin on its row. A trigger being changed
        /// is already there, so this is an edit, and it is the system's to walk back, together with
        /// <paramref name="start" /> where what starts it changed too.
        /// </remarks>
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

        /// <summary>
        /// Makes the sequence this trigger runs, and opens it.
        /// </summary>
        /// <remarks>Made in a system switched off, as everything in one is, so a trigger with no steps behind it is never live.</remarks>
        void RunNew(AutomationSystem system, Trigger trigger, int origin, bool fresh, SystemEdit start)
        {
            // The picker goes with the answer. Left on the stack, backing out of the sequence would
            // land on the choice of what it does, for a trigger that already does something.
            _editor.UnwindTo(origin);

            var sequence = _editor.Program.AddSequence(system);
            sequence.Name = _editor.Name(sequence);

            Points(system, trigger, new Run(sequence.Number), fresh, sequence, start);

            _editor.PushNamed(new SequencePage(_editor, sequence));
        }

        /// <summary>
        /// Asks what one thing the trigger should do, as the next page of the pick what it does was
        /// chosen in, and makes it do exactly that.
        /// </summary>
        /// <remarks>
        /// What it holds is an act of its own. Where the block has an action of the game's own for it,
        /// the host writes that into the button and Sprocket is not part of the press - see
        /// <c>SprocketUnit</c>, which decides that by looking at the act rather than by being told.
        /// <para>
        /// A trigger that already does one thing opens on it, and finishing puts the act chosen in its
        /// place, together with <paramref name="start" />.
        /// </para>
        /// <para>
        /// The page is kept on <paramref name="aim" /> while the single action stays chosen, so going
        /// back to look at what starts the trigger and on again finds it as it was left. What finishing
        /// it does is set afresh every time, since what starts the trigger may have changed between.
        /// </para>
        /// </remarks>
        void DoOneThing(AutomationSystem system, Trigger trigger, int origin, bool fresh,
            SystemEdit start, Aim aim)
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
                aim.Following = new PickPage(_editor, _editor.Texts.PickSingleTask, false, new List<long>(), "", Nature.Native,
                    answer => aim.Finish(answer), opens: aim.Opens, after: aim.Pick);
            }

            aim.Following.Arrived(aim.Straight);
            aim.Straight = false;
            _editor.Push(aim.Following);
        }

        /// <summary>
        /// Takes a trigger out of a system.
        /// </summary>
        /// <remarks>
        /// No question asked, because the system's arrows are the answer to a deletion nobody meant -
        /// and a question in front of every one of them is how people learn to press through
        /// questions. A sequence deleted from its own screen still asks: that is work somebody can
        /// see, and its way back would be on a screen that went with it.
        /// </remarks>
        public void Forget(AutomationSystem system, Trigger trigger)
        {
            _editor.Do(system, new ForgetTrigger(system, trigger));
        }

        /// <summary>
        /// Opens the picker a trigger was made in, on what starts it and what it does now, and puts
        /// the answer in its place.
        /// </summary>
        /// <remarks>
        /// Wherever the trigger's row was pressed - what starts it, or what it does - because the two
        /// are chosen side by side. A trigger that listens for a moment stays one: turning a repeat
        /// into a moment is deleting it and making the one that was meant.
        /// <para>
        /// Whatever somebody wrote about it is carried over, and what it does is changed only where a
        /// different choice was made.
        /// </para>
        /// <para>
        /// It opens on the column of the part pressed. A part of the single action a trigger does is
        /// on the page after, so the picker goes straight on to that page, where the action was made.
        /// </para>
        /// </remarks>
        public void ChangeTrigger(AutomationSystem system, Trigger trigger, Opening opens, bool action)
        {
            // Back to the screen the row is on, which is where the answer lands.
            var origin = _editor.Depth;
            var aim = new Aim(_editor, system, trigger);
            aim.Opens = action ? opens : Opening.Blocks;
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

            // A when comparing a number opens on that number and how it compares; anything else on its moment.
            var compares = was != null && Vocabulary.Compares(_editor.Runner.Bindings, was, SystemInstance.Through(_editor.StandIn(system), was.Sources));
            var picker = new PickPage(_editor, "When something happens", true,
                _editor.Blocks(system, moment.Sources), compares ? was.EventId : Vocabulary.EventOf(_editor.Program, _editor.Runner.Bindings, system, moment, _editor.StandIn(system)),
                compares ? Nature.Value : Nature.Native,
                answer =>
                {
                    // The first pick on a copy is the answer its line was asking for.
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

                        // A button picked again for a button trigger answers the same signal part, so the trigger is kept:
                        // its toolbar slots name it.
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

                        // A trigger left as it was by a block traded within its slot still changes that slot.
                        var change = new ChangeTrigger(moment, made);
                        if (!change.Changes && taken == null && !_editor.Picks.Reslots(system))
                        {
                            Aimed(system, moment, aim, origin, false, null);
                            return;
                        }

                        // One edit either way, because it is one answer to one question.
                        var edit = Answering(system, made, answer, change) ?? change;
                        Aimed(system, made, aim, origin, false, taken == null ? edit : _editor.Claims.TakingOver(edit, taken));
                    }, () => _editor.UnwindTo(origin));
                },
                true, was?.Quantifier ?? Quantifier.Any, compares ? new[] { Comparing.Of(was.Relation), was.Value } : null,
                opens: action ? Opening.Nothing : opens, aim: aim);

            _editor.Push(picker);
            if (action)
                picker.GoOn();
        }

        /// <summary>Adds a copy of <paramref name="trigger" /> to <paramref name="system" />, after its last trigger, doing what it does.</summary>
        /// <remarks>
        /// A button starts only one trigger, so a button trigger's copy starts from a new, empty signal slot of the kinds the
        /// original's expects, and its line says so until the first pick on it. Every other trigger is copied whole.
        /// </remarks>
        public void CopyTrigger(AutomationSystem system, Trigger trigger)
        {
            var signal = trigger as SignalTrigger;
            var shown = system.IsBlueprint ? _editor.StandIn(system) : null;
            if (signal == null || signal.IsWritten || shown == null)
            {
                _editor.Do(system, new AddTrigger(_editor.Program, Automation.Copied(trigger, null), null, new List<SystemEdit>(), null, -1,
                    Change.CopyTrigger));

                return;
            }

            var part = new Part(FreeSlotName(system));
            part.IsSignal = true;
            part.Expects = Expected(system, signal);

            _editor.Picks.Make(system, shown, part);

            var copy = Automation.Copied(trigger, part.Id);
            _editor.Do(system, new AddTrigger(_editor.Program, copy, null, new List<SystemEdit>(), null, -1, Change.CopyTrigger));
            _copies.Add(copy);
        }

        /// <summary>Whether <paramref name="trigger" /> is a copy of a button trigger nobody has picked a start for yet, whose line says why it has a slot of its own.</summary>
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

        /// <summary>
        /// The type <paramref name="type" /> and each type the game builds it on, nearest first, as a kind on the construct has
        /// them; the type alone where no block on the construct is of it.
        /// </summary>
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
