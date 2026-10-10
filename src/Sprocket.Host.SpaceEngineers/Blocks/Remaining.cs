using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block's resource source being listened to as it runs out of what it gives and as it gets some back, until disposed.</summary>
    /// <remarks>
    /// The game raises it from the source as the store reaches nothing and as it rises from nothing: a tank's gas, a
    /// generator's ice, an engine's fuel. Nothing between, so a store that is neither empty nor refilling costs nothing.
    /// </remarks>
    public sealed class Remaining : IDisposable
    {
        readonly MyResourceSourceComponent _source;
        readonly Action _told;

        Remaining(MyResourceSourceComponent source, Action told)
        {
            _source = source;
            _told = told;
            _source.HasCapacityRemainingChanged += Changed;
        }

        /// <summary>Tells <paramref name="told" /> each time <paramref name="block" />'s store runs out or comes back; null where it has no resource source.</summary>
        public static IDisposable Heard(IMyTerminalBlock block, Action told)
        {
            var source = block.Components.Get<MyResourceSourceComponent>();
            return source == null ? null : new Remaining(source, told);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _source.HasCapacityRemainingChanged -= Changed;
        }

        void Changed(MyDefinitionId resource, MyResourceSourceComponent source)
        {
            _told();
        }
    }
}
