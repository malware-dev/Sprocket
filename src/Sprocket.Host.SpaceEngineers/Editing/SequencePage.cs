using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// One sequence, with room for the whole sentence.
    /// </summary>
    sealed class SequencePage : EditorPage
    {
        static readonly int[] WaitPresets = { 60, 120, 300, 600, 1800, 3600 };

        readonly Sequence _sequence;

        SequenceComponent _sentence;

        int _undoCommand;
        int _redoCommand;

        public SequencePage(SprocketEditor editor, Sequence sequence) : base(editor)
        {
            _sequence = sequence;

            // Here rather than in Build, which runs again every time the sentence changes shape -
            // which is exactly when something has just been edited.
            _quiet = editor.Program.Wiring;
        }

        public int SequenceId => _sequence.Id;

        /// <summary>
        /// A named sequence is called by its name; an unnamed one reads as its own
        /// sentence. Either way "Sequence" as a heading would say nothing the user
        /// cannot already see.
        /// </summary>
        /// <remarks>
        /// No subtitle. What the sequence is doing right now belongs on the sentence itself, beside
        /// the switch that changes it - repeating it up here reads as two different things saying the
        /// same words. What starts it is not here either: that is a row on its system's screen, and
        /// a sequence three triggers start has no one line to say so.
        /// </remarks>
        public override string Title => SequenceText.Doing(Editor.Runner.Bindings, _sequence);

        /// <summary>
        /// Naming happens on the heading, because the heading is what the name is for.
        /// </summary>
        /// <remarks>
        /// A sequence arrives already named - "Sequence 3" - the way a block does, so this is always
        /// overtyping rather than filling in a blank.
        /// </remarks>
        public override string CustomName => _sequence.Name ?? "";

        public override bool CanRename => true;

        public override string RenamePrompt => "What do you call this?";

        /// <summary>
        /// A sequence has to be called something, now that the name is what identifies it.
        /// </summary>
        /// <remarks>
        /// The same reasoning a system already carried: with nothing underneath to fall back on, an
        /// empty name leaves a row in a list that cannot be told from the one below it.
        /// </remarks>
        public override bool NameRequired => true;

        public override void Rename(string name)
        {
            SetName(name);
        }

        /// <summary>
        /// One part: what this sequence is called.
        /// </summary>
        /// <remarks>
        /// What starts it is not here. A sequence may be started from several places, and its own
        /// screen cannot say so without becoming a second list of the triggers its system already
        /// lists - so this screen is what the sequence is and what it does, and nothing about why.
        /// </remarks>
        public override void Heading(List<HeadingPart> parts)
        {
            var does = new HeadingPart("");
            does.Prompt = RenamePrompt;
            does.NameRequired = NameRequired;
            does.RenameCommand = Editor.RegisterText(Cmd.Rename, Rename);

            parts.Add(does);
            RefreshHeading(parts);
        }

        public override void RefreshHeading(List<HeadingPart> parts)
        {
            if (parts.Count < 1)
                return;

            // The name where there is one, and what it actually does where there is not - so an unnamed
            // sequence still reads as a whole sentence and naming one is a choice rather than a chore.
            // The same call a list row makes, so the two cannot come to disagree.
            parts[0].Name = CustomName;
            parts[0].Text = SequenceText.Doing(Editor.Runner.Bindings, _sequence);
        }

        public override void Build(List<EditorComponent> components)
        {
            _sentence = new SequenceComponent(_sequence.Id);
            _sentence.Enabled = _sequence.Enabled;
            _sentence.ToggleCommand = Editor.RegisterToggle(Cmd.Enable, SetEnabled);

            // Everything that changes what the system does waits until it is switched off.
            // The commands are simply not issued, so there is nothing for a target to invoke
            // by accident - and the screen keeps the same shape either way, so nothing appears
            // or disappears as somebody flips the switch.
            _sentence.Editable = !_sequence.CanRun;
            _sentence.LockedText = "Running. Stop it to make changes - whatever it has already "
                + "done stays done.";
            // Not held back by the switch, unlike everything that changes what this does. Throwing
            // it away is not editing it, and a sequence somebody wants rid of while it is running is
            // exactly when they want rid of it.
            _sentence.DeleteCommand = Editor.Register(Cmd.DeleteSequence,
                delegate { Editor.DeleteSequence(_sequence); });

            // Whatever the switch says, and that is the point of it. What starts a sequence is
            // usually a door or a button somewhere else on the ship, and somebody working on one
            // parks it first so its triggers leave it alone - which is exactly when they want to
            // see it work.
            _sentence.RunCommand = Editor.Register(Cmd.RunNow,
                delegate { Editor.RunNow(_sequence); });

            // Its other half, registered beside it. Which of the two the button carries is decided
            // every tick by whether the sequence is going; both ids have to exist for either to.
            _sentence.StopCommand = Editor.Register(Cmd.StopNow,
                delegate { Editor.StopNow(_sequence); });

            // Both are edits, so both wait for the switch. Registered here whether or not
            // there is anything to walk back, because issuing a command needs the numbering
            // only a rebuild does - and whether there is anything to walk back changes without
            // one, since renaming deliberately does not rebuild. So Build decides whether the
            // command may exist at all, and Refresh decides whether it is offered.
            _undoCommand = Change(Cmd.Undo, delegate { Editor.Undo(_sequence); });
            _redoCommand = Change(Cmd.Redo, delegate { Editor.Redo(_sequence); });

            // An await is not a line on screen. Concurrency is not a feature yet, so a row saying
            // "wait for it to finish" after every step would be noise about something nobody can
            // change - and the editor writes one after every step anyway. Each row therefore stands
            // for a step and the await that follows it, and the two are moved and deleted as one.
            _shown.Clear();
            for (var i = 0; i < _sequence.Steps.Count; i++)
            {
                if (_sequence.Steps[i] is Await)
                    continue;

                _shown.Add(i);
                _sentence.Steps.Add(BuildStep(_shown.Count - 1, i, _sequence.Steps[i]));
            }

            // Two kinds of step, so two ways to add one. Waiting a set time is not an
            // action on a block and pretending otherwise is what makes the flow
            // awkward - it gets its own door instead.
            _sentence.AddOptions.Add(new ChoiceEntry("do something", false,
                Change(Cmd.AddAction, AddAction)));
            _sentence.AddOptions.Add(new ChoiceEntry("wait", false,
                Change(Cmd.AddWait, AddWait)));

            components.Add(_sentence);
        }

        /// <summary>
        /// Offers to switch a finished sequence on, on the way out.
        /// </summary>
        /// <remarks>
        /// A sequence is parked while it is built, deliberately - a half-written rule must not fire,
        /// and the switch is what makes editing safe. The cost is that somebody finishes, walks away,
        /// and concludes the whole thing does not work. Leaving is the moment that costs one press to
        /// put right, and the only moment anybody is still thinking about this sequence.
        /// <para>
        /// Only when something was actually changed, and only once per change. Opening a sequence to
        /// read it and leaving asks nothing; nor does leaving again after somebody has already
        /// answered. Being asked a question you have answered is how a helpful prompt turns into one
        /// people learn to dismiss without reading.
        /// </para>
        /// <para>
        /// Dismissing it is not answering it. Somebody who backs out has said they did not mean to
        /// leave, so pressing back again asks again - the question is only spent once one of its
        /// answers is taken.
        /// </para>
        /// <para>
        /// Nothing with no steps is offered: there is nothing to switch on yet, and asking would be
        /// treating an abandoned start as though it were finished work. Nor is anything broken -
        /// Sprocket switched that off itself because it cannot run, and offering to start it would be
        /// the screen arguing with its own judgement one press after making it.
        /// </para>
        /// </remarks>
        public override bool Leaving()
        {
            var changed = Editor.Program.Wiring;
            if (changed == _quiet || _sequence.Enabled || _sequence.Steps.Count == 0)
                return true;

            // Never offer to start something that cannot run. Sprocket parked it in the first place
            // because it is broken, and asking would be the screen offering to undo its own
            // judgement one press after making it.
            //
            // A warning is not that. Nothing starts this one yet, which is exactly the state somebody
            // walks away from thinking they have finished, so it earns the offer like any other.
            var fault = SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, _sequence,
                Editor.Runner.Get(_sequence));

            if (fault.Exists && !fault.IsWarning)
                return true;

            Editor.Ask("Switch this sequence on?");

            // Leaving it parked first, because that is what pressing back already meant - the offer
            // must not turn the safe answer into the one that takes a second press. And the one
            // Enter takes, for the same reason: the key that gets pressed without reading must not
            // switch something on.
            Editor.Answer("Leave it off", delegate
            {
                _quiet = Editor.Program.Wiring;
                Editor.Pop();
            }, true);

            Editor.Answer("Switch it on", delegate
            {
                _quiet = Editor.Program.Wiring;
                SetEnabled(true);
                Editor.Pop();
            });

            return false;
        }

        /// <summary>
        /// What the program's edit count was when this last had nothing to ask about.
        /// </summary>
        /// <remarks>
        /// Set on the way in and again whenever the question is put, so leaving a sequence somebody
        /// only came to look at asks nothing at all - and so does leaving again after they have
        /// already answered. Only a change since then earns another question.
        /// </remarks>
        int _quiet;

        public override void Refresh()
        {
            if (_sentence == null)
                return;

            var state = Editor.Runner.Get(_sequence);

            // Asked once and held, because asking moves its own clock: two callers in one pass would
            // leave the second one told that no time had gone by, ever.
            var second = SecondPassed();

            _sentence.Enabled = _sequence.Enabled;
            // Being switched off is said here rather than in the banner below, because the switch
            // that says it is in this same bar: a banner would be a second copy of it appearing and
            // vanishing as that switch is pressed, which moves the whole sentence under it.
            // What it is doing, plainly. What is wrong with it is said by Mark, in the same place
            // and in its own colour, and the two never show at once.
            _sentence.Status = SequenceText.Status(SequenceSummary.Status(_sequence, state));

            // Which way the button faces. A sequence that is going is one to stop, and one that is
            // not is one to start - never both, because the other would do nothing.
            _sentence.Running = state != null && state.IsRunning;
            _sentence.JustRan = state != null && state.LastFiredTick >= 0
                && Editor.CurrentTick - state.LastFiredTick < 10;
            _sentence.SkippedCount = state == null ? 0 : state.SkippedCount;
            _sentence.RefusedCount = state == null ? 0 : state.RefusedCount;

            // Every tick, and it costs nothing: every answer comes out of a table built once.
            _sentence.LastRun = TimeText.Ago(SequenceSummary.TicksSinceRun(state, Editor.CurrentTick));

            // A row is lit while the sequence is anywhere inside it, so a step stays lit while the
            // await after it is what is actually being waited on.
            for (var i = 0; i < _sentence.Steps.Count && i < _shown.Count; i++)
            {
                var from = _shown[i];
                var to = from + Spanned(from);
                _sentence.Steps[i].IsCurrent = state != null && state.IsRunning
                    && state.CurrentStep >= from && state.CurrentStep < to;
            }

            // Read every tick rather than only when the screen is rebuilt, because renaming
            // changes what there is to walk back without changing the screen's shape.
            var canUndo = Editor.CanUndo(_sequence);
            _sentence.UndoCommand = canUndo ? _undoCommand : 0;
            _sentence.UndoText = canUndo ? Editor.UndoLabel(_sequence) : "";

            var canRedo = Editor.CanRedo(_sequence);
            _sentence.RedoCommand = canRedo ? _redoCommand : 0;
            _sentence.RedoText = canRedo ? Editor.RedoLabel(_sequence) : "";

            if (second)
                Mark();
        }

        /// <summary>
        /// Says what is wrong once, and marks the line it is wrong on.
        /// </summary>
        /// <remarks>
        /// Two statements that do not repeat each other: the words say what, the mark says where.
        /// Words alone in front of a nine-step sequence leave somebody reading all of it to work out
        /// which part is being complained about, and they were led here from two screens away
        /// precisely so they would not have to look.
        /// <para>
        /// A fault in what starts it has no line here to mark, because what starts it is not on this
        /// screen. The words still say it, and the row that can be marked is the trigger's own, on
        /// the system's screen.
        /// </para>
        /// <para>
        /// Every line is cleared and one is set, rather than the marks being left where they were.
        /// A fault that has been mended has to stop showing, and the line that was wrong is
        /// frequently the line that was just changed.
        /// </para>
        /// </remarks>
        void Mark()
        {
            var fault = SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, _sequence,
                Editor.Runner.Get(_sequence));

            _sentence.Problem = fault.Exists ? SequenceText.Problem(fault) : "";
            _sentence.ProblemIsWarning = fault.IsWarning;

            for (var i = 0; i < _sentence.Steps.Count && i < _shown.Count; i++)
                _sentence.Steps[i].HasProblem = fault.Exists && fault.Step == _shown[i];
        }

        /// <summary>
        /// Issues a command only while the sequence is switched off.
        /// </summary>
        /// <remarks>
        /// Zero is the way a page says "not now". A target cannot invoke it, so there is
        /// nothing to guard against on the far side and no way for a stray press to reach a
        /// running system. The screen keeps the same shape either way - the rows are all
        /// still there, they simply do nothing until the switch is off, which is why
        /// <see cref="SequenceComponent.LockedText"/> exists to say so.
        /// </remarks>
        int Change(Cmd kind, Action action)
        {
            return Change(kind, 0, 0, action);
        }

        int Change(Cmd kind, int which, Action action)
        {
            return Change(kind, which, 0, action);
        }

        int Change(Cmd kind, int which, int part, Action action)
        {
            return _sequence.CanRun ? 0 : Editor.Register(kind, which, part, action);
        }

        /// <summary>The same, for a slot whose contents are typed rather than chosen.</summary>
        int Typing(Cmd kind, int which, int part, Action<string> action)
        {
            return _sequence.CanRun ? 0 : Editor.RegisterText(kind, which, part, action);
        }

        /// <summary>Where each row's step sits among the sequence's lines.</summary>
        readonly List<int> _shown = new List<int>();

        SentenceStep BuildStep(int row, int index, Step step)
        {
            var position = row;
            var wait = step as Wait;
            var act = step as Act;

            var built = new SentenceStep(row + 1, Kind(step));
            built.Position = position;
            built.MoveToCommand = _sequence.CanRun
                ? 0
                : Editor.RegisterNumber(Cmd.MoveStep, position, delegate(int to) { Move(position, to); });
            built.DeleteCommand = Change(Cmd.DeleteStep, position, delegate { RemoveStep(index); });

            if (wait != null)
            {
                built.Slots.Add(new DurationSlot(wait.Ticks,
                    Change(Cmd.StepSlot, position, 0, delegate { EditWait(wait); })));
                return built;
            }

            var run = step as Run;
            if (run != null)
            {
                // Read only, with no command on the slot: nothing in the editor makes one of these
                // yet, and a slot that opens nothing must still show what the step does.
                var called = _sequence.System == null ? null : _sequence.System.FindSequence(run.Number);
                built.Slots.Add(new SequenceSlot(run.Number, called == null ? "" : called.Name, 0));
                return built;
            }

            // An await has nothing to choose: it waits for whatever is above it, which is the sentence
            // rather than anything on this line.
            if (act == null)
                return built;

            // Shown as written. A step whose blocks do not offer what it does is a fault, said on the
            // sequence, and the gap in its sentence is where it is re-picked.
            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, act.Targets));

            built.Slots.Add(new ActionSlot(chosen,
                Change(Cmd.StepSlot, position, 0, delegate { EditStep(act); })));

            built.Slots.Add(new EntitiesSlot(Resolved(act.Targets), true,
                Change(Cmd.StepSlot, position, 1, delegate { EditStep(act); })));

            // Last, because the blocks are what somebody scanning a list of steps is looking for
            // and the values are the detail. Its own slot, so pressing it changes the values and
            // nothing else - the same rule every other part of a sentence follows.
            if (chosen != null && chosen.Wants > 0)
            {
                var fields = new List<Field>();
                chosen.Fields(fields);

                var given = new ValuesSlot(act.Values, fields,
                    Typing(Cmd.StepSlot, position, 2, delegate(string said) { Gave(act, said); }));

                // Asked by what the form will show rather than by how many values the action takes.
                // The two part company the moment a description says several of them are one thing.
                given.Question = SequenceText.Wants(fields);
                built.Slots.Add(given);
            }

            return built;
        }

        static StepKind Kind(Step step)
        {
            if (step is Wait)
                return StepKind.Wait;

            return step is Run ? StepKind.Run : StepKind.Do;
        }

        /// <summary>How many of the sequence's lines a row stands for: the step, and any await after it.</summary>
        int Spanned(int index)
        {
            return index + 1 < _sequence.Steps.Count && _sequence.Steps[index + 1] is Await ? 2 : 1;
        }

        /// <summary>
        /// Replaces the values a step was given with what was typed into its slot.
        /// </summary>
        /// <remarks>
        /// All of them at once, because that is how they were asked for: an action declares how many
        /// values it wants and nothing about what each one is, so there is no such thing as changing
        /// the second one.
        /// </remarks>
        void Gave(Act step, string said)
        {
            var given = new List<Literal>();
            ValueLine.Read(said, given);

            Editor.Do(_sequence, new SetValues(_sequence.Steps.IndexOf(step), step.Values, given));
        }

        void EditStep(Act step)
        {
            var index = _sequence.Steps.IndexOf(step);

            // Handed what the step already holds, so the question that finishes this opens on those
            // values rather than empty. Changing which blocks a step acts on is not a reason to type
            // its values again.
            Editor.Push(new PickPage(Editor, "Do something", false,
                Resolved(step.Targets), step.ActionId,
                delegate(PickAnswer answer)
                {
                    Editor.Do(_sequence,
                        new ChangeStep(index, step, Declared(answer.Targets), answer.Chosen,
                            answer.Sets));
                    Editor.Pop();
                }, values: step.Values));
        }

        /// <summary>
        /// Asks what to affect, then what to do to it.
        /// </summary>
        /// <remarks>
        /// Blocks first, because the user knows which block they mean but may not know
        /// what verbs exist - pointing at a door and being told what a door can do is
        /// discovery, whereas every verb on the ship in one list is a wall.
        ///
        /// The step is not added until both are answered, so backing out leaves no
        /// half-written line behind.
        /// </remarks>
        void AddAction()
        {
            var origin = Editor.Depth;

            Editor.Push(new PickPage(Editor, "Do something", false,
                new List<long>(), "",
                delegate(PickAnswer answer)
                {
                    // Only once both answers are in. Backing out of the questions adds no
                    // step, so there is nothing to take back.
                    var step = new Act(answer.Chosen);
                    step.Sets = answer.Sets;
                    var named = Declared(answer.Targets);
                    for (var i = 0; i < named.Count; i++)
                        step.Targets.Add(named[i]);

                    // Whatever the action was given, which the picker asked for on the button that
                    // finished it. Empty for everything that takes none, which is nearly everything.
                    for (var i = 0; i < answer.Values.Count; i++)
                        step.Values.Add(answer.Values[i]);

                    Added(step, origin);
                }));
        }

        /// <summary>
        /// Puts the step in, with an await after it.
        /// </summary>
        /// <remarks>
        /// Because a numbered list is read as one thing after another. Overlapping steps means
        /// deleting the await, which is something somebody does on purpose rather than a default
        /// they never asked for. One edit, so one press takes the pair back.
        /// </remarks>
        void Added(Act step, int origin)
        {
            Editor.Do(_sequence,
                new AddStep("adding a step", _sequence.Steps.Count, step, new Await()));
            Editor.UnwindTo(origin);
        }

        /// <summary>
        /// A timed wait, for the things that have no completion signal to wait on -
        /// flashing a beacon, mostly. Rare, and deliberately the second option.
        /// </summary>
        void AddWait()
        {
            var origin = Editor.Depth;
            Editor.Push(WaitPage(delegate(int ticks)
            {
                Editor.Do(_sequence,
                    new AddStep("adding a wait", _sequence.Steps.Count, new Wait(ticks)));
                Editor.UnwindTo(origin);
            }));
        }

        ChoicePage WaitPage(Action<int> chosen)
        {
            var page = new ChoicePage(Editor, "How long should it wait?");
            for (var i = 0; i < WaitPresets.Length; i++)
            {
                var ticks = WaitPresets[i];
                page.Add(TimeText.WaitChoice(ticks), false, delegate { chosen(ticks); });
            }

            page.AddTyped(TimeText.OtherWait, TimeText.UseLength, WholeTicks, delegate(string said)
            {
                int ticks;
                if (int.TryParse(said, NumberStyles.None, CultureInfo.InvariantCulture, out ticks) && ticks > 0)
                    chosen(ticks);
            });

            return page;
        }

        /// <summary>Whether what is typed can still become a number of ticks.</summary>
        /// <remarks>
        /// Digits only, and few enough of them to fit, so the box refuses anything else as it is typed
        /// rather than taking it and doing nothing on Enter. Empty is allowed, so it can be cleared.
        /// </remarks>
        static bool WholeTicks(string said)
        {
            if (string.IsNullOrEmpty(said))
                return true;

            if (said.Length > 9)
                return false;

            for (var i = 0; i < said.Length; i++)
            {
                if (said[i] < '0' || said[i] > '9')
                    return false;
            }

            return true;
        }

        void EditWait(Wait step)
        {
            var origin = Editor.Depth;
            Editor.Push(WaitPage(delegate(int ticks)
            {
                Editor.Do(_sequence, new SetWait(_sequence.Steps.IndexOf(step), step.Ticks, ticks));
                Editor.UnwindTo(origin);
            }));
        }

        void RemoveStep(int index)
        {
            var going = new List<Step>();
            for (var i = 0; i < Spanned(index); i++)
                going.Add(_sequence.Steps[index + i]);

            Editor.Do(_sequence, new RemoveStep(index, going.ToArray()));
        }

        /// <summary>
        /// Moves the row somebody dragged to where they dropped it.
        /// </summary>
        /// <remarks>
        /// Both ends arrive as rows on screen rather than as lines in the sequence, because a row is
        /// what a person took hold of. A row is a step and the await after it.
        /// </remarks>
        void Move(int from, int to)
        {
            if (from < 0 || from >= _shown.Count || to < 0 || to >= _shown.Count || from == to)
                return;

            var start = _shown[from];
            var count = Spanned(start);

            // Where the run lands: the line the row it is going to starts on, less the run itself
            // where it is moving down the list.
            var landing = _shown[to];
            if (to > from)
                landing = landing + Spanned(landing) - count;

            Editor.Do(_sequence, new MoveStep(start, landing, count));
        }

        /// <summary>
        /// Renaming deliberately does not rebuild the screen. The shape has not
        /// changed, and rebuilding would recreate the field mid-word and throw away
        /// the caret.
        /// </summary>
        void SetName(string value)
        {
            // Refused here as well as wherever it was typed, because a target is the part that can be
            // written wrong - and cut to length in the one place that owns what a name may be.
            var wanted = Naming.Clean(value);
            if (wanted.Length == 0)
                return;

            // Folded into one, because a name arrives a letter at a time.
            Editor.Do(_sequence, new Rename(_sequence.Name, wanted), true);
        }

        /// <summary>
        /// Running or parked, which is not one of the things undo covers.
        /// </summary>
        /// <remarks>
        /// Undo walks back what a sequence *is*, not whether it is running. Recording the
        /// switch would mean a press of undo could arm a system - somebody stepping back
        /// through their edits and having the airlock quietly go live behind them, which is
        /// the exact accident the switch exists to prevent.
        ///
        /// Nothing is lost by leaving it out: the switch is one press either way and it is the
        /// most visible control on the screen.
        /// </remarks>
        void SetEnabled(bool on)
        {
            _sequence.Enabled = on;
            Editor.Invalidate();
        }

    }
}
