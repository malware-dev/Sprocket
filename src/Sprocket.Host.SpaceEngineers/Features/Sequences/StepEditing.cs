using Sprocket.Host.SpaceEngineers.Features.Look;
using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Repairs;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Run = Sprocket.Host.SpaceEngineers.Sequences.Run;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The work done on a sequence's steps: adding, changing, removing and pausing them, and the pickers each change is asked through.</summary>
    public sealed class StepEditing
    {
        readonly SprocketEditor _editor;
        readonly Sequence _sequence;

        /// <summary>The work on the steps of <paramref name="sequence" />, in the editor <paramref name="editor" />.</summary>
        public StepEditing(SprocketEditor editor, Sequence sequence)
        {
            _editor = editor;
            _sequence = sequence;
        }

        /// <summary>Whether <paramref name="step" /> starts something that takes time: running another sequence, or an action that lasts.</summary>
        public bool Lasting(Step step)
        {
            var run = step as Run;
            if (run != null)
                return !run.Stops;

            var act = step as Act;
            if (act == null || act.Sets)
                return false;

            var chosen = act.Names(_editor.SlotBlocks.Actions(_sequence.System, act.Targets));
            return chosen != null && chosen.Lasts;
        }

        /// <summary>Whether a wait follows the step at <paramref name="index" />.</summary>
        bool Waited(int index)
        {
            return index + 1 < _sequence.Steps.Count && _sequence.Steps[index + 1] is Await;
        }

        /// <summary>Offers the comparisons <paramref name="relation" /> takes in a menu under the part pressed, the one <paramref name="test" /> makes marked, and changes the test to the one chosen as one edit.</summary>
        public void Relate(Step step, Comparison test, Field relation, bool plural)
        {
            _editor.AskHere("");
            for (var i = 0; i < relation.Words.Count; i++)
            {
                var related = ComparisonQuestion.Read(Literal.Name(relation.Means[i]));
                var said = plural ? SentenceText.Plural(relation.Words[i]) : relation.Words[i];
                _editor.Answer(said, delegate
                {
                    if (related == test.Relation)
                        return;

                    var now = test.Copy();
                    now.Relation = related;
                    _editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
                }, related == test.Relation);
            }
        }

        /// <summary>
        /// Offers the ways <paramref name="test" /> can read its blocks in a menu under the part pressed, in the words the picker
        /// offers them in, the one it reads them by marked, and changes it to the one chosen as one edit; a sum and an average
        /// only of what is <paramref name="numeric" />.
        /// </summary>
        public void Quantify(Step step, Comparison test, bool numeric)
        {
            _editor.AskHere("");
            for (var i = 0; i < SentenceText.Readings.Count; i++)
            {
                var reading = SentenceText.Readings[i];
                if (numeric || reading == Quantifier.Any || reading == Quantifier.All)
                    Quantifying(step, test, reading);
            }
        }

        /// <summary>
        /// Offers reading the blocks of <paramref name="test" /> by <paramref name="quantifier" /> as an answer to the standing
        /// menu, marked where the test reads them so, and changes the test to it as one edit.
        /// </summary>
        void Quantifying(Step step, Comparison test, Quantifier quantifier)
        {
            _editor.Answer(SentenceText.Quantified(quantifier), () =>
            {
                if (quantifier == test.Quantifier)
                    return;

                var now = test.Copy();
                now.Quantifier = quantifier;
                _editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
            }, quantifier == test.Quantifier);
        }

        /// <summary>Opens a step's picker on the column <paramref name="opens" /> says, the part that was pressed.</summary>
        public void EditStep(Act step, OpeningColumn opens)
        {
            Repair repair;
            var system = _sequence.System;
            var ahead = ProgramRepairs.Ahead(_editor.Texts, _editor.Runner.Bindings, system, system == null ? null : _editor.StandIn(system),
                step, out repair);
            if (ahead.Length == 0)
            {
                Edit(step, opens);
                return;
            }

            _editor.Ask(ahead);
            _editor.Answer(repair.Proceed(_editor.Texts), () => Edit(step, opens), true);
            _editor.Answer(repair.Cancel(_editor.Texts), delegate { });
        }

        /// <summary>Opens the picker on <paramref name="step" />, on the column <paramref name="opens" />, to change what it does and to what.</summary>
        void Edit(Act step, OpeningColumn opens)
        {
            var index = _sequence.Steps.IndexOf(step);

            Open(slots => new PickPage(_editor, "Do something", false,
                slots ? new List<long>() : _editor.Blocks(_sequence.System, step.Targets), step.ActionId, step.Form,
                answer => Named(answer, step.Targets, named =>
                {
                    var lasts = Vocabulary.Lasts(_editor.World, answer.Targets, answer.Chosen, answer.Form);
                    _editor.Do(_sequence,
                        new ChangeStep(index, step, named, answer.Chosen,
                            answer.Form, Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, answer.Form),
                            answer.Arguments, Awaited(index, step, lasts)));
                    _editor.Pop();
                }), arguments: step.Arguments, opens: opens, facet: _editor.Picks.Facet(_sequence.System, step.Targets)), step.Targets);
        }

        /// <summary>
        /// Opens the selector <paramref name="make" /> makes, of blocks, its first column's heading switching to the slots the
        /// logic system already has; <paramref name="make" /> is told which of the two to list. The pick goes in place of the
        /// list <paramref name="replacing" />, or null for a new one.
        /// </summary>
        void Open(Func<bool, PickPage> make, IList<string> replacing)
        {
            var source = new SourceSwitch(_editor, (choice, slots) => Sourced(make(slots), choice, slots, replacing), null);
            _editor.Push(source.Open(PickSource.Blocks));
        }

        /// <summary>
        /// <paramref name="page" />, headed by <paramref name="choice" />, naming the slots of the sequence's logic system, which it
        /// lists where <paramref name="slots" /> says so, in place of the list <paramref name="replacing" /> or null for a new one.
        /// </summary>
        PickPage Sourced(PickPage page, SourceSwitch choice, bool slots, IList<string> replacing)
        {
            page.Source = choice;
            page.Slotted = _sequence.System;
            page.ListsSlots = slots;
            page.Replacing = replacing;
            return page;
        }

        /// <summary>
        /// Hands <paramref name="named" /> the names the step answered by <paramref name="answer" /> acts on, in place of
        /// <paramref name="replacing" /> or null for a new step.
        /// </summary>
        void Named(PickAnswer answer, IList<string> replacing, Action<List<string>> named)
        {
            _editor.Picks.Name(_sequence.System, answer, false, replacing, named);
        }

        /// <summary>
        /// Whether the step at <paramref name="index" />, changed to something that <paramref name="lasts" /> or not, is followed by a
        /// wait: the one it has, kept, and one for a step that comes to take time as a new one would have.
        /// </summary>
        bool Awaited(int index, Act step, bool lasts)
        {
            return Waited(index) || lasts && !Lasting(step);
        }

        /// <summary>Asks what to affect, then what to do to it, and adds the step once both are answered, with a wait after it where it takes time.</summary>
        public void AddAction(int at)
        {
            var origin = _editor.Depth;

            Open(slots => new PickPage(_editor, "Do something", false,
                new List<long>(), "", ReachKind.Native,
                answer => Named(answer, null, named =>
                {
                    var step = new Act(answer.Chosen);
                    step.Form = answer.Form;
                    step.Provider = Vocabulary.Provider(_editor.World, answer.Targets, answer.Chosen, answer.Form);
                    foreach (var target in named)
                        step.Targets.Add(target);

                    for (var i = 0; i < answer.Arguments.Count; i++)
                        step.Arguments.Add(answer.Arguments[i]);

                    _editor.Do(_sequence, Vocabulary.Lasts(_editor.World, answer.Targets, answer.Chosen, answer.Form)
                        ? new AddStep(ChangeKind.AddStep, at, step, new Await())
                        : new AddStep(ChangeKind.AddStep, at, step));
                    _editor.UnwindTo(origin);
                })), null);
        }

        /// <summary>Asks what to check, and adds an if that checks it, with nothing in it yet, at the line <paramref name="at" />.</summary>
        public void AddIf(int at)
        {
            var origin = _editor.Depth;
            Ask(SentenceText.IfWord, null, OpeningColumn.Blocks, (test, made) =>
            {
                Do(made, new AddStep(ChangeKind.AddIf, at, new If(test), new EndIf()));
                _editor.UnwindTo(origin);
            });
        }

        /// <summary>Offers is and is not in a menu under the part pressed, the one <paramref name="test" /> makes marked, and changes the test to the one chosen as one edit.</summary>
        public void RelateState(Step step, StateTest test)
        {
            _editor.AskHere("");
            foreach (var related in new[] { Relation.Is, Relation.IsNot })
            {
                var relation = related;
                _editor.Answer(ComparisonQuestion.Said(relation), delegate
                {
                    if (relation == test.Relation)
                        return;

                    var now = test.Copy();
                    now.Relation = relation;
                    _editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
                }, relation == test.Relation);
            }
        }

        /// <summary>The picker a test of a state is made and changed with, its sentence led by <paramref name="lead" />, opened on <paramref name="test" /> where there is one.</summary>
        StatePage StateTestPage(string lead, StateTest test, Action<StateTest, NewState> chosen)
        {
            return new StatePage(_editor, _sequence.System, lead, false, test, null, chosen);
        }

        /// <summary>Asks which of the logic system's states to set and to what, and adds the step that sets it at the line <paramref name="at" />.</summary>
        public void AddSetState(int at)
        {
            var origin = _editor.Depth;
            _editor.Push(SetStatePage(null, (set, made) =>
            {
                Do(made, new AddStep(ChangeKind.AddStep, at, set));
                _editor.UnwindTo(origin);
            }));
        }

        /// <summary>Asks again which state <paramref name="step" /> sets and to what, on the picker that made it.</summary>
        public void ChangeSetState(SetState step)
        {
            var origin = _editor.Depth;
            _editor.Push(SetStatePage(step, (now, made) =>
            {
                Do(made, new ChangeSetState(_sequence.Steps.IndexOf(step), step, now));
                _editor.UnwindTo(origin);
            }));
        }

        /// <summary>The picker a step setting a state is made and changed with, opened on <paramref name="step" /> where there is one.</summary>
        StatePage SetStatePage(SetState step, Action<SetState, NewState> chosen)
        {
            var start = step == null ? null : new StateTest { State = step.State, Value = step.Value, Other = step.From };
            return new StatePage(_editor, _sequence.System, SentenceText.SetWord, true, start, null,
                (set, made) => chosen(new SetState { State = set.State, Value = set.Value, From = set.Other }, made));
        }

        /// <summary>Makes <paramref name="edit" />, with <paramref name="made" /> added to the logic system in the same step where a state was made for it, and its type where one was made too.</summary>
        void Do(NewState made, SequenceEdit edit)
        {
            if (made == null)
            {
                _editor.Do(_sequence, edit);
                return;
            }

            var system = _sequence.System;
            _editor.Do(system, WithMade.State(system, made.State, made.Type, new OnSequence(_sequence, edit)));
        }

        /// <summary>Asks what to check, and adds an otherwise if checking it to the if closed at <paramref name="end" />: last, or above its otherwise, which comes last.</summary>
        public void AddBranch(int end)
        {
            var origin = _editor.Depth;
            Ask(SentenceText.OtherwiseIfWords, null, OpeningColumn.Blocks, (test, made) =>
            {
                var last = _sequence.Otherwise(_sequence.OpenOf(end));
                Do(made, new AddStep(ChangeKind.AddBranch, last < 0 ? end : last, new Else(test)));
                _editor.UnwindTo(origin);
            });
        }

        /// <summary>Asks what to check, and makes the otherwise <paramref name="step" /> an otherwise if checking it, where it stands.</summary>
        public void GiveTest(Step step)
        {
            var origin = _editor.Depth;
            Ask(SentenceText.OtherwiseIfWords, null, OpeningColumn.Blocks, (test, made) =>
            {
                Do(made, new SetTest(_sequence.Steps.IndexOf(step), null, test));
                _editor.UnwindTo(origin);
            });
        }

        /// <summary>Adds an otherwise to the if closed at <paramref name="end" />, as its last branch.</summary>
        public void AddLast(int end)
        {
            _editor.Do(_sequence, new AddStep(ChangeKind.AddBranch, end, new Else(null)));
        }

        /// <summary>Opens the test of <paramref name="step" /> on the column <paramref name="opens" /> says, to be changed: on blocks or on a state, as it is now.</summary>
        public void EditTest(Step step, BranchTest test, OpeningColumn opens)
        {
            Ask(step is If ? SentenceText.IfWord : SentenceText.OtherwiseIfWords, test, opens, (now, made) =>
            {
                Do(made, new SetTest(_sequence.Steps.IndexOf(step), test, now));
                _editor.Pop();
            });
        }

        /// <summary>
        /// Opens the selector a test is made and changed with, its sentence led by <paramref name="lead" />: on blocks, or on the
        /// logic system's states where <paramref name="was" /> tests a state, its first column's heading switching between the
        /// two. <paramref name="chosen" /> is given the test made, and the state made for it where one was.
        /// </summary>
        void Ask(string lead, BranchTest was, OpeningColumn opens, Action<BranchTest, NewState> chosen)
        {
            var source = new SourceSwitch(_editor,
                (choice, slots) => Sourced(TestPage(was as Comparison, opens, test => chosen(test, null), slots), choice, slots, (was as Comparison)?.Targets),
                choice =>
                {
                    var page = StateTestPage(lead, was as StateTest, (test, made) => chosen(test, made));
                    page.Source = choice;
                    return page;
                });

            _editor.Push(source.Open(was is StateTest ? PickSource.States : PickSource.Blocks));
        }

        /// <summary>
        /// The picker a test is made and changed with, opened on <paramref name="test" /> where there is one, listing the slots
        /// already made where <paramref name="slots" /> says so; <paramref name="chosen" /> is given the test made.
        /// </summary>
        PickPage TestPage(Comparison test, OpeningColumn opens, Action<Comparison> chosen, bool slots)
        {
            var value = test?.Names(_editor.SlotBlocks.Actions(_sequence.System, test.Targets));
            var stated = value != null && value.IsStatus && (test.Relation == Relation.Is || test.Relation == Relation.IsNot);
            return new PickPage(_editor, "Check something", false,
                test == null || slots ? new List<long>() : _editor.Blocks(_sequence.System, test.Targets),
                test == null ? "" : stated ? ComparisonQuestion.Id(test.MemberId, test.Value.Said) : test.MemberId,
                stated ? ReachKind.Native : ReachKind.Value,
                answer => Named(answer, test?.Targets, named => chosen(Test(answer, named, test))),
                true, test?.Quantifier ?? Quantifier.Any,
                test == null ? null : ComparisonQuestion.Opening(test.Arguments, test.Relation, stated ? (Literal?)null : test.Value),
                opens, forTests: true, facet: test == null ? "" : _editor.Picks.Facet(_sequence.System, test.Targets));
        }

        /// <summary>The test a pick for one comes to, reading the slots <paramref name="named" />, changing <paramref name="was" /> where there was one.</summary>
        Comparison Test(PickAnswer answer, List<string> named, Comparison was)
        {
            var test = new Comparison();
            test.Quantifier = answer.Quantifier;
            foreach (var target in named)
                test.Targets.Add(target);

            // Read off the blocks picked, since a slot made for them is not filled until the edit is done.
            string id, state;
            var stated = ComparisonQuestion.Split(answer.Chosen, out id, out state);
            var value = Valued(Vocabulary.ActionsOf(_editor.World, answer.Targets), id);
            var given = answer.Arguments ?? new List<Literal>();
            var taken = ComparisonQuestion.Taken(value);
            test.MemberId = id;
            test.Provider = value != null && value.OwnValue != null ? BlockKinds.Own : "";
            test.Relation = given.Count > taken ? ComparisonQuestion.Read(given[taken]) : Relation.Is;
            test.Value = stated ? Stated(state) : given.Count > taken + 1 ? given[taken + 1] : Literal.Of(true);
            for (var i = 0; i < taken && i < given.Count; i++)
                test.Arguments.Add(given[i]);

            // A value the blocks picked lack keeps the arguments it had, since the selector asked for none.
            if (value != null || was == null || was.MemberId != test.MemberId || was.Provider != test.Provider)
                return test;

            for (var i = 0; i < was.Arguments.Count; i++)
                test.Arguments.Add(was.Arguments[i]);

            return test;
        }

        /// <summary>The state a choice checking a status names, as the program holds it: on or off for a switch, a name otherwise.</summary>
        static Literal Stated(string state)
        {
            if (state == Literal.Of(true).Said)
                return Literal.Of(true);

            return state == Literal.Of(false).Said ? Literal.Of(false) : Literal.Name(state);
        }

        /// <summary>The value called <paramref name="id" /> among <paramref name="actions" />, or null where there is none.</summary>
        static EntityAction Valued(IList<EntityAction> actions, string id)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == id && actions[i].Form == ReachKind.Value)
                    return actions[i];
            }

            return null;
        }

        /// <summary>Asks which other sequence of the system to run, or to stop where <paramref name="stops" /> says so, and adds the step that does.</summary>
        /// <remarks>A step that runs one comes with a wait after it, in the same edit.</remarks>
        public void AddRun(bool stops, int at)
        {
            var origin = _editor.Depth;
            _editor.Push(RunPage(stops, 0, number =>
            {
                _editor.Do(_sequence, stops
                    ? new AddStep(ChangeKind.AddStep, at, new Run(number, true))
                    : new AddStep(ChangeKind.AddStep, at, new Run(number), new Await()));
                _editor.UnwindTo(origin);
            }));
        }

        /// <summary>Asks again which sequence <paramref name="step" /> runs or stops, on the list that made it.</summary>
        public void ChangeRun(Run step)
        {
            var origin = _editor.Depth;
            _editor.Push(RunPage(step.Stops, step.Number, number =>
            {
                _editor.Do(_sequence, new SetRun(_sequence.Steps.IndexOf(step), step.Number, number));
                _editor.UnwindTo(origin);
            }));
        }

        /// <summary>The system's other sequences, to run or to stop, with <paramref name="current" /> marked where it is one; <paramref name="chosen" /> is given the one chosen.</summary>
        ChoicePage RunPage(bool stops, int current, Action<int> chosen)
        {
            var texts = _editor.Texts;
            var page = new ChoicePage(_editor, stops ? texts.SequenceStopQuestion : texts.SequenceRunQuestion)
            {
                EmptyText = stops ? texts.SequenceStopNone : texts.SequenceRunNone
            };

            var sequences = _sequence.System.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var other = sequences[i];
                if (other == _sequence)
                    continue;

                page.Add(SequenceText.Called(texts, other), other.Number == current, () => chosen(other.Number));
                page.MarkLast(Symbols.Sequence);
            }

            return page;
        }

        /// <summary>Gives <paramref name="wait" /> the <paramref name="length" /> its panel closed on, or leaves it as it was where it was <paramref name="cancelled" />.</summary>
        public void Closed(Wait wait, Duration? length, bool added, bool cancelled)
        {
            var index = _sequence.Steps.IndexOf(wait);
            if (index < 0)
                return;

            if (added && cancelled)
            {
                _editor.Withdraw(_sequence);
                return;
            }

            if (cancelled || !length.HasValue || length.Value == wait.Length)
                return;

            if (added && _editor.Withdraw(_sequence))
                _editor.Do(_sequence, new AddStep(ChangeKind.AddWait, index, new Wait(length.Value)));
            else
                _editor.Do(_sequence, new SetWait(index, wait.Length, length.Value));
        }

        /// <summary>Deletes the step at <paramref name="index" />; an if, or a branch of one, asks first whether the steps in it go too.</summary>
        public void Remove(int index)
        {
            var step = _sequence.Steps[index];
            if (!(step is Branch))
            {
                RemoveStep(index);
                return;
            }

            var whole = step is If;
            var texts = _editor.Texts;
            _editor.AskHere(whole ? texts.SequenceRemoveIf : texts.SequenceRemoveBranch);
            _editor.Answer(texts.SequenceKeepSteps, () => Unwrap(index, whole));
            _editor.Answer(texts.SequenceDeleteSteps, () => Cut(index, whole));
            _editor.Answer(texts.ButtonLeaveAlone, () => { }, true);
        }

        /// <summary>Puts a copy of the step at <paramref name="index" /> right after it: whatever a drag would carry, an if with every step in it, a branch with its own.</summary>
        public void Duplicate(int index)
        {
            var steps = _sequence.Steps;
            int end;
            if (steps[index] is If)
                end = _sequence.EndOf(index) + 1;
            else if (steps[index] is Else)
                end = _sequence.BranchAfter(index);
            else
                end = index + 1;

            var copies = new List<Step>();
            for (var i = index; i < end; i++)
                copies.Add(Automation.Copied(steps[i]));

            _editor.Do(_sequence, new AddStep(ChangeKind.CopyStep, end, copies.ToArray()));
        }

        void RemoveStep(int index)
        {
            _editor.Do(_sequence, new RemoveStep(index, _sequence.Steps[index]));
        }

        /// <summary>
        /// Takes out the if at <paramref name="index" /> where <paramref name="whole" /> says so, its own lines only, or the branch
        /// line there, keeping every step that was in it where it stands.
        /// </summary>
        /// <remarks>A branch's steps join the branch before it.</remarks>
        void Unwrap(int index, bool whole)
        {
            var lines = whole ? _sequence.Branches(index) : new List<int> { index };
            if (whole)
                lines.Add(_sequence.EndOf(index));

            var steps = new List<Step>();
            foreach (var line in lines)
                steps.Add(_sequence.Steps[line]);

            _editor.Do(_sequence, new UnwrapIf(whole ? ChangeKind.Unwrap : ChangeKind.RemoveBranch, lines, steps));
        }

        /// <summary>Deletes the if at <paramref name="index" /> where <paramref name="whole" /> says so, or the branch there, with every step in it.</summary>
        void Cut(int index, bool whole)
        {
            var end = whole ? _sequence.EndOf(index) + 1 : _sequence.BranchAfter(index);

            var going = new List<Step>();
            for (var i = index; i < end && i < _sequence.Steps.Count; i++)
                going.Add(_sequence.Steps[i]);

            _editor.Do(_sequence, new RemoveStep(whole ? ChangeKind.RemoveIf : ChangeKind.RemoveBranch, index, going.ToArray()));
        }

        /// <summary>Pauses the step at <paramref name="index" />, or lets it go again.</summary>
        public void SwitchStep(int index)
        {
            _editor.Do(_sequence, new SwitchStep(index, 1, !_sequence.Steps[index].IsOff));
        }
    }
}
