using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A type the host provides: things found on a block that are not blocks, like a screen.</summary>
    /// <remarks>
    /// Described once per definition, as a capability is, from a block standing for every block of it, and holding nothing
    /// about any one block. A thing of the type is a block and the number passed to its function: screen 2 of a board. A
    /// slot that expects the type holds one of each of its blocks, and a member of the type acts on that one.
    /// </remarks>
    public abstract class HostType
    {
        /// <summary>The type as a slot expecting it names it, qualified: <c>ext.screen</c>.</summary>
        public abstract string Id { get; }

        /// <summary>The function that returns one from a block, as an answer writes it: <c>screen</c>, in <c>ext.screen(2) on [ board ]</c>.</summary>
        public abstract string Function { get; }

        /// <summary>Whether a block of this definition has anything of this type.</summary>
        public abstract bool Claims(IMyTerminalBlock block);

        /// <summary>What a block of this definition has of this type, each by its number from 1 and its name there, in order.</summary>
        public abstract void Offered(IMyTerminalBlock block, List<HostTypeFacet> into);

        /// <summary>
        /// What the facet numbered <paramref name="number" /> of <paramref name="block" /> is doing now, in the words its members
        /// read it in; empty where the block has no such one.
        /// </summary>
        /// <remarks>Read for the line under a picker's choice of one, as that is shown, never on a tick.</remarks>
        public abstract string Showing(IMyTerminalBlock block, int number);

        /// <summary>Adds to <paramref name="kind" /> the members of this type, each said to be one: see <see cref="EntityAction.Type" />.</summary>
        public abstract void Describe(EntityKind kind, IMyTerminalBlock block);

        /// <summary>
        /// What a member of a type receives: the number of the facet it acts on, from 1, the first where <paramref name="number" />
        /// names none, then <paramref name="arguments" />.
        /// </summary>
        /// <remarks>Made as a step or a check is worked out, never on a tick.</remarks>
        public static List<Literal> Arguments(int number, IList<Literal> arguments)
        {
            var called = new List<Literal>(arguments.Count + 1) { Literal.Of(number < 1 ? 1 : number) };
            called.AddRange(arguments);
            return called;
        }

        /// <summary>The facet numbered <paramref name="number" /> a block of <paramref name="kind" /> has, or null where it has no such one.</summary>
        /// <remarks>How an answer is checked against a block, and how a picker names what a slot holds of it.</remarks>
        public HostTypeFacet Find(EntityKind kind, int number)
        {
            foreach (var facet in kind.Facets(this))
            {
                if (facet.Number == number)
                    return facet;
            }

            return null;
        }
    }
}
