using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>
    /// What one field of a form asks for: what it is called, and what it will take.
    /// </summary>
    /// <remarks>
    /// Told to the form rather than worked out by it, because the form knows nothing about what it is
    /// asking for. A field told nothing is a numbered box that takes whatever is put in it.
    /// <para>
    /// The bounds are written text and not numbers. They arrive from a file somebody else ships,
    /// where absent and zero are different things, and a bound nobody can read as a number is still
    /// worth saying beside the box even though nothing can be held to it.
    /// </para>
    /// <para>
    /// A value rather than an object, because one of these is made per value per question, and the
    /// control that raises a question is dressed on every tick.
    /// </para>
    /// </remarks>
    public struct Field
    {
        /// <summary>How many places after the point a decimal field gives room for.</summary>
        /// <remarks>Enough for a quarter of a metre on a piston, which is as fine as the game's own blocks go.</remarks>
        public const int Places = 2;

        /// <summary>What it is called, or empty to leave it numbered.</summary>
        public string Name;

        /// <summary>The least it may be, or empty where nobody has said.</summary>
        public string Least;

        /// <summary>The most it may be, or empty where nobody has said.</summary>
        public string Most;

        /// <summary>
        /// The only words it will take, or null where anything goes.
        /// </summary>
        /// <remarks>
        /// What a bound is for a number, this is for a value that is one of a few things. A block's
        /// switch takes on or off and nothing else, and a form that does not know that accepts
        /// whatever is typed, writes it into the program, and leaves the block to refuse it in
        /// silence at the moment it runs.
        /// </remarks>
        public IList<string> Words;

        /// <summary>
        /// What this is filled in with, where a box will not do.
        /// </summary>
        /// <remarks>
        /// Not a bound and not a name: it says which control asks the question, and everything else
        /// here describes what that control may answer with.
        /// </remarks>
        public Editor Edits;

        /// <summary>Whether the value has to be given: a field that stands for nothing when it is empty.</summary>
        /// <remarks>
        /// Most values may be left out, and the action then does what it does without one. Some cannot:
        /// a rotor told to turn at no speed sets out for somewhere it can never reach.
        /// </remarks>
        public bool Needed;

        /// <summary>Whether <see cref="Least" /> is a bound the value must be above, rather than one it may be.</summary>
        public bool Above;

        /// <summary>What the field starts with where nothing has been given yet, as it is shown, or null for nothing.</summary>
        public string Default;

        /// <summary>What a sentence says before this value, or null for nothing: "to" in "to 90°".</summary>
        public string Lead;

        /// <summary>What a sentence says straight after this value, or null for nothing: the "°" in "90°".</summary>
        public string Unit;

        /// <summary>
        /// The word a sentence leaves out, or null where it says every one.
        /// </summary>
        /// <remarks>
        /// For the choice that is what the action does anyway: a stuck room holds unless it is told otherwise, so
        /// "depressurize Airlock Vent" says it, and only "or move on" is worth the words.
        /// </remarks>
        public string Quiet;

        /// <summary>What the field is about, said where it is filled in, or null for nothing more than its name.</summary>
        public string About;

        /// <summary>What may be typed into the box.</summary>
        /// <remarks>
        /// Beside the bounds rather than read from them, because a bound says how long the whole part
        /// can be and nothing about the rest: a piston goes from nought to ten metres and stops at a
        /// quarter of one.
        /// </remarks>
        public Entry Entry;

        /// <summary>
        /// How many of the values behind the form this one field stands for.
        /// </summary>
        /// <remarks>
        /// One, except where several values are one thing to a person: a colour is three numbers to
        /// the game and one choice to anybody else. Read through <see cref="Covers" />, because a
        /// field is a value and a value nobody set is zero, which would mean a field standing for
        /// nothing at all.
        /// </remarks>
        public int Spans;

        /// <summary>How many values this field stands for, which is one unless it was told otherwise.</summary>
        public int Covers
        {
            get { return Spans < 1 ? 1 : Spans; }
        }

        /// <summary>One field standing over several values, filled in by a control of its own.</summary>
        public static Field Holding(string name, Editor edits, int spans)
        {
            Field field = Called(name);
            field.Edits = edits;
            field.Spans = spans;
            return field;
        }

        /// <summary>
        /// What the program holds for each of <see cref="Words" />, or null where they are the same.
        /// </summary>
        /// <remarks>
        /// Two different things wherever a value is one of a listed few: the terminal shows a charge
        /// mode as Auto and the block is told a number. Null where a word is its own value, which is
        /// every switch that says on and off, so those need nothing here.
        /// </remarks>
        public IList<string> Means;

        /// <summary>What the program holds for a word somebody chose.</summary>
        /// <remarks>
        /// The word itself where nothing says otherwise, which is what makes a field that never
        /// heard of this work unchanged.
        /// </remarks>
        public string Meaning(string word)
        {
            return Found(Words, word, Means);
        }

        /// <summary>The word for what the program holds, which is the other way round.</summary>
        public string Wording(string said)
        {
            return Found(Means, said, Words);
        }

        /// <summary>
        /// Whether <paramref name="given" />, already the kind this field holds, is one of its words where it lists some, and
        /// within its bounds where it has them and <paramref name="bounded" /> asks for them.
        /// </summary>
        /// <remarks>
        /// A step is held to the bounds, since the block takes nothing outside them; a test is not, since comparing with a
        /// number outside them still says something.
        /// </remarks>
        public bool Admits(Literal given, bool bounded)
        {
            var words = Means ?? Words;
            if (words != null && !words.Contains(given.Said))
                return false;

            var number = given.Kind == ValueKind.Integer || given.Kind == ValueKind.Real;
            if (!bounded || !number)
                return true;

            double bound;
            if (Bound(Least, out bound) && (Above ? given.Number <= bound : given.Number < bound))
                return false;

            return !(Bound(Most, out bound) && given.Number > bound);
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
        /// <remarks>
        /// Unchanged where either list is absent or the two have fallen out of step, because a
        /// mapping that is only half there is one nobody can trust either way round.
        /// </remarks>
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

        /// <summary>One that will only take one of a listed few, each standing for its own value.</summary>
        public static Field Among(string name, IList<string> words, IList<string> means)
        {
            Field field = Called(name);
            field.Edits = Editor.Chosen;
            field.Words = words;
            field.Means = means;
            return field;
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

        /// <summary>One that will only take a value between the two, either of which may be empty.</summary>
        public static Field Between(string name, string least, string most)
        {
            Field field = Called(name);
            field.Least = least ?? "";
            field.Most = most ?? "";
            return field;
        }

        /// <summary>One that will only take one of <paramref name="words" />.</summary>
        public static Field OneOf(string name, params string[] words)
        {
            Field field = Called(name);
            field.Words = words;
            return field;
        }

        /// <summary>
        /// What goes beside the box: what it is called, and what it will take.
        /// </summary>
        /// <remarks>
        /// Together, because the moment the bounds are worth knowing is the moment somebody is
        /// deciding what to type, and the only other place to say them is after it has been refused.
        /// </remarks>
        public string Label(Texts texts)
        {
            string name = Name ?? "";
            string takes = Takes(texts);

            if (takes.Length == 0)
                return name;

            return name.Length == 0 ? takes : name + " " + takes;
        }

        /// <summary>
        /// How the bounds read, or empty where there are none.
        /// </summary>
        /// <remarks>
        /// Either end on its own is worth saying: "0 or more" is a real thing to know, and so is a
        /// ceiling with no floor.
        /// </remarks>
        public string Takes(Texts texts)
        {
            if (Words != null && Words.Count > 0)
                return Listed(texts);

            string least = Least ?? "";
            string most = Most ?? "";

            // In brackets beside the name, as a range is written: "speed (0 - 30)".
            if (least.Length > 0 && most.Length > 0)
                return "(" + least + " - " + most + ")";

            if (least.Length > 0)
                return Template.Fill(texts.ValueAtLeast, "least", least);

            return most.Length > 0 ? Template.Fill(texts.ValueAtMost, "most", most) : "";
        }

        /// <summary>The words it takes, said the way somebody would say them aloud.</summary>
        string Listed(Texts texts)
        {
            var rest = "";
            for (var i = 0; i < Words.Count - 1; i++)
                rest += i == 0 ? Words[i] : ", " + Words[i];

            var last = Words[Words.Count - 1];
            return rest.Length == 0 ? last : Template.Fill(Template.Fill(texts.ValueEither, "last", last), "rest", rest);
        }

        /// <summary>
        /// Why <paramref name="said" /> cannot be sent, or empty where it can.
        /// </summary>
        /// <remarks>
        /// Only against what this field was told, which is the whole of what the form can know. A
        /// value the block will refuse for some reason of its own gets through here and is refused
        /// there, where it is said in the sequence's own problem line.
        /// <para>
        /// <paramref name="called" /> is what the field is called on screen rather than
        /// <see cref="Name" />, so the sentence names the box the way the words beside it do - which
        /// for a value nobody named is its place in the order.
        /// </para>
        /// </remarks>
        public string Refuses(Texts texts, string called, string said)
        {
            // An empty field is a value not given, and the action then does what it declared it does
            // without one - unless it declared that it needs one. Held to a bound it would be a value
            // of nothing, which is a different thing and a worse one.
            if (string.IsNullOrEmpty(said))
                return Needed ? Template.Fill(texts.ValueNeeded, "name", called) : "";

            if (Words != null && Words.Count > 0)
                return Knows(said) ? "" : Template.Fill(Template.Fill(texts.ValueOneOf, "words", Listed(texts)), "name", called);

            double low, high;
            bool hasLow = Reads(Least, out low);
            bool hasHigh = Reads(Most, out high);
            bool numeric = Entry == Entry.Whole || Entry == Entry.Decimal;

            if (!hasLow && !hasHigh && !numeric)
                return "";

            double value;
            if (!Reads(said, out value))
                return Template.Fill(texts.ValueNumber, "name", called);

            long whole;
            if (Entry == Entry.Whole
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
        /// <remarks>
        /// Only how it is shown. A program holding 5 for a speed holds a whole number, and the box
        /// showing 5.0 says the speed takes fractions without changing what was written.
        /// </remarks>
        public string Shown(string said)
        {
            long whole;
            if (Entry != Entry.Decimal || string.IsNullOrEmpty(said)
                || !long.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
            {
                return said;
            }

            return said + ".0";
        }

        static bool Reads(string said, out double value)
        {
            value = 0;
            return !string.IsNullOrEmpty(said)
                && double.TryParse(said, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
