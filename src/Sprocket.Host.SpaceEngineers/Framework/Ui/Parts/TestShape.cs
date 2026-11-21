using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>
    /// How a line testing blocks is said: where the words saying how it reads them stand, what they are, how the blocks are
    /// joined and whether what follows is said for several.
    /// </summary>
    public sealed class TestShape
    {
        readonly string _join;

        TestShape(string reading, bool leads, bool joinsAll, bool isPlural, bool namesValue, string join)
        {
            _join = join ?? SentenceText.OnWord;
            Reading = reading;
            Leads = leads;
            JoinsAll = joinsAll;
            IsPlural = isPlural;
            NamesValue = namesValue;
        }

        /// <summary>The words saying how the line reads its blocks, or empty where it reads one block and says nothing of it.</summary>
        public string Reading { get; private set; }

        /// <summary>Whether the line says how it reads its blocks.</summary>
        public bool IsRead => Reading.Length != 0;

        /// <summary>Whether those words open the line, before the value it names; otherwise they stand just before the blocks.</summary>
        public bool Leads { get; private set; }

        /// <summary>Whether the blocks are joined with "and" rather than "or".</summary>
        public bool JoinsAll { get; private set; }

        /// <summary>Whether the comparison after the blocks is said for several of them: "are open".</summary>
        public bool IsPlural { get; private set; }

        /// <summary>Whether the line names a value on its blocks, rather than having the blocks as its subject.</summary>
        public bool NamesValue { get; private set; }

        /// <summary>
        /// The shape of a line reading its blocks by <paramref name="quantifier" />, naming a value on them where
        /// <paramref name="namesValue" />, reading several where <paramref name="many" />, and naming slots where
        /// <paramref name="slotted" />, the blocks joined by <paramref name="join" />, or the usual word where that is null.
        /// </summary>
        public static TestShape Of(Quantifier quantifier, bool namesValue, bool many, bool slotted, string join = null)
        {
            var totals = SentenceText.Totals(quantifier);
            var reading = many ? Words(quantifier, namesValue, slotted) : "";

            if (namesValue)
                return new TestShape(reading, many && totals, true, false, true, join);

            return new TestShape(reading, many && (totals || !slotted), quantifier != Quantifier.Any,
                quantifier == Quantifier.All && many, false, join);
        }

        /// <summary>
        /// Puts the parts of the line in <paramref name="into" /> in the order they are said: <paramref name="reading" />, which
        /// is null where the line does not say how it reads, <paramref name="value" />, which is null where the blocks are the
        /// subject, the arguments <paramref name="taking" /> the value is read with straight after it, null where it takes none,
        /// and <paramref name="blocks" />.
        /// </summary>
        /// <remarks>Each part is given the word that joins it to the one before, which a row draws between them.</remarks>
        public void Lay(List<SentenceSlot> into, SentenceSlot reading, SentenceSlot value, SentenceSlot blocks, SentenceSlot taking = null)
        {
            var named = value != null;
            var parts = new[] { reading, value, blocks };
            foreach (var part in Order(reading != null, named))
            {
                var slot = parts[(int)part];
                slot.Joined = Before(part, named);
                into.Add(slot);
                if (part != LinePart.Value || taking == null)
                    continue;

                taking.Joined = "";
                into.Add(taking);
            }
        }

        /// <summary>
        /// The line as words, up to what it compares with: <paramref name="value" /> read with <paramref name="taking" />, null
        /// where it takes nothing, on <paramref name="blocks" />, or the blocks alone where <paramref name="value" /> is null; the
        /// same words, in the same order, as <see cref="Lay" /> gives a row.
        /// </summary>
        public string Says(string value, string blocks, string taking = null)
        {
            var named = value != null;
            var said = new List<string>();
            foreach (var part in Order(IsRead, named))
            {
                var joined = Before(part, named);
                if (joined.Length != 0)
                    said.Add(joined);

                said.Add(part == LinePart.Reading ? Reading : part == LinePart.Value ? value : blocks);
                if (part == LinePart.Value && taking != null)
                    said.Add(taking);
            }

            return string.Join(" ", said);
        }

        /// <summary>The parts of the line in the order they are said, with the reading where <paramref name="read" /> and the value where <paramref name="named" />.</summary>
        List<LinePart> Order(bool read, bool named)
        {
            var order = new List<LinePart>(3);
            if (read && Leads)
                order.Add(LinePart.Reading);

            if (named)
                order.Add(LinePart.Value);

            if (read && !Leads)
                order.Add(LinePart.Reading);

            order.Add(LinePart.Blocks);
            return order;
        }

        /// <summary>The word that joins <paramref name="part" /> to what comes before it in a line that names a value where <paramref name="named" />.</summary>
        string Before(LinePart part, bool named)
        {
            if (!named)
                return "";

            if (part == LinePart.Value)
                return Leads ? "" : SentenceText.TheWord;

            if (part == LinePart.Reading)
                return Leads ? "" : _join;

            return Leads || !IsRead ? _join : "";
        }

        /// <summary>The parts of a line testing blocks, up to its comparison, each numbered by its place among Lay's arguments.</summary>
        enum LinePart
        {
            Reading,
            Value,
            Blocks
        }

        /// <summary>The words saying how a line reads several blocks by <paramref name="quantifier" />.</summary>
        static string Words(Quantifier quantifier, bool namesValue, bool slotted)
        {
            if (quantifier == Quantifier.Sum)
                return "the sum of the";

            if (quantifier == Quantifier.Average)
                return "the average of the";

            if (quantifier == Quantifier.Min)
                return "the lowest of the";

            if (quantifier == Quantifier.Max)
                return "the highest of the";

            // A value read on every block is said for each of them, since "all" would want the value in the plural.
            var word = quantifier == Quantifier.All ? namesValue ? "each" : "all" : "any";
            if (slotted)
                return $"{word} in";

            return namesValue ? $"{word} of" : word;
        }
    }
}
