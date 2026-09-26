using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui.Rich;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// Words a system's name and what it is doing.
    /// </summary>
    /// <remarks>
    /// Naming is worked out rather than demanded. Nothing is declared before it is used, so a
    /// system exists the moment somebody builds something and has no name until they care to
    /// give it one - and in the meantime it still has to be called something on screen.
    /// </remarks>
    static class SystemSummary
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

        /// <summary>What one of a system is called: Airlock 1, or whatever it was renamed to.</summary>
        public static string DescribeInstance(Texts texts, Bindings bindings, AutomationSystem system,
            SystemInstance instance)
        {
            if (instance.HasName)
                return instance.Name;

            var numbered = Template.Fill(texts.SystemsInstance, "number", instance.Number.ToString());
            return Template.Fill(numbered, "name", Describe(texts, bindings, system));
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

                var instances = system.Instances;
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

        /// <summary>
        /// How many of a system's sequences have something wrong with them.
        /// </summary>
        /// <remarks>
        /// On a cadence rather than on a tick: <see cref="SequenceSummary.Fault" /> may send a host
        /// off to read something expensive.
        /// </remarks>
        public static int Problems(GridWorld world, Bindings bindings, AutomationRunner runner,
            AutomationSystem system, SystemInstance instance)
        {
            var count = 0;
            var sequences = system.Sequences;

            for (var i = 0; i < sequences.Count; i++)
            {
                SystemInstance through = instance ?? system.First;
                SequenceRunner state = runner.Get(sequences[i], through);

                var fault = SequenceSummary.Fault(world, bindings, sequences[i], state, through);
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
        public static int Remarks(GridWorld world, Bindings bindings, AutomationRunner runner,
            AutomationSystem system, SystemInstance instance)
        {
            var count = 0;
            var sequences = system.Sequences;

            for (var i = 0; i < sequences.Count; i++)
            {
                SystemInstance through = instance ?? system.First;
                SequenceRunner state = runner.Get(sequences[i], through);

                var fault = SequenceSummary.Fault(world, bindings, sequences[i], state, through);
                if (fault.Problem == SequenceProblem.Conflicted)
                    count++;
            }

            return count;
        }

        static void CollectEntityNames(Bindings bindings, AutomationSystem system, List<string> result)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var sequence = sequences[i];

                var moment = sequence.Trigger as MomentTrigger;
                if (moment != null)
                    AddNames(bindings, SystemInstance.Through(system.First, moment.Sources), result);

                for (var s = 0; s < sequence.Steps.Count; s++)
                {
                    var act = sequence.Steps[s] as Act;
                    if (act != null)
                        AddNames(bindings, SystemInstance.Through(system.First, act.Targets), result);
                }
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
