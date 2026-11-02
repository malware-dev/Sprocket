using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// How a line testing blocks is said: where the words saying how it reads them stand, what they are, how the blocks are
    /// joined and whether what follows is said for several.
    /// </summary>
    public sealed class TestShape
    {
        TestShape(string reading, bool leads, bool joinsAll, bool isPlural, bool namesValue)
        {
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
        /// <paramref name="slotted" />.
        /// </summary>
        public static TestShape Of(Quantifier quantifier, bool namesValue, bool many, bool slotted)
        {
            var totals = SentenceText.Totals(quantifier);
            var reading = many ? Words(quantifier, namesValue, slotted) : "";

            if (namesValue)
                return new TestShape(reading, many && totals, true, false, true);

            return new TestShape(reading, many && (totals || !slotted), quantifier != Quantifier.Any,
                quantifier == Quantifier.All && many, false);
        }

        /// <summary>
        /// Puts the parts of the line in <paramref name="into" /> in the order they are said: <paramref name="reading" />, which
        /// is null where the line does not say how it reads, <paramref name="value" />, which is null where the blocks are the
        /// subject, and <paramref name="blocks" />.
        /// </summary>
        public void Lay(List<SentenceSlot> into, SentenceSlot reading, SentenceSlot value, SentenceSlot blocks)
        {
            if (reading != null && Leads)
                into.Add(reading);

            if (value != null)
                into.Add(value);

            if (reading != null && !Leads)
                into.Add(reading);

            into.Add(blocks);
        }

        /// <summary>
        /// The line as words, up to what it compares with: <paramref name="value" /> on <paramref name="blocks" />, or the blocks
        /// alone where <paramref name="value" /> is null.
        /// </summary>
        public string Says(string value, string blocks)
        {
            var read = IsRead ? $"{Reading} {blocks}" : blocks;
            if (value == null)
                return read;

            return Leads ? $"{Reading} {value} on {blocks}" : $"{SentenceText.TheWord} {value} on {read}";
        }

        /// <summary>The words saying how a line reads several blocks by <paramref name="quantifier" />.</summary>
        static string Words(Quantifier quantifier, bool namesValue, bool slotted)
        {
            if (quantifier == Quantifier.Sum)
                return "the sum of the";

            if (quantifier == Quantifier.Average)
                return "the average of the";

            // A value read on every block is said for each of them, since "all" would want the value in the plural.
            var word = quantifier == Quantifier.All ? namesValue ? "each" : "all" : "any";
            if (slotted)
                return $"{word} in";

            return namesValue ? $"{word} of" : word;
        }
    }
}
