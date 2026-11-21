using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A trigger started each time a signal is sent: <c>whenever button then run 1</c>, or a signal written in place,
    /// <c>whenever sprocket.finishes_closing on [ inner ] then run 2</c>.
    /// </summary>
    public sealed class SignalTrigger : MomentTrigger
    {
        /// <summary>A trigger started by a signal, named by no slot or variable yet.</summary>
        public SignalTrigger() : base("")
        {
        }

        /// <summary>A trigger started by the signal <paramref name="eventId" />.</summary>
        public SignalTrigger(string eventId) : base(eventId)
        {
        }

        /// <summary>The namespace the signal written in place is qualified by, or empty where the trigger names its signal.</summary>
        /// <remarks>Setting it keeps the id; the signal is still named by <see cref="MomentTrigger.Key" />.</remarks>
        public string Provider
        {
            get { return Key.Namespace; }
            set { Key = new MemberKey(value, Key.Id); }
        }

        /// <summary>The slot on its blocks' toolbars a signal written in place is sent from, or null for a block's own change.</summary>
        public ToolbarSlot At { get; set; }

        /// <summary>Whether the signal is written in place, on the blocks it is sent from, rather than named.</summary>
        public bool IsWritten => Provider.Length > 0;

        /// <summary>Whether this is a block's own change, which Sprocket watches for itself, rather than a toolbar slot.</summary>
        public bool IsWatched => IsWritten && At == null;

        /// <summary>The signal part or variable this names, or empty where it names none or is written in place.</summary>
        public string Part => !IsWritten && Sources.Count > 0 ? Sources[0] : "";

        /// <summary>The trigger that is the signal part <paramref name="part" />, answered in each instance by its toolbar slots.</summary>
        public static SignalTrigger Named(string part)
        {
            var trigger = new SignalTrigger();
            trigger.Sources.Add(part);
            return trigger;
        }

        /// <summary>
        /// The trigger that is the signal <paramref name="eventId" /> qualified by <paramref name="provider" />, written in
        /// place and sent from any of the blocks added to its sources.
        /// </summary>
        public static SignalTrigger Written(string provider, string eventId)
        {
            var trigger = new SignalTrigger(eventId);
            trigger.Provider = provider;
            return trigger;
        }
    }
}
