using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Storage.Upgrades
{
    /// <summary>Alpha 17: each clause of a system's header, <c>changed</c> and <c>from blueprint</c>, went onto a line of its own.</summary>
    /// <remarks>After the other changes of alpha 17, so the clauses it places are the ones they left.</remarks>
    public sealed class HeaderLines : FormatChange
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

                // Past the identity, 'as' and the name, where the clauses start.
                var first = system.Says(0, Keyword.Blueprint) ? 4 : 3;
                var lines = new List<int>();
                var values = new List<Value>();
                for (var v = 0; v < system.Count; v++)
                {
                    if (v >= first && (system.Says(v, Keyword.Changed) || system.Says(v, Keyword.From)))
                        lines.Add(v);

                    values.Add(system.ValueAt(v));
                }

                if (lines.Count > 0)
                    constructs[i] = system.With(values, lines, system.Attributes);
            }
        }
    }
}
