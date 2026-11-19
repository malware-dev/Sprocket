using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Copies a program in memory into an empty one, as writing it out and reading it back would make it.</summary>
    /// <remarks>
    /// Built through the calls the reader makes, so ids, numbers and order come out as a load gives them; what the switches
    /// hold comes across as the block's switches would carry it.
    /// </remarks>
    public static class ProgramCopy
    {
        /// <summary>
        /// Copies <paramref name="from" />, whose values <paramref name="fromValues" /> holds, into the empty program
        /// <paramref name="into" />, with its values made in <paramref name="intoValues" />.
        /// </summary>
        public static void Into(Automation from, Values fromValues, Automation into, Values intoValues)
        {
            into.Annotation = from.Annotation;
            into.Attributes = Kept(from.Attributes);

            foreach (var declared in from.Refs)
                into.Restore(Ref(declared, fromValues, intoValues));

            foreach (var declared in from.SignalRefs)
                into.Restore(SignalRef(declared));

            foreach (var system in from.Systems)
                System(system, into);

            foreach (var system in from.Systems)
            {
                var copy = into.FindSystem(system.Identity);
                foreach (var instance in system.Instances)
                    Instance(instance, copy);
            }

            BlockSwitches.Put(BlockSwitches.Of(from), into);
        }

        /// <summary>The block variable <paramref name="declared" /> again, its keys made in <paramref name="intoValues" />.</summary>
        static EntityRef Ref(EntityRef declared, Values fromValues, Values intoValues)
        {
            var copy = new EntityRef(declared.Name, declared.KindId);
            copy.Annotation = declared.Annotation;
            copy.Attributes = Kept(declared.Attributes);
            foreach (var key in declared.Keys)
                copy.Keys.Add(Key(key, fromValues, intoValues));

            return copy;
        }

        /// <summary><paramref name="key" /> again; text is the one kind held in the values rather than in the key itself.</summary>
        static EntityKey Key(EntityKey key, Values fromValues, Values intoValues)
        {
            if (key.Cells != null)
                return new EntityKey((VRageMath.Vector3I[])key.Cells.Clone());

            if (key.Kind != ValueKind.Text)
                return key;

            string text;
            fromValues.TryText(key.Kind, key.Value, out text);
            return new EntityKey(ValueKind.Text, intoValues.Text(text));
        }

        static SignalRef SignalRef(SignalRef declared)
        {
            var copy = new SignalRef(declared.Name, new List<string>(declared.Blocks), declared.At);
            copy.Annotation = declared.Annotation;
            copy.Attributes = Kept(declared.Attributes);
            return copy;
        }

        /// <summary>Puts <paramref name="system" /> into <paramref name="into" />, without its installations.</summary>
        static void System(AutomationSystem system, Automation into)
        {
            var copy = into.Restore(system.Identity, system.Namespace, system.Name);
            copy.Annotation = system.Annotation;
            copy.ListedOn = system.ListedOn;
            copy.Attributes = Kept(system.Attributes);
            copy.IsBlueprint = system.IsBlueprint;
            copy.Used(system.LastSequenceNumber);

            copy.Changed = system.Changed;
            copy.Origin = system.Origin;
            copy.OriginChanged = system.OriginChanged;

            foreach (var part in system.Parts)
                Part(part, copy);

            foreach (var type in system.Types)
                copy.Types.Add(Type(type));

            foreach (var state in system.States)
                copy.States.Add(State(state));

            foreach (var trigger in system.Triggers)
                copy.AddTrigger(Trigger(trigger));

            foreach (var sequence in system.Sequences)
            {
                var made = into.Restore(copy, sequence.Number, null);
                made.Annotation = sequence.Annotation;
                made.Attributes = Kept(sequence.Attributes);
                made.Name = sequence.Name;
                made.Category = sequence.Category;
                made.Exclusive = sequence.Exclusive;
                foreach (var step in sequence.Steps)
                    made.Steps.Add(Step(step));
            }
        }

        static void Part(Part part, AutomationSystem into)
        {
            var copy = into.PartFor(part.Id);
            copy.Annotation = part.Annotation;
            copy.Attributes = Kept(part.Attributes);
            copy.Short = part.Short;
            copy.Category = part.Category;
            copy.Optional = part.Optional;
            copy.IsSignal = part.IsSignal;
            copy.Holds = part.Holds;
            copy.Expects = part.Expects;
            copy.Setting = part.Setting;
            copy.HasDefault = part.HasDefault;
            copy.Default = part.Default;
        }

        static DeclaredType Type(DeclaredType type)
        {
            var copy = new DeclaredType(type.Name);
            copy.Annotation = type.Annotation;
            copy.Attributes = Kept(type.Attributes);
            foreach (var choice in type.Choices)
                copy.Choices.Add(choice);

            return copy;
        }

        static State State(State state)
        {
            var copy = new State(state.Name, state.Type, state.Start);
            copy.Annotation = state.Annotation;
            copy.Attributes = Kept(state.Attributes);
            return copy;
        }

        static Trigger Trigger(Trigger trigger)
        {
            Trigger copy;
            var every = trigger as EveryTrigger;
            var when = trigger as WhenTrigger;
            var signal = trigger as SignalTrigger;
            if (every != null)
                copy = new EveryTrigger(every.Interval) { IntervalSetting = every.IntervalSetting };
            else if (when != null)
                copy = when.Copy();
            else if (signal != null)
            {
                var made = new SignalTrigger(signal.EventId);
                made.Provider = signal.Provider;
                made.At = signal.At;
                foreach (var source in signal.Sources)
                    made.Sources.Add(source);

                copy = made;
            }
            else
                copy = new StateTrigger(((StateTrigger)trigger).Test.Copy());

            copy.Annotation = trigger.Annotation;
            copy.Category = trigger.Category;
            copy.Attributes = Kept(trigger.Attributes);
            copy.Does = Step(trigger.Does) as Run;
            return copy;
        }

        static Step Step(Step step)
        {
            var copy = Made(step);
            copy.Annotation = step.Annotation;
            copy.Attributes = Kept(step.Attributes);
            copy.IsOff = step.IsOff;
            return copy;
        }

        /// <summary>A step of the kind of <paramref name="step" />, saying what it says.</summary>
        static Step Made(Step step)
        {
            var act = step as Act;
            if (act != null)
            {
                var copy = new Act(act.ActionId);
                copy.Form = act.Form;
                copy.Provider = act.Provider;
                foreach (var target in act.Targets)
                    copy.Targets.Add(target);

                foreach (var argument in act.Arguments)
                    copy.Arguments.Add(argument.Copied);

                return copy;
            }

            var wait = step as Wait;
            if (wait != null)
                return new Wait(wait.Length) { Setting = wait.Setting };

            var run = step as Run;
            if (run != null)
                return new Run(run.Number, run.Stops) { Named = run.Named };

            if (step is Await)
                return new Await();

            var abort = step as Abort;
            if (abort != null)
                return new Abort { Message = abort.Message, Setting = abort.Setting, Level = abort.Level };

            var opens = step as If;
            if (opens != null)
                return new If(Test(opens.Test));

            var branch = step as Else;
            if (branch != null)
                return new Else(Test(branch.Test));

            if (step is EndIf)
                return new EndIf();

            if (step is Catch)
                return new Catch();

            if (step is Finally)
                return new Finally();

            var sets = (SetState)step;
            return new SetState { State = sets.State, Value = sets.Value, From = sets.From };
        }

        static BranchTest Test(BranchTest test)
        {
            if (test == null)
                return null;

            var compared = test as Comparison;
            return compared != null ? (BranchTest)compared.Copy() : ((StateTest)test).Copy();
        }

        /// <summary>Puts <paramref name="instance" /> into <paramref name="into" />, its number, name and every answer intact.</summary>
        static void Instance(SystemInstance instance, AutomationSystem into)
        {
            var copy = into.Restore(instance.Number, instance.Name);
            copy.Annotation = instance.Annotation;
            copy.Attributes = Kept(instance.Attributes);
            foreach (var slot in instance.Filled)
            {
                copy.Note(slot, instance.NoteOn(slot));
                copy.Keep(slot, Kept(instance.KeptOn(slot)));

                var blocks = new List<string>(instance.FilledWith(slot));
                var positions = instance.Positions(slot);
                var facets = instance.Facets(slot);
                if (positions != null)
                    copy.Fill(slot, blocks, new List<ToolbarSlot>(positions));
                else if (facets != null)
                    copy.Fill(slot, blocks, new List<int>(facets));
                else
                    copy.Fill(slot, blocks);
            }

            foreach (var setting in instance.Answered)
            {
                Literal value;
                instance.AnswerTo(setting, out value);
                copy.Answer(setting, value);
                copy.Note(setting, instance.NoteOn(setting));
                copy.Keep(setting, Kept(instance.KeptOn(setting)));
            }
        }

        static KeptAttributes Kept(KeptAttributes kept)
        {
            if (kept == null)
                return null;

            var copy = new KeptAttributes();
            for (var i = 0; i < kept.Count; i++)
                copy.Add(kept.Name(i), kept.Written(i));

            return copy;
        }
    }
}
