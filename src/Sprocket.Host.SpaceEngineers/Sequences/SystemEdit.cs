using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One change to a system: what starts its sequences, and what each of those runs.
    /// </summary>
    public abstract class SystemEdit : Edit<AutomationSystem>
    {
        /// <summary>
        /// Whether that trigger is still in the system, saying what it was left saying.
        /// </summary>
        protected static bool Holds(AutomationSystem system, Trigger trigger, IList<string> sources,
            string eventId)
        {
            if (trigger == null || system.IndexOfTrigger(trigger) < 0)
                return false;

            var moment = trigger as MomentTrigger;
            if (moment == null)
                return true;

            return moment.EventId == eventId && Same(moment.Sources, sources);
        }

        /// <summary>Waits for the logic system's switch, as making it did.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return system.Enabled;
        }

        /// <summary>Whether <paramref name="instance" /> is one of <paramref name="system" />'s installations.</summary>
        protected static bool Has(AutomationSystem system, SystemInstance instance)
        {
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i] == instance)
                    return true;
            }

            return false;
        }

        /// <summary>Whether that trigger is not in the system at all.</summary>
        protected static bool Gone(AutomationSystem system, Trigger trigger)
        {
            return trigger != null && system.IndexOfTrigger(trigger) < 0;
        }
    }
}
