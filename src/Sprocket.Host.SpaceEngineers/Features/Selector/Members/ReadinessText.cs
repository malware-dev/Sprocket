using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Members
{
    /// <summary>The tag under a member that cannot be made into a step, and why it cannot.</summary>
    public static class ReadinessText
    {
        /// <summary>The short tag a picker row carries for <paramref name="readiness" />, in <paramref name="texts" />, or empty where the member is ready.</summary>
        public static string Tag(Texts texts, Readiness readiness)
        {
            switch (readiness)
            {
                case Readiness.Pending: return texts.PickTagPending;
                case Readiness.Unsupported: return texts.PickTagUnsupported;
                default: return "";
            }
        }

        /// <summary>Why a member is held back, in <paramref name="texts" />, or empty where nothing holds it.</summary>
        public static string Why(Texts texts, UnreadyReason why)
        {
            switch (why)
            {
                case UnreadyReason.Events: return texts.Codex.WithheldEvents;
                case UnreadyReason.Camera: return texts.Codex.WithheldCamera;
                case UnreadyReason.Rotor: return texts.Codex.WithheldRotor;
                case UnreadyReason.Ammo: return texts.Codex.WithheldAmmo;
                case UnreadyReason.Unread: return texts.Codex.WithheldUnread;
                default: return "";
            }
        }
    }
}
