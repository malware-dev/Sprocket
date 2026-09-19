using System.Collections.Generic;
using System.Xml.Serialization;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// What somebody says the parameters of one of the game's own actions are.
    /// </summary>
    /// <remarks>
    /// Written as a file a mod ships rather than as code it runs: nothing has to be registered,
    /// nothing has to load in an order, and a mod that describes its actions works in a world with
    /// no automation mod in it at all, because a file nobody reads is inert.
    /// <para>
    /// Public fields because the game's own serialiser reads it.
    /// </para>
    /// </remarks>
    public sealed class Description
    {
        /// <summary>The game's own id for the action, which is what a program and a slot hold.</summary>
        [XmlAttribute("id")]
        public string Id;

        /// <summary>
        /// Which block it is on, as a full definition id, or empty for any block that offers it.
        /// </summary>
        /// <remarks>
        /// An action id is unique on a block rather than in the game, so two blocks may both offer
        /// <c>SetColor</c> and mean different things by it. A mod describing its own actions knows
        /// exactly which blocks they are on; one that leaves it out is saying "wherever this id
        /// turns up", which is the looser claim and loses to the exact one.
        /// </remarks>
        [XmlAttribute("block")]
        public string Block;

        /// <summary>Its parameters, in the order the action declares them.</summary>
        [XmlElement("parameter", typeof(Parameter))]
        [XmlElement("group", typeof(Group))]
        public List<Piece> Pieces = new List<Piece>();

        /// <summary>
        /// How many of the action's parameters are accounted for.
        /// </summary>
        /// <remarks>
        /// Not the number of pieces, because a group covers as many as it holds. An action whose
        /// count does not match this is a description that has fallen out of step with the block it
        /// is about, which is said in the log rather than guessed at.
        /// </remarks>
        [XmlIgnore]
        public int Covers
        {
            get
            {
                var covered = 0;
                for (var i = 0; i < Pieces.Count; i++)
                    covered += Pieces[i].Span;

                return covered;
            }
        }

        /// <summary>
        /// What each parameter is called, one per parameter, in order.
        /// </summary>
        /// <remarks>
        /// Flattened, because the form asks for one value at a time whatever the grouping says. A
        /// group's own name is not here: it belongs to the editor that group asks for, and until
        /// there is one its parameters are asked for singly under their own names.
        /// </remarks>
        public void Names(IList<string> into)
        {
            for (var i = 0; i < Pieces.Count; i++)
            {
                var one = Pieces[i] as Parameter;
                if (one != null)
                {
                    into.Add(one.Name ?? "");
                    continue;
                }

                var group = Pieces[i] as Group;
                if (group == null)
                    continue;

                for (var p = 0; p < group.Parameters.Count; p++)
                    into.Add(group.Parameters[p].Name ?? "");
            }
        }
    }
}
