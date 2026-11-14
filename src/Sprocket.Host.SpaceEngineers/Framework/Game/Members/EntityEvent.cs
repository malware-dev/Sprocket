using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// Something that can happen to an entity, and which a rule can listen for.
    /// </summary>
    public sealed class EntityEvent
    {
        readonly Func<ToolbarSlot, string> _says;
        readonly Dictionary<string, EntityEvent> _slots;

        /// <summary>The moment <paramref name="id" />: <paramref name="flag" /> crossing <paramref name="edge" />, read in a sentence as <paramref name="text" />.</summary>
        public EntityEvent(string id, Reading flag, SignalEdge edge, string text)
        {
            Id = id;
            Flag = flag;
            Edge = edge;
            Text = text;
        }

        /// <summary>A signal: one the construct sends from the toolbar slot <paramref name="at" />, which its id names, with no reading behind it at all.</summary>
        public EntityEvent(ToolbarSlot at, string text)
        {
            Id = at.EventId;
            Text = text;
            Answered = true;
        }

        /// <summary>
        /// The signals of a toolbar a person presses, <paramref name="toolbar" />, as one: <paramref name="pages" /> pages of
        /// <paramref name="slots" /> slots, read as <paramref name="text" /> until a slot is chosen and then as
        /// <paramref name="says" /> words that slot.
        /// </summary>
        public EntityEvent(ToolbarSlot toolbar, string text, int pages, int slots, Func<ToolbarSlot, string> says)
            : this(toolbar, text)
        {
            Pages = pages;
            Slots = slots;
            _says = says;
            _slots = new Dictionary<string, EntityEvent>();
        }

        /// <summary>Stable identifier, referenced by saved rules.</summary>
        public string Id { get; private set; }

        /// <summary>
        /// Whether something on the construct answers this rather than the block reporting it.
        /// </summary>
        /// <remarks>An answered moment is never also sampled: that would start the same sequence twice on one press.</remarks>
        public bool Answered { get; private set; }

        /// <summary>Whether the block says when this may have happened, so it is looked for then rather than only on a cadence.</summary>
        public bool Announced { get; private set; }

        /// <summary>The flag that, while it reads true, rules this moment out, so the block need not be looked at then; null where nothing does.</summary>
        public Reading RuledOutBy { get; private set; }

        /// <summary>Why this moment is looked for on a cadence, where the block neither announces it nor anything that rules it out; empty where it is watched another way.</summary>
        public string LookedBecause { get; private set; } = "";

        /// <summary>The flag this moment is a change in, or null for a signal.</summary>
        public Reading Flag { get; private set; }

        /// <summary>Which change in <see cref="Flag" /> this moment is.</summary>
        public SignalEdge Edge { get; private set; }

        /// <summary>How this reads inside a sentence, completing "When the hangar door ___": "finishes opening".</summary>
        public string Text { get; private set; }

        /// <summary>How the block being in this moment reads, e.g. "is closed", to complete "if the hangar door ___"; empty where it is no state.</summary>
        public string State { get; private set; } = "";

        /// <summary>How the block not being in this moment reads, e.g. "is not closed".</summary>
        public string Unstate { get; private set; } = "";

        /// <summary>Whether this is a state a block is in or not, which a test can read.</summary>
        public bool IsState => State.Length > 0;

        /// <summary>How many pages of slots a person chooses among, or 0 where this is no toolbar a person presses.</summary>
        public int Pages { get; private set; }

        /// <summary>How many slots each of <see cref="Pages" /> has.</summary>
        public int Slots { get; private set; }

        /// <summary>Whether this is something Sprocket vouches for, which is what the organized view lists; the terminal view lists the rest.</summary>
        public bool Vetted { get; private set; } = true;

        /// <summary>Whether this is a toolbar a person presses, its page and slot chosen beside it.</summary>
        public bool IsPressed => Pages > 0;

        /// <summary>The id of Sprocket's own value this is a change in, or empty where it is a change in nothing a block has.</summary>
        public string Value { get; private set; } = "";

        /// <summary>What <see cref="Value" /> comes to be as this happens.</summary>
        public Literal Becomes { get; private set; }

        /// <summary>The signal of the one slot <paramref name="slot" /> of this toolbar, worded for it; this itself where it is no toolbar a person presses.</summary>
        /// <remarks>Made once for each slot, and kept.</remarks>
        public EntityEvent At(ToolbarSlot slot)
        {
            if (!IsPressed || slot == null || slot.IsMany)
                return this;

            var id = slot.EventId;
            EntityEvent one;
            if (!_slots.TryGetValue(id, out one))
            {
                one = new EntityEvent(slot, _says(slot)) { Vetted = Vetted };
                _slots.Add(id, one);
            }

            return one;
        }

        /// <summary>Whether <paramref name="eventId" /> names this: its own id, or one slot of a toolbar a person presses.</summary>
        public bool Names(string eventId)
        {
            if (eventId == Id)
                return true;

            var whole = IsPressed ? ToolbarSlot.Of(Id) : null;
            var slot = whole == null ? null : ToolbarSlot.Of(eventId);
            return slot != null && !slot.IsMany && whole.Covers(slot);
        }

        /// <summary>Says this is <paramref name="value" /> coming to be <paramref name="becomes" />.</summary>
        public void Changes(string value, Literal becomes)
        {
            Value = value;
            Becomes = becomes;
        }

        /// <summary>Says this is the game's own, which Sprocket does not vouch for.</summary>
        public void Unvouch()
        {
            Vetted = false;
        }

        /// <summary>Says the block announces this.</summary>
        public void Announce()
        {
            Announced = true;
        }

        /// <summary>Says <paramref name="flag" />, while it reads true, rules this moment out.</summary>
        public void RuleOutBy(Reading flag)
        {
            RuledOutBy = flag;
        }

        /// <summary>Says this moment is looked for on a cadence, and <paramref name="reason" /> why nothing better is possible.</summary>
        public void Look(string reason)
        {
            LookedBecause = reason;
        }

        /// <summary>Gives the moment the words a test reads it in, being in it and not.</summary>
        public void Read(string state, string unstate)
        {
            State = state;
            Unstate = unstate;
        }
    }
}
