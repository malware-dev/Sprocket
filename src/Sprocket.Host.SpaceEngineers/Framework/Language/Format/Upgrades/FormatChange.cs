using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>One change to the format, and how a program written before it is carried across it.</summary>
    /// <remarks>
    /// Never removed, since every program written before it reads through it. A change knows nothing of the others; a
    /// program runs every change that arrived after the draft it was written for, in the order they arrived.
    /// </remarks>
    public abstract class FormatChange
    {
        /// <summary>The draft of the format the change arrived in.</summary>
        public abstract int Arrived { get; }

        /// <summary>Rewrites <paramref name="constructs" />, read from a program written before the change, into its form, before they are read.</summary>
        public virtual void Statements(List<Statement> constructs)
        {
        }

        /// <summary>
        /// Carries <paramref name="program" />, read from text written before the change, across it, the blocks it names found
        /// in <paramref name="world" /> where there is one.
        /// </summary>
        /// <returns>What it changed, for the log, or empty where nothing needed it.</returns>
        public virtual string Program(Automation program, GridWorld world)
        {
            return "";
        }
    }
}
