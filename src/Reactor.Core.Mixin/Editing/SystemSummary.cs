using System.Collections.Generic;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
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
        const string Running = "running";
        const string Idle = "";

        readonly static List<string> Names = new List<string>();

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
        public static string Describe(IWorld world, AutomationSystem system)
        {
            if (system.HasName)
                return system.Name;

            Names.Clear();
            CollectEntityNames(world, system, Names);

            var common = CommonPrefix(Names);
            if (common.Length > 0)
                return common;

            return "System " + system.Id;
        }

        /// <summary>What one of a system is called: Airlock 1, or whatever it was renamed to.</summary>
        public static string DescribeInstance(IWorld world, AutomationSystem system, SystemInstance instance)
        {
            if (instance.HasName)
                return instance.Name;

            return Describe(world, system) + " " + instance.Number;
        }

        /// <summary>
        /// How many of this system are fitted.
        /// </summary>
        /// <remarks>
        /// Without naming what they are fitted to. Saying "on this ship" is the same mistake
        /// <see cref="EntityNoun"/> exists to prevent one level up - not every construct is a ship, and
        /// no game but this one has ships at all. Nowhere needs the word: the screen is already showing
        /// the thing.
        /// </remarks>
        public static string Count(AutomationSystem system)
        {
            return "fitted " + system.Instances.Count + (system.Instances.Count == 1 ? " time" : " times");
        }

        /// <summary>Whether anything in this system is doing something.</summary>
        public static bool IsRunning(AutomationSystem system, AutomationRunner runner)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var state = runner.Get(sequences[i]);
                if (state != null && state.IsRunning)
                    return true;
            }
            return false;
        }

        public static string RunningText(bool running)
        {
            return running ? Running : Idle;
        }

        static void CollectEntityNames(IWorld world, AutomationSystem system, List<string> result)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var sequence = sequences[i];

                var when = sequence.Trigger as WhenTrigger;
                if (when != null)
                    AddNames(world, when.Sources, result);

                for (var s = 0; s < sequence.Steps.Count; s++)
                {
                    var step = sequence.Steps[s];
                    AddNames(world, step.Targets, result);

                    var gate = step.Gate as OnceGate;
                    if (gate != null)
                        AddNames(world, gate.Condition.Entities, result);
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

        static void AddNames(IWorld world, IList<long> ids, List<string> result)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                var entity = world.FindEntity(ids[i]);
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
