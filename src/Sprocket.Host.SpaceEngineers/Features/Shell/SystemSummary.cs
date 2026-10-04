using System.Globalization;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// Words a system's name and what it is doing.
    /// </summary>
    /// <remarks>
    /// Naming is worked out rather than demanded. Nothing is declared before it is used, so a
    /// system exists the moment somebody builds something and has no name until they care to
    /// give it one - and in the meantime it still has to be called something on screen.
    /// </remarks>
    public static class SystemSummary
    {

        /// <summary>
        /// What to call this system: what the player called it, what its blocks suggest, or
        /// its number.
        /// </summary>
        /// <remarks>
        /// The suggestion is the leading words its blocks have in common. People prefix block
        /// names constantly - Airlock Inner Door, Airlock Outer Door, Airlock Button - so the
        /// common part is usually the name of the system, and using it costs nothing and
        /// reads as having been understood rather than interrogated.
        ///
        /// Worked out when the screen is built, never on a tick: it walks names and splits
        /// strings, which is not something to do sixty times a second.
        /// </remarks>
        public static string Describe(Texts texts, Bindings bindings, AutomationSystem system)
        {
            if (system.HasName)
                return system.Name;

            var names = new List<string>();
            CollectEntityNames(bindings, system, names);

            var common = CommonPrefix(names);
            if (common.Length > 0)
                return common;

            return Template.Fill(texts.SystemsNumbered, "number", system.Id.ToString());
        }

        /// <summary>What an installation of a system is called.</summary>
        /// <remarks>Every installation is named as it is made; one read from a program that names none is called by its place among them.</remarks>
        public static string DescribeInstance(Texts texts, Bindings bindings, AutomationSystem system,
            SystemInstance instance)
        {
            if (instance.HasName)
                return instance.Name;

            var at = 0;
            var instances = system.Instances;
            while (at < instances.Count && instances[at] != instance)
                at++;

            return Template.Fill(texts.SystemsInstance, "number", (at + 1).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>The name a new installation of <paramref name="system" /> is given: the first number free, counting on from the installations there are.</summary>
        /// <remarks>Unrelated to the installation's own number, which only tells it from the others.</remarks>
        public static string NewInstanceName(Texts texts, AutomationSystem system)
        {
            var instances = system.Instances;
            for (var number = instances.Count + 1; ; number++)
            {
                var wanted = Template.Fill(texts.SystemsInstance, "number", number.ToString(CultureInfo.InvariantCulture));
                var taken = false;
                for (var i = 0; i < instances.Count && !taken; i++)
                    taken = instances[i].Name == wanted;

                if (!taken)
                    return wanted;
            }
        }

        /// <summary>What an installation is called where it does not stand under its system, as in a question: Installation 1 of Airlock.</summary>
        public static string DescribeInstanceOf(Texts texts, Bindings bindings, AutomationSystem system, SystemInstance instance)
        {
            var named = Template.Fill(texts.SystemsInstanceOf, "installation", DescribeInstance(texts, bindings, system, instance));
            return Template.Fill(named, "name", Describe(texts, bindings, system));
        }

        /// <summary>
        /// Whether anything in this system is doing something, on one of it or on any.
        /// </summary>
        /// <remarks>
        /// The sequences belong to the system and run once for each one of it, so which one is asked
        /// about is the whole question: the port airlock cycling is not the front one cycling, and a
        /// row that said otherwise would be pointing at the wrong doors. A null instance asks about
        /// any of them, which is the honest answer where the screen is showing the system rather than
        /// one of it.
        /// </remarks>
        public static bool IsRunning(AutomationSystem system, AutomationRunner runner,
            SystemInstance instance)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (instance != null)
                {
                    var one = runner.Get(sequences[i], instance);
                    if (one != null && one.IsRunning)
                        return true;

                    continue;
                }

                var instances = system.Uses;
                for (var n = 0; n < instances.Count; n++)
                {
                    var any = runner.Get(sequences[i], instances[n]);
                    if (any != null && any.IsRunning)
                        return true;
                }
            }

            return false;
        }

        public static string RunningText(Texts texts, bool running)
        {
            return running ? texts.SystemsRunning : "";
        }

        /// <summary>The first of a system's sequences, or of its triggers' own acts, with something wrong with it on <paramref name="instance" />, and what, or null where none has.</summary>
        /// <remarks>A lookup per run: what is wrong is found where it happens and kept by each run's runner.</remarks>
        public static Sequence Failing(AutomationRunner runner, AutomationSystem system, SystemInstance instance,
            out SequenceFault fault)
        {
            var sequences = system.Sequences;
            var through = instance ?? system.First;

            for (var i = 0; i < sequences.Count; i++)
            {
                fault = SequenceSummary.Fault(runner.Get(sequences[i], through));
                if (fault.Exists && !fault.IsWarning)
                    return sequences[i];
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var alone = runner.Alone(system, triggers[i]);
                fault = alone == null ? SequenceFault.None : SequenceSummary.Fault(runner.Get(alone, through));
                if (fault.Exists && !fault.IsWarning)
                    return alone;
            }

            fault = SequenceFault.None;
            return null;
        }

        /// <summary>
        /// How many of a system's sequences have something wrong with them.
        /// </summary>
        /// <remarks>A lookup per sequence, as <see cref="Failing" /> is.</remarks>
        public static int Problems(AutomationRunner runner, AutomationSystem system, SystemInstance instance)
        {
            var count = 0;
            var through = instance ?? system.First;
            var sequences = system.Sequences;

            for (var i = 0; i < sequences.Count; i++)
            {
                var fault = SequenceSummary.Fault(runner.Get(sequences[i], through));
                if (fault.Exists && !fault.IsWarning)
                    count++;
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var alone = runner.Alone(system, triggers[i]);
                var fault = alone == null ? SequenceFault.None : SequenceSummary.Fault(runner.Get(alone, through));
                if (fault.Exists && !fault.IsWarning)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// How many of a system's sequences have something to remark on rather than something broken.
        /// </summary>
        /// <remarks>
        /// Not every warning. A system nobody has finished wiring says "nothing starts this yet"
        /// about half its sequences, and a ship's screen marking that sends somebody looking for a
        /// fault that is not there - which is why warnings went uncounted here to begin with.
        /// <para>
        /// What is counted is a warning about something that <em>happened</em>: a run that gave up
        /// because another sequence took one of its blocks. Nobody can see that from the overview
        /// otherwise, and it is the case where not saying is worse than saying too much.
        /// </para>
        /// </remarks>
        public static int Remarks(AutomationRunner runner, AutomationSystem system, SystemInstance instance)
        {
            var count = 0;
            var sequences = system.Sequences;

            for (var i = 0; i < sequences.Count; i++)
            {
                var fault = SequenceSummary.Fault(runner.Get(sequences[i], instance ?? system.First));
                if (fault.Problem == SequenceProblem.Conflicted)
                    count++;
            }

            return count;
        }

        static void CollectEntityNames(Bindings bindings, AutomationSystem system, List<string> result)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var own = triggers[i].Acts;
                if (own != null)
                    AddNames(bindings, SystemInstance.Through(system.First, own.Targets), result);
            }

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var sequence = sequences[i];

                var moment = sequence.Trigger as MomentTrigger;
                if (moment != null)
                    AddNames(bindings, SystemInstance.Through(system.First, moment.Sources), result);

                for (var s = 0; s < sequence.Steps.Count; s++)
                    AddNames(bindings, SystemInstance.Through(system.First, sequence.Steps[s].Targets), result);
            }
        }

        static bool HasTwoDistinct(List<string> names)
        {
            for (var i = 1; i < names.Count; i++)
            {
                if (!string.Equals(names[i], names[0]))
                    return true;
            }
            return false;
        }

        static void AddNames(Bindings bindings, IList<string> refs, List<string> result)
        {
            for (var i = 0; i < refs.Count; i++)
            {
                var entity = bindings.Entity(refs[i]);
                if (entity != null && !string.IsNullOrEmpty(entity.Name))
                    result.Add(entity.Name);
            }
        }

        /// <summary>
        /// The words every name begins with, or empty.
        /// </summary>
        /// <remarks>
        /// Whole words only. "Airlock Inner Door" and "Airlock Outer Door" share "Airlock",
        /// not "Airlock " and certainly not "Airlock In" - a suggestion cut mid-word reads as
        /// a bug rather than as help.
        /// </remarks>
        static string CommonPrefix(List<string> names)
        {
            // One block says nothing about the system. Naming a system after its only block
            // reads as a mistake - the system is not the button - and a number is honest
            // where a guess would be wrong.
            if (!HasTwoDistinct(names))
                return "";

            var words = names[0].Split(' ');
            var shared = words.Length;

            for (var i = 1; i < names.Count; i++)
            {
                var other = names[i].Split(' ');
                var limit = other.Length < shared ? other.Length : shared;

                var matched = 0;
                while (matched < limit && string.Equals(words[matched], other[matched]))
                    matched++;

                shared = matched;
                if (shared == 0)
                    return "";
            }

            // Sharing every word means the names are identical, which is the same as having
            // one of them.
            if (shared == words.Length)
                return "";

            var result = "";
            for (var i = 0; i < shared; i++)
                result = i == 0 ? words[i] : result + " " + words[i];

            return result;
        }
    }
}
