using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One change to a system: what starts its sequences, and what each of those runs.
    /// </summary>
    /// <remarks>
    /// A trigger belongs to the system rather than to the sequence it runs, so taking one back is
    /// not something a sequence's history could hold - two triggers may run one sequence, and a
    /// trigger that does one plain thing runs no sequence at all. These are walked back from the
    /// system's screen, which is where they are made.
    /// </remarks>
    public abstract class SystemEdit : Edit<AutomationSystem>
    {
        /// <summary>
        /// Whether that trigger is still in the system, saying what it was left saying.
        /// </summary>
        /// <remarks>
        /// The same blocks and the same moment is enough, which is the whole of what somebody can
        /// change from outside the editor. Nothing compares the object it used to be: a trigger that
        /// says the same thing about the same blocks is the same trigger to the person who made it.
        /// <para>
        /// A repeat has nothing anybody outside could have changed, so being there is the whole
        /// question for one.
        /// </para>
        /// </remarks>
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

        /// <summary>Whether that trigger is not in the system at all.</summary>
        protected static bool Gone(AutomationSystem system, Trigger trigger)
        {
            return trigger != null && system.IndexOfTrigger(trigger) < 0;
        }
    }
}
