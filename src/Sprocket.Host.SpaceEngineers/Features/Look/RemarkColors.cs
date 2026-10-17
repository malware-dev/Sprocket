using Sprocket.Host.SpaceEngineers.Features.Shell;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>What a remark is written in, by how much it matters.</summary>
    public static class RemarkColors
    {
        /// <summary>The ink of a remark at <paramref name="level" />: red for a problem, amber for a warning, and <paramref name="plain" />, the words around it, for a note.</summary>
        public static Color Of(NoticeLevel level, Color plain)
        {
            if (level == NoticeLevel.Problem)
                return Palette.Problem;

            return level == NoticeLevel.Warning ? Palette.Notice : plain;
        }

        /// <summary>The ground of a line carrying a remark at <paramref name="level" />: a wash for a problem, none for a warning or a note.</summary>
        public static Color Wash(NoticeLevel level)
        {
            return level == NoticeLevel.Problem ? Palette.ProblemWash : Palette.None;
        }
    }
}
