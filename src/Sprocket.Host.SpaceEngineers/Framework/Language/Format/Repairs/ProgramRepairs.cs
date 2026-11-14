using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs
{
    /// <summary>Every repair a stored program can need, in the order they arrived, and what the check, the words and the picker ask of them.</summary>
    public static class ProgramRepairs
    {
        // In the order they arrived; an immutable table, read by every check.
        static readonly Repair[] All =
        {
            new ScreenOnBlockSlot(),
        };

        /// <summary>The repair whose problem is <paramref name="problem" />, or null where it is no repair's.</summary>
        public static Repair Of(SequenceProblem problem)
        {
            foreach (var repair in All)
            {
                if (repair.Problem == problem)
                    return repair;
            }

            return null;
        }

        /// <summary>The first step of <paramref name="sequence" /> on <paramref name="through" /> that any repair finds, or none.</summary>
        /// <remarks>Asked with the program's check, after every ordinary one, and on no tick.</remarks>
        public static SequenceFault Find(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            foreach (var repair in All)
            {
                var found = repair.Find(bindings, sequence, through);
                if (found.Exists)
                    return found;
            }

            return SequenceFault.None;
        }

        /// <summary>
        /// The slot of <paramref name="system" /> a pick of <paramref name="type" /> in place of <paramref name="replacing" />
        /// changes in place to hold it, and the repair it mends as <paramref name="by" />; or null where none does.
        /// </summary>
        /// <remarks>Asked as the picks change, never on a tick.</remarks>
        public static Part Reslots(Bindings bindings, AutomationSystem system, SystemInstance shown, IList<string> replacing,
            HostType type, out Repair by)
        {
            foreach (var repair in All)
            {
                var part = repair.Reslots(bindings, system, shown, replacing, type);
                if (part == null)
                    continue;

                by = repair;
                return part;
            }

            by = null;
            return null;
        }

        /// <summary>
        /// The lists of steps and triggers of <paramref name="system" />, other than <paramref name="replacing" />, that every
        /// repair moves with it onto the slot a pick of <paramref name="type" /> makes in place of the one it names.
        /// </summary>
        /// <remarks>Asked once, as the pick is made into a slot.</remarks>
        public static List<IList<string>> Carried(Bindings bindings, AutomationSystem system, SystemInstance shown,
            IList<string> replacing, HostType type)
        {
            var carried = new List<IList<string>>();
            foreach (var repair in All)
                carried.AddRange(repair.Carried(bindings, system, shown, replacing, type));

            return carried;
        }

        /// <summary>
        /// What opening the picker on <paramref name="step" /> of <paramref name="system" /> will do to mend it, and the repair
        /// that says it as <paramref name="by" />; or empty where no repair has anything to say about the step.
        /// </summary>
        /// <remarks>Asked as the step is pressed, never on a tick.</remarks>
        public static string Ahead(Texts texts, Bindings bindings, AutomationSystem system, SystemInstance shown, Act step, out Repair by)
        {
            foreach (var repair in All)
            {
                var said = repair.Ahead(texts, bindings, system, shown, step);
                if (said.Length == 0)
                    continue;

                by = repair;
                return said;
            }

            by = null;
            return "";
        }

        /// <summary>
        /// The words beside a new slot's name where a pick of <paramref name="type" /> in place of <paramref name="replacing" />
        /// makes one instead of mending one of <paramref name="system" />, and the repair that says them as <paramref name="by" />;
        /// or empty where none does.
        /// </summary>
        public static string Instead(Texts texts, AutomationSystem system, IList<string> replacing, HostType type, out Repair by)
        {
            foreach (var repair in All)
            {
                var said = repair.Instead(texts, system, replacing, type);
                if (said.Length == 0)
                    continue;

                by = repair;
                return said;
            }

            by = null;
            return "";
        }
    }
}
