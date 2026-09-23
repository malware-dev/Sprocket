using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Words
{
    /// <summary>The tag under a member that cannot be made into a step.</summary>
    /// <remarks>
    /// Two or three words, because it sits under the name in a list somebody is scanning. The whole
    /// reason is the member's own, and shows when it is chosen.
    /// </remarks>
    public static class ReadinessText
    {
        public static string Tag(Readiness readiness)
        {
            switch (readiness)
            {
                case Readiness.Pending: return "pending";
                case Readiness.Unsupported: return "not supported";
                default: return "";
            }
        }
    }
}
