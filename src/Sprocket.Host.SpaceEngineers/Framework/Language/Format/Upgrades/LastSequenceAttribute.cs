using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System.Collections.Generic;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>Alpha 17: a system's <c>last sequence &lt;n&gt;</c> left its header and became the attribute <c>#@ lastSequence: &lt;n&gt;</c>.</summary>
    public sealed class LastSequenceAttribute : FormatChange
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

                var at = system.Find(Keyword.Last);
                var number = system.ValueAt(at + 2);
                if (at < 0 || !system.Says(at + 1, Keyword.Sequence) || number.Form != ValueForm.Integer)
                    continue;

                var values = new List<Value>();
                for (var v = 0; v < system.Count; v++)
                {
                    if (v < at || v > at + 2)
                        values.Add(system.ValueAt(v));
                }

                var attributes = new List<ScriptAttribute>(system.Attributes);
                var written = number.Whole.ToString(CultureInfo.InvariantCulture);
                attributes.Add(new ScriptAttribute(ScriptAttributes.LastSequence, number, written));
                constructs[i] = system.With(values, system.Lines, attributes);
            }
        }
    }
}
