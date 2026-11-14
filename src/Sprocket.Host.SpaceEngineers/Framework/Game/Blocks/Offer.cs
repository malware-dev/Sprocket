using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// One thing the game says a block can be told to do, in the game's own terms.
    /// </summary>
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
        public Offer(string id, string text, ValueKind sets) : this(id, text, sets, "", "")
        {
        }

        /// <summary>One value a block carries, which it can be told to be, and which it will only accept from <paramref name="least" /> to <paramref name="most" />.</summary>
        public Offer(string id, string text, ValueKind sets, string least, string most)
        {
            Id = id;
            Text = text;
            Wants = 1;

            Sets = sets;
            Least = least ?? "";
            Most = most ?? "";
            Choices = null;
            Unreadable = false;
            Declares = null;
        }

        /// <summary>The least it will accept, or empty where it says nothing.</summary>
        public string Least { get; private set; }

        /// <summary>The most it will accept, or empty where it says nothing.</summary>
        public string Most { get; private set; }

        /// <summary>What kind of value it holds, where it is one.</summary>
        public ValueKind Sets { get; private set; }

        /// <summary>Whether this value is one of a listed few whose list the game would not hand over.</summary>
        public bool Unreadable { get; private set; }

        /// <summary>What type the action says each of its values is, one per value.</summary>
        /// <remarks>Null where the block was not asked.</remarks>
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
        /// This offer, for a value that is one of a listed few rather than anything within bounds.
        /// </summary>
        /// <remarks>Holds <paramref name="choices" /> rather than copying it, so it must not be written to afterwards.</remarks>
        public Offer Between(IList<Choice> choices)
        {
            Offer made = this;
            made.Choices = choices;
            return made;
        }

        /// <summary>
        /// Everything this value may be, or null where it is not one of a listed few.
        /// </summary>
        public IList<Choice> Choices { get; private set; }

        /// <summary>The game's own name for it, which is what a slot and a saved program hold.</summary>
        public string Id { get; private set; }

        /// <summary>What the terminal calls it, which is what the player has already read.</summary>
        public string Text { get; private set; }

        /// <summary>
        /// How many values it has to be given, which is nearly always none.
        /// </summary>
        public int Wants { get; private set; }
    }
}
