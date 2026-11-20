using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// What somebody says one of an action's parameters is.
    /// </summary>
    public sealed class Parameter : ParameterPiece
    {
        /// <summary>What to call it, or empty to leave it numbered.</summary>
        public string Name;

        /// <summary>
        /// What it is for, at length, or empty.
        /// </summary>
        public string Note;

        /// <summary>
        /// The least it may be, or empty where nobody has said.
        /// </summary>
        public string Least;

        /// <summary>The most it takes, or empty where none is given.</summary>
        public string Most;

        /// <summary>
        /// The type the action takes it as, named as .NET names it in lower case - <c>byte</c>,
        /// <c>int32</c>, <c>single</c> - or empty where the action's own declaration stands.
        /// </summary>
        public string Sort;

        /// <summary>The type the action takes it as, or none where nobody has said or the word is not one.</summary>
        public Primitive Holds
        {
            get
            {
                switch (Sort)
                {
                    case "boolean":
                        return Primitive.Boolean;
                    case "byte":
                        return Primitive.Byte;
                    case "sbyte":
                        return Primitive.SByte;
                    case "int16":
                        return Primitive.Int16;
                    case "uint16":
                        return Primitive.UInt16;
                    case "int32":
                        return Primitive.Int32;
                    case "uint32":
                        return Primitive.UInt32;
                    case "int64":
                        return Primitive.Int64;
                    case "uint64":
                        return Primitive.UInt64;
                    case "single":
                        return Primitive.Single;
                    case "double":
                        return Primitive.Double;
                    case "char":
                        return Primitive.Char;
                    case "string":
                        return Primitive.String;
                }

                return Primitive.None;
            }
        }

        /// <summary>How many of the action's parameters this covers, which is always one.</summary>
        public override int Span => 1;

        /// <summary>The type the action takes it as, as it is given.</summary>
        public override string Kind => Sort ?? "";
    }
}
