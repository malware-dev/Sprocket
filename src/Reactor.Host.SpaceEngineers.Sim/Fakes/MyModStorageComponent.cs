using System;
using System.Collections.Generic;
using VRage.Game.Components;

namespace Sandbox.Game.EntityComponents
{
    /// <summary>
    /// Mod storage on a block, as the harness keeps it.
    /// </summary>
    /// <remarks>
    /// The game's own is in Sandbox.Game, which the harness does not reference - see
    /// build/Reactor.GameReferences.props. The abstract it derives from is the game's, so what the
    /// mod holds is the real type and only the keeping of it is the harness's.
    /// <para>
    /// In memory and nothing else. Keeping a construct's program between runs is a file, and that is
    /// <see cref="Reactor.Sim.Machine.Store" />, which is this with somewhere to write.
    /// </para>
    /// </remarks>
    public class MyModStorageComponent : MyModStorageComponentBase
    {
        readonly Dictionary<Guid, string> _held = new Dictionary<Guid, string>();

        protected IEnumerable<KeyValuePair<Guid, string>> Held => _held;

        public override string GetValue(Guid key)
        {
            string held;
            return _held.TryGetValue(key, out held) ? held : null;
        }

        public override void SetValue(Guid key, string value)
        {
            _held[key] = value;
            Wrote(key, value);
        }

        public override bool RemoveValue(Guid key)
        {
            return _held.Remove(key);
        }

        public override bool TryGetValue(Guid key, out string value)
        {
            return _held.TryGetValue(key, out value);
        }

        /// <summary>Told after a value lands, for anything that keeps it somewhere.</summary>
        protected virtual void Wrote(Guid key, string value)
        {
        }

        protected void Put(Guid key, string value)
        {
            _held[key] = value;
        }
    }
}
