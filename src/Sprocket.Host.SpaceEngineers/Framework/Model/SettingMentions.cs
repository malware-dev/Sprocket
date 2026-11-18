using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Where a system blueprint's steps, tests and triggers name its settings, in place of a value or as what an <c>if</c> tests, and those names changed everywhere.</summary>
    /// <remarks>Walks what it is given, so it is for a check or an edit, never for a tick.</remarks>
    public static class SettingMentions
    {
        /// <summary>Adds to <paramref name="into" /> every setting <paramref name="step" /> names, each once.</summary>
        public static void Named(Step step, List<string> into)
        {
            Walk(step, into, null, null);
        }

        /// <summary>Adds to <paramref name="into" /> every setting <paramref name="trigger" /> names, in what starts it and in the step it does, each once.</summary>
        public static void Named(Trigger trigger, List<string> into)
        {
            Walk(trigger, into, null, null);
        }

        /// <summary>Adds to <paramref name="into" /> every setting a state of <paramref name="system" /> starts at, each once.</summary>
        public static void Started(AutomationSystem system, List<string> into)
        {
            foreach (var state in system.States)
                Walk(state.Start, into, null, null);
        }

        /// <summary>Adds to <paramref name="into" /> every setting anything in <paramref name="system" /> names, each once: its states, triggers and steps.</summary>
        public static void Named(AutomationSystem system, List<string> into)
        {
            Started(system, into);
            foreach (var trigger in system.Triggers)
                Walk(trigger, into, null, null);

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                    Walk(step, into, null, null);
            }
        }

        /// <summary>Has everything in <paramref name="system" /> that names the setting <paramref name="was" /> name <paramref name="now" /> instead.</summary>
        public static void Rename(AutomationSystem system, string was, string now)
        {
            foreach (var state in system.States)
                state.Start = Walk(state.Start, null, was, now);

            foreach (var trigger in system.Triggers)
                Walk(trigger, null, was, now);

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                    Walk(step, null, was, now);
            }
        }

        static void Walk(Trigger trigger, List<string> into, string was, string now)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
            {
                Walk(when.Arguments, into, was, now);
                when.Value = Walk(when.Value, into, was, now);
                when.IntervalSetting = Walk(when.IntervalSetting, into, was, now);
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                every.IntervalSetting = Walk(every.IntervalSetting, into, was, now);

            var watched = trigger as StateTrigger;
            if (watched != null)
                Walk(watched.Test, into, was, now);

            Walk(trigger.Does, into, was, now);
        }

        static void Walk(Step step, List<string> into, string was, string now)
        {
            if (step == null)
                return;

            Walk(step.Arguments, into, was, now);

            var branch = step as Branch;
            var compared = branch?.Compared;
            if (compared != null)
                compared.Value = Walk(compared.Value, into, was, now);
            else
                Walk(branch?.Test as StateTest, into, was, now);

            var wait = step as Wait;
            if (wait != null)
                wait.Setting = Walk(wait.Setting, into, was, now);

            var abort = step as Abort;
            if (abort != null)
                abort.Setting = Walk(abort.Setting, into, was, now);

            var sets = step as SetState;
            if (sets != null && sets.From.Length == 0)
                sets.Value = Walk(sets.Value, into, was, now);
        }

        static void Walk(StateTest test, List<string> into, string was, string now)
        {
            if (test == null)
                return;

            test.Setting = Walk(test.Setting, into, was, now);
            if (test.Other.Length == 0)
                test.Value = Walk(test.Value, into, was, now);
        }

        static void Walk(IList<Literal> given, List<string> into, string was, string now)
        {
            for (var i = 0; i < given.Count; i++)
            {
                if (given[i].IsSetting)
                    given[i] = Walk(given[i], into, was, now);
            }
        }

        static Literal Walk(Literal value, List<string> into, string was, string now)
        {
            return value.IsSetting ? Literal.Setting(Walk(value.Text, into, was, now), value.IsQualified) : value;
        }

        /// <summary>The setting named <paramref name="name" />, added to <paramref name="into" /> where it is not there, and renamed where it is <paramref name="was" />.</summary>
        static string Walk(string name, List<string> into, string was, string now)
        {
            if (name.Length == 0)
                return name;

            if (into != null && !into.Contains(name))
                into.Add(name);

            return name == was ? now : name;
        }
    }
}
