using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// What somebody says the parameters of one of the game's own actions are.
    /// </summary>
    /// <remarks>Read by the game's XML serializer from a file a mod ships, so its names are a persisted format.</remarks>
    public sealed class Description
    {
        /// <summary>The game's own id for the action, which is what a program and a slot hold.</summary>
        [XmlAttribute("id")]
        public string Id;

        /// <summary>
        /// Which block it is on, as a full definition id, or empty for any block that offers it.
        /// </summary>
        [XmlAttribute("block")]
        public string Block;

        /// <summary>Its parameters, in the order the action declares them.</summary>
        [XmlElement("parameter", typeof(Parameter))]
        [XmlElement("group", typeof(ParameterGroup))]
        public List<ParameterPiece> Pieces = new List<ParameterPiece>();

        /// <summary>
        /// How many of the action's parameters are accounted for.
        /// </summary>
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
        static FieldEditor Drawn(string kind)
        {
            return kind == "color" ? FieldEditor.Color : FieldEditor.Typed;
        }

        static Field Asks(Parameter one)
        {
            return Field.Between(one.Name, one.Least, one.Most);
        }
    }
}
