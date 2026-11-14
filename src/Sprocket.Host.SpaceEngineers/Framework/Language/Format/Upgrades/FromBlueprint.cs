using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>Alpha 17: a system blueprint's <c>from "&lt;guid&gt;"</c> was written <c>from blueprint "&lt;guid&gt;"</c>, saying what the identity names.</summary>
    public sealed class FromBlueprint : FormatChange
    {
        /// <inheritdoc />
        public override int Arrived => 17;

        /// <inheritdoc />
        public override void Statements(List<Statement> constructs)
        {
            for (var i = 0; i < constructs.Count; i++)
            {
                var system = constructs[i];
                if (system.Key != Keyword.System)
                    continue;

                var at = system.Find(Keyword.From);
                if (at < 0 || system.Says(at + 1, Keyword.Blueprint))
                    continue;

                var values = new List<Value>();
                for (var v = 0; v < system.Count; v++)
                {
                    values.Add(system.ValueAt(v));
                    if (v == at)
                        values.Add(new Value(ValueForm.Keyword, null, 0, 0, Keyword.Blueprint));
                }

                constructs[i] = system.With(values, system.Lines, system.Attributes);
            }
        }
    }
}
