
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>One change to the program as a whole: a logic system or an installation made or taken out.</summary>
    /// <remarks>
    /// Walked back from the home screen, which is where these are made. A logic system taken out cannot hold the way back to
    /// itself, so the program holds it.
    /// </remarks>
    abstract class ProgramEdit : Edit<Automation>
    {
        /// <summary>Where <paramref name="system" /> stands among the program's systems, or -1.</summary>
        protected static int IndexOf(Automation program, AutomationSystem system)
        {
            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                if (systems[i] == system)
                    return i;
            }

            return -1;
        }

        /// <summary>Whether any installation of <paramref name="system" /> answers a trigger slot with a toolbar slot another installation in <paramref name="program" /> answers now.</summary>
        /// <remarks>
        /// Asked before something taken out is put back: its buttons were let go of, and one taken since by something else
        /// would end up claimed twice, which starts neither. A walk of the program's trigger slots, with no toolbar read.
        /// </remarks>
        protected static bool Clashes(Automation program, AutomationSystem system)
        {
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (Clashes(program, system, instances[i]))
                    return true;
            }

            return false;
        }

        /// <summary>The same, for the one installation <paramref name="instance" /> of <paramref name="system" />.</summary>
        protected static bool Clashes(Automation program, AutomationSystem system, SystemInstance instance)
        {
            var parts = system.Parts;
            for (var p = 0; p < parts.Count; p++)
            {
                if (!parts[p].IsTrigger)
                    continue;

                var blocks = instance.FilledWith(parts[p].Id);
                var at = instance.Positions(parts[p].Id);
                for (var i = 0; blocks != null && at != null && i < blocks.Count && i < at.Count; i++)
                {
                    if (Answered(program, instance, blocks[i], at[i]))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Whether an installation in <paramref name="program" /> other than <paramref name="except" /> answers a trigger slot with the toolbar slot <paramref name="at" /> of <paramref name="block" />.</summary>
        static bool Answered(Automation program, SystemInstance except, string block, ToolbarSlot at)
        {
            var systems = program.Systems;
            for (var s = 0; s < systems.Count; s++)
            {
                var parts = systems[s].Parts;
                var instances = systems[s].Instances;
                for (var i = 0; i < instances.Count; i++)
                {
                    if (instances[i] == except)
                        continue;

                    for (var p = 0; p < parts.Count; p++)
                    {
                        if (!parts[p].IsTrigger)
                            continue;

                        var blocks = instances[i].FilledWith(parts[p].Id);
                        var positions = instances[i].Positions(parts[p].Id);
                        for (var b = 0; blocks != null && positions != null && b < blocks.Count && b < positions.Count; b++)
                        {
                            if (blocks[b] == block && positions[b].Same(at))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>Whether <paramref name="system" /> has an installation other than <paramref name="instance" /> carrying its number.</summary>
        protected static bool Numbered(AutomationSystem system, SystemInstance instance)
        {
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i] != instance && instances[i].Number == instance.Number)
                    return true;
            }

            return false;
        }

        /// <summary>Where <paramref name="instance" /> stands among <paramref name="system" />'s installations, or -1.</summary>
        protected static int IndexOf(AutomationSystem system, SystemInstance instance)
        {
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i] == instance)
                    return i;
            }

            return -1;
        }
    }
}
