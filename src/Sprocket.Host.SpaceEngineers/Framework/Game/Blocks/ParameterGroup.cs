using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// Several of an action's parameters that are one thing to a person.
    /// </summary>
    public sealed class ParameterGroup : ParameterPiece
    {
        /// <summary>What the parameters make up together, such as <c>color</c>.</summary>
        public string Sort;

        /// <summary>The parameters the group covers, in order.</summary>
        public List<Parameter> Parameters = new List<Parameter>();

        /// <inheritdoc />
        public override int Span => Parameters.Count;

        /// <inheritdoc />
        public override string Kind => Sort ?? "";
    }
}
