using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Features.Mods;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Sequences.Edits;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System;
using System.Collections.Generic;
using Lumel;
using Run = Sprocket.Host.SpaceEngineers.Framework.Model.Run;

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
            Repaired(step, () => Edit(step, opens));
        }

        /// <summary>
        /// Goes on to <paramref name="change" /> <paramref name="step" />, once anything its change will mend has been told and
        /// agreed to; at once where there is nothing.
        /// </summary>
        public void Repaired(Act step, Action change)
        {
            Repair repair;
            var system = _sequence.System;
            var ahead = ProgramRepairs.Ahead(_editor.Texts, _editor.Runner.Bindings, system, system == null ? null : _editor.StandIn(system),
                step, out repair);
            if (ahead.Length == 0)
            {
                change();
                return;
            }

            _editor.Ask(ahead);
            _editor.Answer(repair.Proceed(_editor.Texts), change, true);
            _editor.Answer(repair.Cancel(_editor.Texts), delegate { });
        }

        /// <summary>Changes the test of <paramref name="step" /> from <paramref name="test" /> to <paramref name="now" /> as one edit, where the step is still there with that test.</summary>
        public void Retest(Step step, BranchTest test, BranchTest now)
        {
            var index = _sequence.Steps.IndexOf(step);
            var branch = step as Branch;
            if (index < 0 || branch == null || branch.Test != test)
                return;

            _editor.Do(_sequence, new SetTest(index, test, now));
        }

        /// <summary>
        /// Gives <paramref name="step" /> the <paramref name="values" /> its panel closed on, as one edit, or leaves it as it was
        /// where it was <paramref name="cancelled" />. Toggle chosen on a switch makes it the game's toggle beside it.
        /// </summary>
        public void Closed(Act step, List<Literal> values, bool cancelled)
        {
            var index = _sequence.Steps.IndexOf(step);
            if (index < 0 || cancelled)
                return;

            var world = _editor.World;
            var blocks = _editor.Blocks(_sequence.System, step.Targets);
            var now = Vocabulary.Given(world, blocks, step, values);
            var awaited = now.Key == step.Key && now.Form == step.Form
                ? Waited(index)
                : Awaited(index, step, Vocabulary.Lasts(world, blocks, now.Key, now.Form));
            _editor.Do(_sequence, new ChangeStep(index, step, now.Targets, now.Key, now.Form, now.Arguments, awaited));
        }

        /// <summary>Opens the picker on <paramref name="step" />, on the column <paramref name="opens" />, to change what it does and to what.</summary>
        void Edit(Act step, OpeningColumn opens)
        {
            var index = _sequence.Steps.IndexOf(step);

            Open(slots => new PickPage(_editor, _editor.Texts.PickHeadingTask, false,
                slots ? new List<long>() : _editor.Blocks(_sequence.System, step.Targets), step.Key, step.Form,
                answer => Named(answer, step.Targets, named =>
                {
                    var lasts = Vocabulary.Lasts(_editor.World, answer.Targets, answer.Key, answer.Form);
                    _editor.Do(_sequence,
                        new ChangeStep(index, step, named, answer.Key, answer.Form, answer.Arguments, Awaited(index, step, lasts)));
                    _editor.Pop();
                }), arguments: step.Arguments, opens: opens, facet: _editor.Picks.Facet(_sequence.System, step.Targets)), step.Targets,
                Sourcing(opens, PickSource.Blocks), PickSource.Blocks);
        }

        /// <summary>
        /// The source a selector changing something opens on at once: <paramref name="current" />, where the part pressed,
        /// opening <paramref name="opens" />, is not the one naming what it is about; null, to ask which, where it is.
        /// </summary>
        static PickSource? Sourcing(OpeningColumn opens, PickSource? current)
        {
            return opens == OpeningColumn.Blocks ? null : current;
        }

        /// <summary>
        /// Opens the selector <paramref name="make" /> makes, of blocks, its first column's heading switching to the slots the
        /// logic system already has; <paramref name="make" /> is told which of the two to list. The pick goes in place of the
        /// list <paramref name="replacing" />, or null for a new one.
        /// </summary>
        void Open(Func<bool, PickPage> make, IList<string> replacing, PickSource? from = null, PickSource? current = null)
        {
            new SourceSwitch(_editor, slots => Sourced(make(slots), slots, replacing), null).Open(from, current);
        }

        /// <summary>
        /// <paramref name="page" />, naming the slots of the sequence's logic system, which it lists where <paramref name="slots" />
        /// says so, in place of the list <paramref name="replacing" /> or null for a new one.
        /// </summary>
        PickPage Sourced(PickPage page, bool slots, IList<string> replacing)
        {
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

        /// <summary>
        /// Asks what to affect, from the source <paramref name="from" /> where it was chosen already, then what to do to it, and
        /// adds the step once both are answered, with a wait after it where it takes time.
        /// </summary>
        public void AddAction(int at, PickSource? from = null)
        {
            var origin = _editor.Depth;

            Open(slots => new PickPage(_editor, _editor.Texts.PickHeadingTask, false,
                new List<long>(), default(MemberKey), ReachKind.Native,
                answer => Named(answer, null, named =>
                {
                    var step = new Act(answer.Key);
                    step.Form = answer.Form;
                    foreach (var target in named)
                        step.Targets.Add(target);

                    for (var i = 0; i < answer.Arguments.Count; i++)
                        step.Arguments.Add(answer.Arguments[i]);

                    _editor.Do(_sequence, Vocabulary.Lasts(_editor.World, answer.Targets, answer.Key, answer.Form)
                        ? new AddStep(ChangeKind.AddStep, at, step, new Await())
                        : new AddStep(ChangeKind.AddStep, at, step));
                    _editor.UnwindTo(origin);
                })), null, from);
        }

        /// <summary>
        /// Asks what to check, from the source <paramref name="from" /> where it was chosen already, and adds an if that checks
        /// it, with nothing in it yet, at the line <paramref name="at" />.
        /// </summary>
        public void AddIf(int at, PickSource? from = null)
        {
            var origin = _editor.Depth;
            Ask(SentenceText.IfWord, null, OpeningColumn.Blocks, (test, made) =>
            {
                Do(made, new AddStep(ChangeKind.AddIf, at, new If(test), new EndIf()));
                _editor.UnwindTo(origin);
            }, from);
        }

        /// <summary>Offers is and is not in a menu under the part pressed, and the orderings for a number or a length, the one <paramref name="test" /> makes marked, and changes the test to the one chosen as one edit.</summary>
        public void RelateState(Step step, StateTest test)
        {
            _editor.AskHere("");
            var state = _sequence.System.FindState(test.State);
            foreach (var related in ComparisonQuestion.StateRelations(state != null && state.IsOrdered))
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

        /// <summary>
        /// Offers enable and disable, and toggle where the switch flips, in a menu under the part pressed, the one <paramref name="step" /> does marked, and changes
        /// what it does to the one chosen as one edit.
        /// </summary>
        public void Switch(Act step)
        {
            var flips = Vocabulary.Toggle(_editor.World, _editor.Blocks(_sequence.System, step.Targets), step.Key) != null;
            SwitchQuestion.Offer(_editor, step, flips, values => Closed(step, new List<Literal>(values), false));
        }

        /// <summary>
        /// Offers the values the state <paramref name="test" /> checks can hold in a menu under the part pressed, and changes the
        /// test to the one chosen as one edit; the value of another state opens the selector. A new setting is asked in a panel hung
        /// from what <paramref name="part" /> finds; a number, text or length is given in a panel handed to <paramref name="asking" />.
        /// </summary>
        public void CompareState(Step step, StateTest test, Func<Element> part, Action<ValuesQuestion> asking)
        {
            StateValueQuestion.Offer(_editor, _sequence.System, test.State, test.Value, test.Other, part, value =>
            {
                var now = test.Copy();
                now.Value = value;
                now.Other = "";
                _editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
            }, () => EditTest(step, test, OpeningColumn.Arguments), asking);
        }

        /// <summary>
        /// Offers the values the state <paramref name="step" /> sets can hold in a menu under the part pressed, and sets it to the
        /// one chosen as one edit; the value of another state opens the selector, and an expression the page it is built on. A new
        /// setting is asked in a panel hung from what <paramref name="part" /> finds; a number, text or length is given in a panel
        /// handed to <paramref name="asking" />.
        /// </summary>
        public void SetStateTo(SetState step, Func<Element> part, Action<ValuesQuestion> asking)
        {
            var value = step.Expression != null ? Literal.Worked(step.Expression) : step.Value;
            StateValueQuestion.Offer(_editor, _sequence.System, step.State, value, step.From, part, chosen => SetStateTo(step, chosen),
                () => ChangeSetState(step), asking, () => BuildStateValue(step));
        }

        /// <summary>Has <paramref name="step" /> set its state to <paramref name="value" />, an expression, a setting or a value of its own, as one edit.</summary>
        void SetStateTo(SetState step, Literal value)
        {
            var index = _sequence.Steps.IndexOf(step);
            if (index < 0)
                return;

            var now = value.IsExpression ? new SetState { State = step.State, Expression = value.Tree } : new SetState { State = step.State, Value = value };
            _editor.Do(_sequence, new ChangeSetState(index, step, now));
        }

        /// <summary>Opens the page the value <paramref name="step" /> sets its state to is built on as an expression, which changes it as one edit when done.</summary>
        void BuildStateValue(SetState step)
        {
            var state = _sequence.System.FindState(step.State);
            if (state == null)
                return;

            var start = step.Expression != null ? Literal.Worked(step.Expression)
                : step.From.Length > 0 ? (Literal?)null
                : step.Value;
            _editor.Push(new ExpressionPage(_editor, _sequence.System, state.IsFlag ? "flag" : state.Type, step.State, start, built => SetStateTo(step, built)));
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

        /// <summary>
        /// Asks what to check, from the source <paramref name="from" /> where it was chosen already, and adds an otherwise if
        /// checking it to the if closed at <paramref name="end" />: last, or above its otherwise, which comes last.
        /// </summary>
        public void AddBranch(int end, PickSource? from = null)
        {
            var origin = _editor.Depth;
            Ask(SentenceText.OtherwiseIfWords, null, OpeningColumn.Blocks, (test, made) =>
            {
                var last = _sequence.Otherwise(_sequence.OpenOf(end));
                Do(made, new AddStep(ChangeKind.AddBranch, last < 0 ? end : last, new Else(test)));
                _editor.UnwindTo(origin);
            }, from);
        }

        /// <summary>
        /// Asks what to check, from the source <paramref name="from" /> where it was chosen already, and makes the otherwise
        /// <paramref name="step" /> an otherwise if checking it, where it stands.
        /// </summary>
        public void GiveTest(Step step, PickSource? from = null)
        {
            var origin = _editor.Depth;
            Ask(SentenceText.OtherwiseIfWords, null, OpeningColumn.Blocks, (test, made) =>
            {
                Do(made, new SetTest(_sequence.Steps.IndexOf(step), null, test));
                _editor.UnwindTo(origin);
            }, from);
        }

        /// <summary>Whether a test may check a mod: checking for one is unfinished, so it is offered in developer mode, and only where a mod has a namespace to check for.</summary>
        public bool ChecksMods => _editor.DeveloperMode && HostNamespaces.HasMods;

        /// <summary>Adds an otherwise to the if closed at <paramref name="end" />, as its last branch.</summary>
        public void AddLast(int end)
        {
            _editor.Do(_sequence, new AddStep(ChangeKind.AddBranch, end, new Else(null)));
        }

        /// <summary>Opens the test of <paramref name="step" /> on the column <paramref name="opens" /> says, to be changed: on blocks or on a state, as it is now.</summary>
        public void EditTest(Step step, BranchTest test, OpeningColumn opens)
        {
            var origin = _editor.Depth;
            Ask(step is If ? SentenceText.IfWord : SentenceText.OtherwiseIfWords, test, opens, (now, made) =>
            {
                Do(made, new SetTest(_sequence.Steps.IndexOf(step), test, now));
                _editor.UnwindTo(origin);
            });
        }

        /// <summary>
        /// Opens the selector a test is made and changed with, its sentence led by <paramref name="lead" />: on blocks, or on the
        /// logic system's states where <paramref name="was" /> tests a state, or on the host's mods where it tests one, its first
        /// column's heading switching between them. <paramref name="chosen" /> is given the test made, and the state made for it where one was.
        /// </summary>
        void Ask(string lead, BranchTest was, OpeningColumn opens, Action<BranchTest, NewState> chosen, PickSource? from = null)
        {
            var source = new SourceSwitch(_editor,
                slots => Sourced(TestPage(was as Comparison, opens, test => chosen(test, null), slots), slots, (was as Comparison)?.Targets),
                () => StateTestPage(lead, was as StateTest, (test, made) => chosen(test, made)),
                // Checking for a mod is unfinished, so it is offered in developer mode where a mod has a namespace to check for, and to a test already checking one.
                ChecksMods || was is HostTest
                    ? () => new ModsPage(_editor, lead, was as HostTest, test => chosen(test, null))
                    : (Func<Framework.Ui.Shell.EditorPage>)null,
                () => new ExpressionPage(_editor, _sequence.System, "flag", null, was is ExpressionTest ? Literal.Worked(((ExpressionTest)was).Condition) : (Literal?)null,
                    built => chosen(ConditionShapes.TestOf(_sequence.System, built.IsExpression ? built.Tree : new Operand(built)), null), true));

            var current = was is StateTest ? PickSource.States : was is HostTest ? PickSource.Mods : was is ExpressionTest ? PickSource.Expression
                : was != null ? PickSource.Blocks : (PickSource?)null;
            source.Open(from ?? Sourcing(opens, current), current);
        }

        /// <summary>
        /// The picker a test is made and changed with, opened on <paramref name="test" /> where there is one, listing the slots
        /// already made where <paramref name="slots" /> says so; <paramref name="chosen" /> is given the test made.
        /// </summary>
        PickPage TestPage(Comparison test, OpeningColumn opens, Action<Comparison> chosen, bool slots)
        {
            var value = test?.Names(_editor.SlotBlocks.Actions(_sequence.System, test.Targets));
            var stated = value != null && value.IsStatus && (test.Relation == Relation.Is || test.Relation == Relation.IsNot);
            return new PickPage(_editor, _editor.Texts.PickHeadingCondition, false,
                test == null || slots ? new List<long>() : _editor.Blocks(_sequence.System, test.Targets),
                test == null ? default(MemberKey) : stated ? test.Key.WithId(ComparisonQuestion.Id(test.MemberId, test.Value.Said)) : test.Key,
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
            test.Key = stated ? answer.Key.WithId(id) : answer.Key;
            var value = EntityAction.Value(Vocabulary.ActionsOf(_editor.World, answer.Targets), test.Key);
            var given = answer.Arguments ?? new List<Literal>();
            var taken = ComparisonQuestion.Taken(value);
            test.Relation = given.Count > taken ? ComparisonQuestion.Read(given[taken]) : Relation.Is;
            test.Value = stated ? Stated(state) : given.Count > taken + 1 ? given[taken + 1] : Literal.Of(true);
            for (var i = 0; i < taken && i < given.Count; i++)
                test.Arguments.Add(given[i]);

            // A value the blocks picked lack keeps the arguments it had, since the selector asked for none.
            if (value != null || was == null || was.Key != test.Key)
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

        /// <summary>
        /// Offers, in a menu under what was pressed, a new sequence and the system's other sequences to run, or those to stop
        /// where <paramref name="stops" /> says so, and adds the step that does at the line <paramref name="at" />.
        /// </summary>
        /// <remarks>A step that runs one comes with a wait after it, in the same edit; a new sequence is made in it too, and opened.</remarks>
        public void AddRun(bool stops, int at)
        {
            Offer(stops, 0,
                number => _editor.Do(_sequence, stops
                    ? new AddStep(ChangeKind.AddStep, at, new Run(number, true))
                    : new AddStep(ChangeKind.AddStep, at, new Run(number), new Await())),
                made => new AddStep(ChangeKind.AddStep, at, new Run(made.Number), new Await()));
        }

        /// <summary>
        /// Offers, in a menu under the part pressed, the system's other sequences, and for a step that runs one a new sequence
        /// first, the one <paramref name="step" /> names marked, and changes it to the one chosen as one edit.
        /// </summary>
        public void Rerun(Run step)
        {
            Offer(step.Stops, step.Number,
                number =>
                {
                    var index = _sequence.Steps.IndexOf(step);
                    if (index >= 0)
                        _editor.Do(_sequence, new SetRun(index, step.Number, number));
                },
                made => new SetRun(_sequence.Steps.IndexOf(step), step.Number, made.Number));
        }

        /// <summary>
        /// Offers the system's other sequences in a menu under what was pressed, <paramref name="current" /> marked: to stop
        /// where <paramref name="stops" /> says so, saying so where there is none; to run otherwise, after a new sequence and a
        /// line. <paramref name="chosen" /> is given the number of another one chosen; <paramref name="fresh" /> makes the edit
        /// that uses a new sequence, which is made with it as one edit and opened.
        /// </summary>
        void Offer(bool stops, int current, Action<int> chosen, Func<Sequence, SequenceEdit> fresh)
        {
            var texts = _editor.Texts;
            var sequences = _sequence.System.Sequences;
            _editor.AskHere(stops && sequences.Count < 2 ? texts.SequenceStopNone : "");
            if (!stops)
            {
                _editor.Answer(texts.PickNewSequence, () => RunNew(fresh));
                if (sequences.Count > 1)
                    _editor.AnswerRule();
            }

            for (var i = 0; i < sequences.Count; i++)
            {
                var other = sequences[i];
                if (other == _sequence)
                    continue;

                var number = other.Number;
                _editor.Answer(SequenceText.Called(texts, other), delegate
                {
                    if (number != current)
                        chosen(number);
                }, number == current);
            }
        }

        /// <summary>Makes a new sequence in the system, and with it the edit <paramref name="uses" /> makes of it, as one edit; then opens it.</summary>
        void RunNew(Func<Sequence, SequenceEdit> uses)
        {
            var system = _sequence.System;
            var made = _editor.Program.AddSequence(system);
            made.Name = _editor.Name(made);
            _editor.Do(system, new WithMade(new MakeSequence(_editor.Program, made, ChangeKind.NewSequence), new OnSequence(_sequence, uses(made))));
            _editor.PushNamed(new SequencePage(_editor, made));
        }

        /// <summary>
        /// Gives <paramref name="wait" /> the <paramref name="length" /> its panel closed on, or the setting <paramref name="setting" /> where it
        /// is not empty, or leaves it as it was where it was <paramref name="cancelled" />.
        /// </summary>
        public void Closed(Wait wait, Duration? length, string setting, bool added, bool cancelled)
        {
            var index = _sequence.Steps.IndexOf(wait);
            if (index < 0)
                return;

            if (added && cancelled)
            {
                _editor.Withdraw(_sequence);
                return;
            }

            if (cancelled || setting.Length == 0 && !length.HasValue)
                return;

            var now = setting.Length > 0 || !length.HasValue ? wait.Length : length.Value;
            if (now == wait.Length && setting == wait.Setting)
                return;

            if (added && _editor.Withdraw(_sequence))
                _editor.Do(_sequence, new AddStep(ChangeKind.AddWait, index, new Wait(now) { Setting = setting }));
            else
                _editor.Do(_sequence, new SetWait(index, wait.Length, wait.Setting, now, setting, wait.Expression));
        }

        /// <summary>Opens the page the length of the wait at <paramref name="index" /> is built on as an expression, which changes it as one edit when done.</summary>
        public void BuildLength(int index)
        {
            var wait = _sequence.Steps[index] as Wait;
            if (wait == null)
                return;

            var start = wait.Expression != null ? Literal.Worked(wait.Expression)
                : wait.Setting.Length > 0 ? Literal.Setting(wait.Setting)
                : Literal.Of(wait.Length);
            _editor.Push(new ExpressionPage(_editor, _sequence.System, "duration", SentenceText.WaitWord, start, built => Lasting(index, built)));
        }

        /// <summary>Has the wait at <paramref name="index" /> last as <paramref name="built" /> says: an expression, a setting or a length of its own.</summary>
        void Lasting(int index, Literal built)
        {
            var wait = index < _sequence.Steps.Count ? _sequence.Steps[index] as Wait : null;
            if (wait == null)
                return;

            var edit = built.IsExpression ? new SetWait(index, wait.Length, wait.Setting, wait.Length, "", wait.Expression, built.Tree)
                : built.IsSetting ? new SetWait(index, wait.Length, wait.Setting, wait.Length, built.Text, wait.Expression)
                : new SetWait(index, wait.Length, wait.Setting, built.Length, "", wait.Expression);
            _editor.Do(_sequence, edit);
        }

        /// <summary>Gives <paramref name="abort" /> the <paramref name="message" />, or the setting <paramref name="setting" /> where it is not empty, or <paramref name="expression" /> where it is given, and <paramref name="level" /> its panel closed on, or leaves it as it was where it was <paramref name="cancelled" />.</summary>
        public void Closed(Abort abort, string message, string setting, Expression expression, AbortLevel level, bool cancelled)
        {
            var index = _sequence.Steps.IndexOf(abort);
            if (index < 0 || cancelled)
                return;

            _editor.Do(_sequence, new SetAbort(index, abort, message, setting, expression, level));
        }

        /// <summary>Deletes the step at <paramref name="index" />; an if, or a branch of one, asks first whether the steps in it go too.</summary>
        public void Remove(int index)
        {
            var step = _sequence.Steps[index];
            if (step is Handler)
            {
                RemoveHandler(index);
                return;
            }

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

        /// <summary>
        /// Removes the catch or finally at <paramref name="index" />: at once where it holds nothing, and otherwise asking whether its
        /// steps go with it or join the steps above.
        /// </summary>
        void RemoveHandler(int index)
        {
            var end = index + 1;
            while (end < _sequence.Steps.Count && !(_sequence.Steps[end] is Handler))
                end++;

            if (end == index + 1)
            {
                RemoveStep(index);
                return;
            }

            var texts = _editor.Texts;
            _editor.AskHere(texts.SequenceRemoveHandler);
            _editor.Answer(texts.SequenceKeepSteps, () => Unwrap(index, false));
            _editor.Answer(texts.SequenceDeleteSteps, () =>
            {
                var going = new List<Step>();
                for (var i = index; i < end; i++)
                    going.Add(_sequence.Steps[i]);

                _editor.Do(_sequence, new RemoveStep(ChangeKind.RemoveBranch, index, going.ToArray()));
            });
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
