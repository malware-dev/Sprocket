using System.Collections.Generic;
using System.Text;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>The tooltips of the parts of a sentence: what a part holds, which its chip shortens, above what pressing it does.</summary>
    public static class PartTips
    {
        /// <summary>
        /// The tooltip of <paramref name="part" />: what it holds where its chip shortens it, a part holding blocks said as
        /// <paramref name="held" />; and <paramref name="press" />, what pressing it does or why it cannot be pressed, or nothing
        /// for null.
        /// </summary>
        public static string Of(Texts texts, GridWorld world, SentenceSlot part, string held, string press)
        {
            var blocks = part as EntitiesSlot;
            if (blocks != null)
                return Blocks(texts, world, blocks, held, press);

            var length = part as DurationSlot;
            return length != null ? Length(texts, length, press) : press;
        }

        /// <summary>
        /// The tooltip of <paramref name="part" />. Holding blocks, it says the slots they come from, then <paramref name="held" />
        /// over each block on a line of its own as <paramref name="world" /> calls it, with why it cannot be pressed last where it
        /// cannot; naming slots the installation shown leaves empty, it says the slots and that they are empty, then
        /// <paramref name="press" />, what pressing it does or why it cannot be pressed; holding nothing else, it says only that.
        /// </summary>
        public static string Blocks(Texts texts, GridWorld world, EntitiesSlot part, string held, string press)
        {
            var said = new StringBuilder();
            if (part.Entities.Count == 0 && !part.SaysSlots)
            {
                Paragraph(said, press);
                return said.ToString();
            }

            if (part.Slots.Count > 0)
            {
                var slots = Markdown.Escape(string.Join(", ", part.Slots));
                Paragraph(said, Template.Fill(part.Slots.Count == 1 ? texts.Codex.PartFromSlot : texts.Codex.PartFromSlots, "slots", "**" + slots + "**"));
            }

            // A slot the installation shown leaves empty is still named, so the line says which slot is waiting.
            if (part.Entities.Count == 0)
            {
                Paragraph(said, texts.Codex.PartSlotEmpty);
                Paragraph(said, press);
                return said.ToString();
            }

            Paragraph(said, held);
            var named = new List<Mention>();
            HeldBlocks.List(texts, world, part.Entities, part.RequiresAll, named);
            foreach (var block in named)
                said.Append("- ").Append(Markdown.Escape(block.Text)).Append('\n');

            if (part.Opens == null)
                Paragraph(said, press);

            return said.ToString();
        }

        /// <summary>The tooltip of <paramref name="part" />: its length in the unit it is not written in, and <paramref name="press" />, what pressing it does, or nothing for null.</summary>
        public static string Length(Texts texts, DurationSlot part, string press)
        {
            var said = new StringBuilder();
            Paragraph(said, Markdown.Escape(TimeText.Other(texts, part.Length)));
            Paragraph(said, press);
            return said.ToString();
        }

        /// <summary>Adds <paramref name="words" /> to <paramref name="said" /> as a paragraph of its own, where there are any.</summary>
        static void Paragraph(StringBuilder said, string words)
        {
            if (string.IsNullOrEmpty(words))
                return;

            if (said.Length > 0)
                said.Append('\n');

            said.Append(words).Append('\n');
        }
    }
}
