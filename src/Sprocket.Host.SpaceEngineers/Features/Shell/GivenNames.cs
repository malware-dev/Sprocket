using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>The names installations and sequences are given when they come into being, as the game names a new block.</summary>
    /// <remarks>A name is given once and kept; only the player changes it afterwards.</remarks>
    public static class GivenNames
    {
        /// <summary>
        /// Gives every installation and sequence of <paramref name="program" /> that has no name one, in the words
        /// <paramref name="texts" /> give: the name it has been shown as, where that is free, and otherwise the next free number.
        /// Whether it named anything.
        /// </summary>
        public static bool Give(Automation program, Texts texts)
        {
            var gave = false;
            foreach (var system in program.Systems)
            {
                var instances = system.Instances;
                for (var i = 0; i < instances.Count; i++)
                {
                    if (instances[i].HasName)
                        continue;

                    var shown = SystemSummary.DescribeInstance(texts, "", i + 1);
                    instances[i].Name = InstanceFree(system, shown) ? shown : SystemSummary.NewInstanceName(texts, system);
                    gave = true;
                }

                foreach (var sequence in system.Sequences)
                {
                    if (sequence.HasName)
                        continue;

                    var shown = SequenceText.DefaultName(texts, sequence.Number);
                    sequence.Name = SequenceFree(system, shown) ? shown : SequenceText.NewName(texts, system);
                    gave = true;
                }
            }

            return gave;
        }

        /// <summary>Whether no installation of <paramref name="system" /> is called <paramref name="name" />.</summary>
        static bool InstanceFree(AutomationSystem system, string name)
        {
            foreach (var instance in system.Instances)
            {
                if (instance.Name == name)
                    return false;
            }

            return true;
        }

        /// <summary>Whether no sequence of <paramref name="system" /> is called <paramref name="name" />.</summary>
        static bool SequenceFree(AutomationSystem system, string name)
        {
            foreach (var sequence in system.Sequences)
            {
                if (sequence.Name == name)
                    return false;
            }

            return true;
        }
    }
}
