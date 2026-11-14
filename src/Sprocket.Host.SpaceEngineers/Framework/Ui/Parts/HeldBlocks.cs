using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Collections.Generic;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>How the blocks in a slot are listed one under another, wherever they are: a part's tooltip, a slot's row on the installation panel, a slot chosen in the picker.</summary>
    public static class HeldBlocks
    {
        /// <summary>How many lines the blocks take at most, the last saying how many more there are where they do not fit.</summary>
        public const int Most = 8;

        /// <summary>
        /// Adds to <paramref name="into" /> each of <paramref name="blocks" />, by entity id, as <paramref name="world" /> calls it
        /// with its icon, and where there are more than <see cref="Most" />, a last piece saying how many more, in
        /// <paramref name="texts" />: every one taking part where <paramref name="all" />, and any one of them otherwise.
        /// </summary>
        public static void List(Texts texts, GridWorld world, IList<long> blocks, bool all, List<Mention> into)
        {
            var more = SentenceText.Listed(world, blocks, Most, into);
            if (more > 0)
                into.Add(new Mention(Template.Fill(all ? texts.SlotAndMore : texts.SlotOrMore, "count", more.ToString(CultureInfo.InvariantCulture)), null));
        }
    }
}
