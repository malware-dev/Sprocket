using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One thing the game says a block can be told to do, in the game's own terms.
    /// </summary>
    /// <remarks>
    /// What a block declares for itself, before Sprocket has any opinion about it. An
    /// <see cref="EntityAction" /> is what Sprocket makes of one: the same id, and words chosen for a
    /// sentence where there are better ones to be had.
    /// </remarks>
    public struct Offer
    {
        /// <summary>The action <paramref name="id" />, said as <paramref name="text" />, taking no values.</summary>
        public Offer(string id, string text) : this(id, text, 0)
        {
        }

        /// <summary>The action <paramref name="id" />, said as <paramref name="text" />, taking <paramref name="wants" /> values.</summary>
        public Offer(string id, string text, int wants)
        {
            Id = id;
            Text = text;
            Wants = wants;
            Sets = ValueKind.Flag;
            Least = "";
            Most = "";
            Choices = null;
            Unreadable = false;
            Declares = null;
        }

        /// <summary>
        /// One value a block carries, which it can be told to be.
        /// </summary>
        /// <remarks>
        /// The other half of what a block declares. An action is a thing to do; this is a thing to
        /// be, and the block says everything about it - what it is called, what kind of value it
        /// holds, and what it will accept - where an action says only how many values it takes.
        /// </remarks>
        public Offer(string id, string text, ValueKind sets) : this(id, text, sets, "", "")
        {
        }

        /// <summary>One value a block carries, which it can be told to be, and which it will only accept from <paramref name="least" /> to <paramref name="most" />.</summary>
        public Offer(string id, string text, ValueKind sets, string least, string most)
        {
            Id = id;
            Text = text;

            // One. A property holds a value, so telling it something is telling it one thing.
            Wants = 1;

            Sets = sets;
            Least = least ?? "";
            Most = most ?? "";
            Choices = null;
            Unreadable = false;
            Declares = null;
        }

        /// <summary>The least it will accept, and the most, or empty where it says nothing.</summary>
        public string Least { get; private set; }

        /// <summary>The most it will accept, or empty where it says nothing.</summary>
        public string Most { get; private set; }

        /// <summary>What kind of value it holds, where it is one.</summary>
        public ValueKind Sets { get; private set; }

        /// <summary>Whether this value is one of a listed few whose list the game would not hand over.</summary>
        public bool Unreadable { get; private set; }

        /// <summary>What type the action says each of its values is, one per value.</summary>
        /// <remarks>
        /// Null where the block was not asked. What an action declares is often wrong: most of the
        /// game's own declare text for values they refuse unless they are numbers, which is what
        /// <see cref="Known" /> is there to correct.
        /// </remarks>
        public IList<Primitive> Declares { get; private set; }

        /// <summary>This offer, saying with <paramref name="types" /> what type each of its values is declared as.</summary>
        public Offer Declaring(IList<Primitive> types)
        {
            Offer declaring = this;
            declaring.Declares = types;
            return declaring;
        }

        /// <summary>This offer, for a value whose list the game would not hand over.</summary>
        public Offer Unread()
        {
            Offer unread = this;
            unread.Unreadable = true;
            return unread;
        }

        /// <summary>
        /// The same, for a value that is one of a listed few rather than anything within bounds.
        /// </summary>
        /// <remarks>
        /// Handed back rather than set, because this is a struct and one that changed itself would
        /// change whatever copy happened to be in hand. The list is held rather than copied: it is
        /// built once per block kind and never written to afterwards.
        /// </remarks>
        public Offer Between(IList<Choice> choices)
        {
            Offer made = this;
            made.Choices = choices;
            return made;
        }

        /// <summary>
        /// Everything this value may be, or null where it is not one of a listed few.
        /// </summary>
        /// <remarks>
        /// Not on the value at all in the game's own terms: a set of permitted values lives on the
        /// terminal control, which is the same object the property is, and says both what each one is
        /// called and what the block is told for it.
        /// </remarks>
        public IList<Choice> Choices { get; private set; }

        /// <summary>The game's own name for it, which is what a slot and a saved program hold.</summary>
        public string Id { get; private set; }

        /// <summary>What the terminal calls it, which is what the player has already read.</summary>
        public string Text { get; private set; }

        /// <summary>
        /// How many values it has to be given, which is nearly always none.
        /// </summary>
        /// <remarks>
        /// A step cannot carry a value yet, so this is how an action that needs one is told apart
        /// from one that does not - and left out rather than offered as something that would do
        /// nothing when it ran. See issue 7.
        /// <para>
        /// The game declares one default per parameter, so the count is the arity and the type is
        /// the runtime type of each default. Nothing declares what a value may be <em>one of</em>;
        /// a set of permitted values exists on a terminal control rather than on an action.
        /// </para>
        /// </remarks>
        public int Wants { get; private set; }
    }
}
