using System.Collections.Generic;
using System.Xml.Serialization;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.World
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
        [XmlElement("group", typeof(ParameterGroup))]
        public List<ParameterPiece> Pieces = new List<ParameterPiece>();

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
                foreach (var piece in Pieces)
                    covered += piece.Span;

                return covered;
            }
        }

        /// <summary>
        /// What each parameter asks for, one per parameter, in order.
        /// </summary>
        /// <remarks>
        /// Flattened, because the form asks for one value at a time whatever the grouping says. A
        /// group's own name is not here: it belongs to the editor that group asks for, and until
        /// there is one its parameters are asked for singly under their own names.
        /// </remarks>
        public void Fields(IList<Field> into)
        {
            foreach (var piece in Pieces)
            {
                var one = piece as Parameter;
                if (one != null)
                {
                    into.Add(Asks(one));
                    continue;
                }

                var group = piece as ParameterGroup;
                if (group == null)
                    continue;

                // One field for the whole group where there is a control that asks for what it is.
                // The parameters are still described one by one in the file - that is what says how
                // many values the group covers, and what order they go back in.
                FieldEditor edits = Drawn(group.Kind);
                if (edits != FieldEditor.Typed)
                {
                    into.Add(Field.Holding("", edits, group.Span));
                    continue;
                }

                foreach (var parameter in group.Parameters)
                    into.Add(Asks(parameter));
            }
        }

        /// <summary>
        /// Overwrites the type of each parameter this says the type of, one per parameter, in order.
        /// </summary>
        public void Correct(IList<Primitive> types)
        {
            var at = 0;
            foreach (var piece in Pieces)
            {
                var one = piece as Parameter;
                if (one != null)
                {
                    Correct(types, at++, one);
                    continue;
                }

                var group = piece as ParameterGroup;
                if (group == null)
                    continue;

                foreach (var parameter in group.Parameters)
                    Correct(types, at++, parameter);
            }
        }

        static void Correct(IList<Primitive> types, int at, Parameter one)
        {
            if (at < types.Count && one.Holds != Primitive.None)
                types[at] = one.Holds;
        }

        /// <summary>
        /// Which control asks for a group of this kind, or none where Sprocket has never heard of it.
        /// </summary>
        /// <remarks>
        /// A kind this build does not know is not an error and is not said anywhere: the group's
        /// parameters are asked for singly under their own names, which is what they would have got
        /// if nobody had grouped them. A file written against a later Sprocket therefore still works
        /// here, and loses only the control.
        /// </remarks>
        static FieldEditor Drawn(string kind)
        {
            return kind == "color" ? FieldEditor.Colour : FieldEditor.Typed;
        }

        static Field Asks(Parameter one)
        {
            return Field.Between(one.Name, one.Least, one.Most);
        }
    }
}
