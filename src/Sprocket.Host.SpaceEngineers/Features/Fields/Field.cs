using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>
    /// What one field of a form asks for: what it is called, and what it will take.
    /// </summary>
    /// <remarks>A struct, since one is made per value per question on every tick the control raising the question is dressed.</remarks>
    public struct Field
    {
        /// <summary>How many places after the point a decimal field gives room for.</summary>
        /// <remarks>Enough for a quarter of a meter on a piston, which is as fine as the game's own blocks go.</remarks>
        public const int Places = 2;

        /// <summary>What it is called, or empty to leave it numbered.</summary>
        public string Name;

        /// <summary>The name of the value of the same form whose word decides whether this one is asked for, or null where it always is.</summary>
        public string AskedWith;

        /// <summary>The words of <see cref="AskedWith" /> this one is asked for under.</summary>
        public IList<string> AskedFor;

        /// <summary>The least it may be, or empty where nobody has said.</summary>
        public string Least;

        /// <summary>The most it may be, or empty where nobody has said.</summary>
        public string Most;

        /// <summary>
        /// The only words it will take, or null where anything goes.
        /// </summary>
        public IList<string> Words;

        /// <summary>
        /// What this is filled in with, where a box will not do.
        /// </summary>
        public FieldEditor Edits;

        /// <summary>Whether the value has to be given: a field that stands for nothing when it is empty.</summary>
        public bool Needed;

        /// <summary>Whether <see cref="Least" /> is a bound the value must be above, rather than one it may be.</summary>
        public bool Above;

        /// <summary>What the field starts with where nothing has been given yet, as it is shown, or null for nothing.</summary>
        public string Default;

        /// <summary>What a sentence says before this value, or null for nothing: "to" in "to 90°".</summary>
        public string Lead;

        /// <summary>What a sentence says straight after this value, or null for nothing: the "°" in "90°", and what stands after its box.</summary>
        public string Unit;

        /// <summary>
        /// How many of what is shown make one of what is stored: 100 for a value the game holds as 0 to 1 and a person reads
        /// as a percent, and 0 or 1 where the two are the same.
        /// </summary>
        /// <remarks>The script keeps the game's own value, and bounds are stored as the game has them too.</remarks>
        public double Scale;

        /// <summary>
        /// What a shown value moves in, or 0 for any amount: 90 for a rotation the game snaps to quarter turns, so the slider
        /// stops at each and a number typed between them goes to the nearest.
        /// </summary>
        public double Step;

        /// <summary>
        /// The word a sentence leaves out, or null where it says every one.
        /// </summary>
        public string Quiet;

        /// <summary>What the field is about, said where it is filled in, or null for nothing more than its name.</summary>
        public string About;

        /// <summary>What may be typed into the box.</summary>
        public FieldInput Entry;

        /// <summary>How many characters text written over several lines may run to, or zero for no limit: what the block itself holds.</summary>
        public int Longest;

        /// <summary>
        /// How many of the values behind the form this one field stands for.
        /// </summary>
        /// <remarks>Read through <see cref="Covers" />: a field nobody set holds zero here.</remarks>
        public int Spans;

        /// <summary>How many values this field stands for, which is one unless it was told otherwise.</summary>
        public int Covers => Spans < 1 ? 1 : Spans;

        /// <summary>Whether it is a choice between on and off and nothing else.</summary>
        public bool IsOnOff =>
            Edits == FieldEditor.Chosen && Words != null && Words.Count == 2
            && Words.Contains(Literal.Of(true).Said) && Words.Contains(Literal.Of(false).Said);

        /// <summary>One field standing over several values, filled in by a control of its own.</summary>
        public static Field Holding(string name, FieldEditor edits, int spans)
        {
            Field field = Called(name);
            field.Edits = edits;
            field.Spans = spans;
            return field;
        }

        /// <summary>
        /// What the program holds for each of <see cref="Words" />, or null where they are the same.
        /// </summary>
        public IList<string> Means;

        /// <summary>What the program holds for a word somebody chose.</summary>
        public string Meaning(string word)
        {
            return Found(Words, word, Means);
        }

        /// <summary>The word for <paramref name="said" />, what the program holds.</summary>
        public string Wording(string said)
        {
            return Found(Means, said, Words);
        }

        /// <summary>
        /// Whether <paramref name="given" />, already the kind this field holds, is one of its words where it lists some, and
        /// within its bounds where it has them and <paramref name="bounded" /> asks for them.
        /// </summary>
        /// <remarks>
        /// An empty list of words is one that could not be read, as where the game's definitions are not loaded, and admits
        /// anything.
        /// </remarks>
        public bool Admits(Literal given, bool bounded)
        {
            if (Unfilled(given))
                return true;

            var words = Means ?? Words;
            if (words != null && words.Count > 0 && !words.Contains(given.Said))
                return false;

            var number = given.Kind == ValueKind.Integer || given.Kind == ValueKind.Real;
            if (!bounded || !number)
                return true;

            double bound;
            if (Bound(Least, out bound) && (Above ? given.Number <= bound : given.Number < bound))
                return false;

            return !(Bound(Most, out bound) && given.Number > bound);
        }

        /// <summary>Whether <paramref name="given" /> is this field left empty, which is a value not given where none is needed.</summary>
        public bool Unfilled(Literal given)
        {
            return !Needed && given.Kind == ValueKind.Text && string.IsNullOrEmpty(given.Text);
        }

        static bool Bound(string written, out double bound)
        {
            bound = 0;
            return !string.IsNullOrEmpty(written)
                   && double.TryParse(written, NumberStyles.Float, CultureInfo.InvariantCulture, out bound);
        }

        /// <summary>
        /// What <paramref name="said" /> stands for, by its place in one list and out of the other.
        /// </summary>
        static string Found(IList<string> among, string said, IList<string> into)
        {
            if (among == null || into == null || among.Count != into.Count)
                return said;

            for (var i = 0; i < among.Count; i++)
            {
                if (among[i] == said)
                    return into[i];
            }

            return said;
        }

        /// <summary>A field that takes only one of a listed few, each standing for its own value.</summary>
        public static Field Among(string name, IList<string> words, IList<string> means)
        {
            Field field = Called(name);
            field.Edits = FieldEditor.Chosen;
            field.Words = words;
            field.Means = means;
            return field;
        }

        /// <summary>The stored number <paramref name="stored" /> on the scale it is shown at, or what it says as it is where it is no number.</summary>
        public string Scaled(string stored)
        {
            double value;
            if (Scale == 0 || Scale == 1 || !double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return stored;

            return (value * Scale).ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The number <paramref name="shown" /> on the scale it is stored at, moved to the nearest step where it moves in steps,
        /// or what it says as it is where it is no number.
        /// </summary>
        public string Unscaled(string shown)
        {
            double value;
            if (Scale == 0 && Step == 0 || Scale == 1 && Step == 0
                || !double.TryParse(shown, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return shown;

            if (Step > 0)
                value = System.Math.Round(value / Step) * Step;

            return (value / (Scale == 0 ? 1 : Scale)).ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary>A field nobody has described, which is a box that takes anything.</summary>
        public static Field Unnamed()
        {
            return Called("");
        }

        /// <summary>A box that takes anything, named <paramref name="name" />.</summary>
        public static Field Called(string name)
        {
            var field = new Field();
            field.Name = name ?? "";
            field.Least = "";
            field.Most = "";
            return field;
        }

        /// <summary>A field that takes only a value between <paramref name="least" /> and <paramref name="most" />, either of which may be empty.</summary>
        public static Field Between(string name, string least, string most)
        {
            Field field = Called(name);
            field.Least = least ?? "";
            field.Most = most ?? "";
            return field;
        }

        /// <summary>A field that takes only one of <paramref name="words" />.</summary>
        public static Field OneOf(string name, params string[] words)
        {
            Field field = Called(name);
            field.Words = words;
            return field;
        }

        /// <summary>
        /// What goes beside the box: what it is called, and what it will take.
        /// </summary>
        public string Label(Texts texts)
        {
            string name = Name ?? "";
            string takes = Takes(texts);

            if (takes.Length == 0)
                return name;

            return name.Length == 0 ? takes : $"{name} {takes}";
        }

        /// <summary>
        /// How the bounds read, or empty where there are none.
        /// </summary>
        public string Takes(Texts texts)
        {
            if (Words != null && Words.Count > 0)
                return Listed(texts);

            string least = Least ?? "";
            string most = Most ?? "";

            if (least.Length > 0 && most.Length > 0)
                return $"({least} - {most})";

            if (least.Length > 0)
                return Template.Fill(texts.ValueAtLeast, "least", least);

            return most.Length > 0 ? Template.Fill(texts.ValueAtMost, "most", most) : "";
        }

        /// <summary>The words it takes, said the way somebody would say them aloud.</summary>
        string Listed(Texts texts)
        {
            var rest = "";
            for (var i = 0; i < Words.Count - 1; i++)
                rest += i == 0 ? Words[i] : $", {Words[i]}";

            var last = Words[Words.Count - 1];
            return rest.Length == 0 ? last : Template.Fill(Template.Fill(texts.ValueEither, "last", last), "rest", rest);
        }

        /// <summary>
        /// Why <paramref name="said" /> cannot be sent, or empty where it can.
        /// </summary>
        /// <remarks><paramref name="called" /> is what the field is called on screen, its place in the order for a value nobody named.</remarks>
        public string Refuses(Texts texts, string called, string said)
        {
            if (string.IsNullOrEmpty(said))
                return Needed ? Template.Fill(texts.ValueNeeded, "name", SentenceText.Capitalised(called)) : "";

            if (Words != null && Words.Count > 0)
                return Knows(said) ? "" : Template.Fill(Template.Fill(texts.ValueOneOf, "words", Listed(texts)), "name", called);

            double low, high;
            bool hasLow = Reads(Least, out low);
            bool hasHigh = Reads(Most, out high);
            bool numeric = Entry == FieldInput.Whole || Entry == FieldInput.Decimal;

            if (!hasLow && !hasHigh && !numeric)
                return "";

            double value;
            if (!Reads(said, out value))
                return Template.Fill(texts.ValueNumber, "name", called);

            long whole;
            if (Entry == FieldInput.Whole
                && !long.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
            {
                return Template.Fill(texts.ValueWhole, "name", called);
            }

            if (!hasLow && !hasHigh)
                return "";

            if ((hasLow && (Above ? value <= low : value < low)) || (hasHigh && value > high))
                return Template.Fill(Template.Fill(texts.ValueWithin, "range", Within(texts, hasLow, hasHigh)), "name", called);

            return "";
        }

        /// <summary>The bounds said as part of a sentence, rather than as the label's shorthand.</summary>
        string Within(Texts texts, bool hasLow, bool hasHigh)
        {
            if (hasLow && hasHigh)
                return Template.Fill(Template.Fill(Above ? texts.ValueAboveAtMost : texts.ValueBetween, "least", Least), "most", Most);

            if (hasLow)
                return Template.Fill(Above ? texts.ValueAbove : texts.ValueOrMore, "least", Least);

            return Template.Fill(texts.ValueOrLess, "most", Most);
        }

        bool Knows(string said)
        {
            for (var i = 0; i < Words.Count; i++)
            {
                if (Words[i] == said)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// How a value reads in the box: with a place after the point where the field takes a fraction
        /// and the value was written without one.
        /// </summary>
        public string Shown(string said)
        {
            long whole;
            if (Entry != FieldInput.Decimal || string.IsNullOrEmpty(said)
                || !long.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
            {
                return said;
            }

            return $"{said}.0";
        }

        static bool Reads(string said, out double value)
        {
            value = 0;
            return !string.IsNullOrEmpty(said)
                && double.TryParse(said, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
