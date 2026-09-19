using System.Collections.Generic;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>
    /// What one field of a form asks for: what it is called, and what it will take.
    /// </summary>
    /// <remarks>
    /// Told to the form rather than worked out by it, because nothing in the game says any of this:
    /// an action declares how many values it wants and not one word about any of them. A field told
    /// nothing is a numbered box that takes whatever is put in it, which is the usual case.
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

        /// <summary>A field nobody has described, which is a box that takes anything.</summary>
        public static Field Unnamed()
        {
            return Called("");
        }

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
        public string Label
        {
            get
            {
                string name = Name ?? "";
                string takes = Takes;

                if (takes.Length == 0)
                    return name;

                return name.Length == 0 ? takes : name + " " + takes;
            }
        }

        /// <summary>
        /// How the bounds read, or empty where there are none.
        /// </summary>
        /// <remarks>
        /// Either end on its own is worth saying: "0 or more" is a real thing to know, and so is a
        /// ceiling with no floor.
        /// </remarks>
        public string Takes
        {
            get
            {
                if (Words != null && Words.Count > 0)
                    return Listed();

                string least = Least ?? "";
                string most = Most ?? "";

                if (least.Length > 0 && most.Length > 0)
                    return least + "-" + most;

                if (least.Length > 0)
                    return least + " or more";

                return most.Length > 0 ? most + " or less" : "";
            }
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
        /// <summary>The words it takes, said the way somebody would say them aloud.</summary>
        string Listed()
        {
            var said = "";
            for (var i = 0; i < Words.Count; i++)
            {
                if (said.Length > 0)
                    said += i == Words.Count - 1 ? " or " : ", ";

                said += Words[i];
            }

            return said;
        }

        public string Refuses(string called, string said)
        {
            // An empty field is a value not given, and the action then does what it declared it does
            // without one. Held to a bound it would be a value of nothing, which is a different
            // thing and a worse one.
            if (string.IsNullOrEmpty(said))
                return "";

            if (Words != null && Words.Count > 0)
                return Knows(said) ? "" : called + " has to be " + Listed() + ".";

            double low, high;
            bool hasLow = Reads(Least, out low);
            bool hasHigh = Reads(Most, out high);

            if (!hasLow && !hasHigh)
                return "";

            double value;
            if (!Reads(said, out value))
                return called + " has to be a number.";

            if ((hasLow && value < low) || (hasHigh && value > high))
                return called + " has to be " + Within(hasLow, hasHigh) + ".";

            return "";
        }

        /// <summary>The bounds said as part of a sentence, rather than as the label's shorthand.</summary>
        string Within(bool hasLow, bool hasHigh)
        {
            if (hasLow && hasHigh)
                return "between " + Least + " and " + Most;

            return hasLow ? Least + " or more" : Most + " or less";
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

        static bool Reads(string said, out double value)
        {
            value = 0;
            return !string.IsNullOrEmpty(said)
                && double.TryParse(said, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
