using Lumel;
using Lumel.Drawing;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using VRageMath;
using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>How a remark looks, by how much it matters: a problem and a warning each one way wherever they are said, and a note as the words around it.</summary>
    public static class RemarkLook
    {
        /// <summary>The ink of a remark at <paramref name="level" />: red for a problem, amber for a warning, and <paramref name="plain" />, the words around it, for a note.</summary>
        public static Color Of(NoticeLevel level, Color plain)
        {
            if (level == NoticeLevel.Problem)
                return Palette.Problem;

            return level == NoticeLevel.Warning ? Palette.Notice : plain;
        }

        /// <summary>The wash under what carries a remark at <paramref name="level" />: red for a problem, amber for a warning, and <paramref name="plain" />, its own ground, for a note.</summary>
        public static Color Wash(NoticeLevel level, Color plain)
        {
            if (level == NoticeLevel.Problem)
                return Palette.ProblemWash;

            return level == NoticeLevel.Warning ? Palette.NoticeWash : plain;
        }

        /// <summary>The hazard stripes over that wash at <paramref name="level" />, or null for a note.</summary>
        public static Pattern Stripes(NoticeLevel level)
        {
            if (level == NoticeLevel.Problem)
                return Styles.ProblemStripes;

            return level == NoticeLevel.Warning ? Styles.NoticeStripes : null;
        }

        /// <summary>Lays the ground of a remark at <paramref name="level" /> under <paramref name="element" />, which carries it, or <paramref name="plain" /> for a note.</summary>
        public static void Ground(Element element, NoticeLevel level, Color plain)
        {
            element.Base.Background = Wash(level, plain);
            element.Base.Pattern = Stripes(level);
        }

        /// <summary>The style of a remark at <paramref name="level" /> on a line of its own, on its ground, or <paramref name="plain" /> for a note.</summary>
        public static IStyle<Label> Line(NoticeLevel level, IStyle<Label> plain)
        {
            if (level == NoticeLevel.Problem)
                return Styles.ProblemLine;

            return level == NoticeLevel.Warning ? Styles.WarningLine : plain;
        }
    }
}
