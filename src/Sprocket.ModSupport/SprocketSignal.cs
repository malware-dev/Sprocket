using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>A signal a mod raises on its blocks, which players start their automation on, such as "gains grip".</summary>
    public sealed class SprocketSignal : ISprocketWired
    {
        readonly ISprocketRegistry _registration;
        readonly SprocketMember _member;

        /// <summary>The signal <paramref name="member" /> describes, raised through <paramref name="registration" />, which only the wrapper holds.</summary>
        public SprocketSignal(ISprocketRegistry registration, SprocketMember member)
        {
            _registration = registration;
            _member = member;
        }

        /// <summary>The signal's id in the mod's namespace.</summary>
        public string Id => _member.Id;

        /// <summary>Names the signal <paramref name="name" /> in Sprocket's sentences and describes it with <paramref name="description" />.</summary>
        public SprocketSignal Named(string name, string description)
        {
            _member.Named(name, description);
            return this;
        }

        /// <summary>Names and describes the signal by the texts <paramref name="name" /> and <paramref name="description" /> name in the mod's own texts.</summary>
        public SprocketSignal Named(MyStringId name, MyStringId description)
        {
            _member.Named(name, description);
            return this;
        }

        /// <summary>Stands the signal in a group of the mod's own, headed <paramref name="heading" />.</summary>
        public SprocketSignal InGroup(string heading)
        {
            _member.InGroup(heading);
            return this;
        }

        /// <summary>Stands the signal in <paramref name="group" />, one of Sprocket's own.</summary>
        public SprocketSignal InGroup(SprocketGroup group)
        {
            _member.InGroup(group);
            return this;
        }

        /// <summary>Raises the signal on <paramref name="block" />, starting whatever listens for it there.</summary>
        /// <remarks>Costs next to nothing where nothing listens; called on the game's main thread.</remarks>
        public void Raise(IMyTerminalBlock block)
        {
            _registration.Raise(block, Id);
        }

        /// <inheritdoc />
        Dictionary<string, object> ISprocketWired.Wire() => ((ISprocketWired)_member).Wire();
    }
}
