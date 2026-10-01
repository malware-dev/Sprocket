using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The tag under a member that cannot be made into a step, and why it cannot.</summary>
    /// <remarks>
    /// Two or three words, because it sits under the name in a list somebody is scanning. The whole
    /// reason is the member's own, and shows when it is chosen.
    /// </remarks>
    public static class ReadinessText
    {
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
        public static string Why(Texts texts, Holdback why)
        {
            switch (why)
            {
                case Holdback.Events: return texts.WithheldEvents;
                case Holdback.Camera: return texts.WithheldCamera;
                case Holdback.Rotor: return texts.WithheldRotor;
                case Holdback.Ammo: return texts.WithheldAmmo;
                case Holdback.Unread: return texts.WithheldUnread;
                default: return "";
            }
        }
    }
}
