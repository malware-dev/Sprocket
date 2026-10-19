using System.Globalization;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>The words for a system's name and what it is doing.</summary>
    public static class SystemSummary
    {

        /// <summary>
        /// What to call this system: what the player called it, what its blocks suggest, or
        /// its number.
        /// </summary>
        /// <remarks>Called when a screen is built, never every tick: it walks names and splits strings.</remarks>
        public static string Describe(Texts texts, Bindings bindings, AutomationSystem system)
        {
            return Describe(texts, Named(bindings, system), system.Id);
        }

        /// <summary>What to call a system named <paramref name="named" />, as <see cref="Named" /> gives it, and numbered <paramref name="number" />.</summary>
        public static string Describe(Texts texts, string named, int number)
        {
            return named.Length > 0 ? named : Template.Fill(texts.SystemsNumbered, "number", number.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>What the player called this system, or what its blocks suggest, or empty where neither says anything.</summary>
        /// <remarks>Walks names and splits strings, so it is asked when something changes, never every tick.</remarks>
        public static string Named(Bindings bindings, AutomationSystem system)
        {
            if (system.HasName)
                return system.Name;

            var names = new List<string>();
            CollectEntityNames(bindings, system, names);
            return CommonPrefix(names);
        }

        /// <summary>What an installation of a system is called.</summary>
        public static string DescribeInstance(Texts texts, Bindings bindings, AutomationSystem system,
            SystemInstance instance)
        {
            return DescribeInstance(texts, instance.HasName ? instance.Name : "", Position(system, instance));
        }

        /// <summary>What an installation called <paramref name="named" />, or with no name where it is empty, and listed <paramref name="position" />th is called.</summary>
        public static string DescribeInstance(Texts texts, string named, int position)
        {
            return named.Length > 0 ? named : Template.Fill(texts.SystemsInstance, "number", position.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Where <paramref name="instance" /> stands among <paramref name="system" />'s installations, from 1.</summary>
        public static int Position(AutomationSystem system, SystemInstance instance)
        {
            var at = 0;
            var instances = system.Instances;
            while (at < instances.Count && instances[at] != instance)
                at++;

            return at + 1;
        }

        /// <summary>The name a new installation of <paramref name="system" /> is given: the first number free, counting on from the installations there are.</summary>
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

        /// <summary>Whether any sequence of this system is running on <paramref name="instance" />, or on any installation where it is null.</summary>
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

        /// <summary>What a row says while its logic system is <paramref name="running" />, in <paramref name="texts" />, or empty while it is not.</summary>
        public static string RunningText(Texts texts, bool running)
        {
            return running ? texts.SystemsRunning : "";
        }

        /// <summary>The first of a system's sequences, or of its triggers' own acts, with something wrong with it on <paramref name="instance" />, and what, or null where none has.</summary>
        /// <remarks>Costs a lookup per run.</remarks>
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

        /// <summary>How many of a system's sequences, and of its triggers' own acts, have something wrong with them.</summary>
        /// <remarks>Costs a lookup per run.</remarks>
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

        /// <summary>How many of a system's sequences gave up a run because another sequence took one of its blocks.</summary>
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

        /// <summary>The whole words every name begins with, or empty.</summary>
        static string CommonPrefix(List<string> names)
        {
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

            if (shared == words.Length)
                return "";

            var result = "";
            for (var i = 0; i < shared; i++)
                result = i == 0 ? words[i] : $"{result} {words[i]}";

            return result;
        }
    }
}
