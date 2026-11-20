using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs
{
    /// <summary>
    /// One form a program can hold that no upgrade carries over safely: how a step in that form is found, what it is warned
    /// with, and what the editor does differently while a person mends it.
    /// </summary>
    /// <remarks>
    /// Never removed while a stored program can still hold its form. A repair knows nothing of the others; each is asked in the
    /// order <see cref="ProgramRepairs" /> lists them. A step needing one still runs, so it is a warning.
    /// </remarks>
    public abstract class Repair
    {
        /// <summary>The problem a step needing it is marked with, whose number is stored in saves.</summary>
        public abstract SequenceProblem Problem { get; }

        /// <summary>Whether the form is in the logic system itself, the same in every installation, rather than in what one installation holds.</summary>
        public abstract bool IsAboutSystem { get; }

        /// <summary>The first step of <paramref name="sequence" /> on <paramref name="through" /> in the form, its blocks found through <paramref name="bindings" />; or none.</summary>
        /// <remarks>Asked with the program's check, after every ordinary one, and on no tick.</remarks>
        public abstract SequenceFault Find(Bindings bindings, Sequence sequence, SystemInstance through);

        /// <summary>What a step in the form is warned with, in <paramref name="texts" />.</summary>
        public abstract string Warning(Texts texts, SequenceFault fault);

        /// <summary>
        /// The slot of <paramref name="system" /> a pick of the host type <paramref name="type" /> in place of the list
        /// <paramref name="replacing" /> changes in place to hold it, mending the form, as <paramref name="shown" /> fills it and
        /// its blocks are found through <paramref name="bindings" />; or null where this repair changes nothing about the pick.
        /// </summary>
        /// <remarks>Asked as the picks change, never on a tick.</remarks>
        public virtual Part Reslots(Bindings bindings, AutomationSystem system, SystemInstance shown, IList<string> replacing,
            HostType type)
        {
            return null;
        }

        /// <summary>
        /// The words beside a new slot's name in the picker's heading, where a pick of <paramref name="type" /> in place of the
        /// list <paramref name="replacing" /> makes a slot instead of mending one of <paramref name="system" /> in place; or
        /// empty where this repair has nothing to say about it.
        /// </summary>
        public virtual string Instead(Texts texts, AutomationSystem system, IList<string> replacing, HostType type)
        {
            return "";
        }

        /// <summary>
        /// The lists of steps and triggers of <paramref name="system" />, other than <paramref name="replacing" />, that move with
        /// it onto the slot a pick of <paramref name="type" /> makes in place of the one it names, as <paramref name="shown" />
        /// fills it and its blocks are found through <paramref name="bindings" />; empty where none do.
        /// </summary>
        public virtual List<IList<string>> Carried(Bindings bindings, AutomationSystem system, SystemInstance shown, IList<string> replacing,
            HostType type)
        {
            return new List<IList<string>>();
        }

        /// <summary>
        /// What opening the picker on <paramref name="step" /> of <paramref name="system" /> will do to mend it, asked before it
        /// opens, as <paramref name="shown" /> fills the slots and their blocks are found through <paramref name="bindings" />; or
        /// empty where the step is not in this repair's form.
        /// </summary>
        /// <remarks>Asked as the step is pressed, never on a tick.</remarks>
        public virtual string Ahead(Texts texts, Bindings bindings, AutomationSystem system, SystemInstance shown, Act step)
        {
            return "";
        }

        /// <summary>The answer to <see cref="Ahead" /> that opens the picker, the one Enter takes.</summary>
        public virtual string Proceed(Texts texts)
        {
            return "";
        }

        /// <summary>The answer to <see cref="Ahead" /> that leaves the step as it is.</summary>
        public virtual string Cancel(Texts texts)
        {
            return "";
        }
    }
}
