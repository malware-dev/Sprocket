
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Something that can happen to an entity, and which a rule can listen for.
    /// </summary>
    /// <remarks>
    /// An edge on a flag read off the block: the moment the reading changes. The same reading
    /// backs the conditions somebody writes about it - "only if the door is open" - so a block
    /// is read one way for both the moment and the situation.
    /// </remarks>
    public sealed class EntityEvent
    {
        /// <summary>The moment <paramref name="id" />: <paramref name="flag" /> crossing <paramref name="edge" />, read in a sentence as <paramref name="text" />.</summary>
        public EntityEvent(string id, Reading flag, Edge edge, string text)
        {
            Id = id;
            Flag = flag;
            Edge = edge;
            Text = text;
        }

        /// <summary>A signal: one the construct sends from the toolbar slot <paramref name="at" />, which its id names, with no reading behind it at all.</summary>
        public EntityEvent(Sequences.ToolbarSlot at, string text)
        {
            Id = at.EventId;
            Text = text;
            Answered = true;
        }

        /// <summary>Stable identifier, referenced by saved rules.</summary>
        public string Id { get; private set; }

        /// <summary>
        /// Whether something on the construct answers this rather than the block reporting it.
        /// </summary>
        /// <remarks>
        /// A button is the case: it raises nothing until a slot on its toolbar holds something, and
        /// what it then runs is decided by that slot rather than by anything a rule could sample. A
        /// timer, a sensor and an event controller are the same, because each is a toolbar.
        /// <para>
        /// So there is nothing to watch, and watching anyway is worse than useless: the press arrives
        /// by the slot invoking Sprocket's action, and a rule sampling for it as well starts the same
        /// sequence twice on one press.
        /// </para>
        /// </remarks>
        public bool Answered { get; private set; }

        /// <summary>Whether the block says when this may have happened, so it is looked for then rather than only on a cadence.</summary>
        /// <remarks>See <see cref="Capability.Listen" />. Set by the capability that knows the block raises it.</remarks>
        public bool Announced { get; private set; }

        /// <summary>The flag that, while it reads true, rules this moment out, so the block need not be looked at then; null where nothing does.</summary>
        /// <remarks>
        /// For a moment the block does not announce, where it does announce when the moment becomes possible: a door
        /// says when it starts opening and when it has closed, and only between the two can it start closing. While every
        /// block watched reads this true, the moment is looked for only when one of them announces a change; otherwise on
        /// its cadence.
        /// </remarks>
        public Reading RuledOutBy { get; private set; }

        /// <summary>Why this moment is looked for on a cadence, where the block neither announces it nor anything that rules it out; empty where it is watched another way.</summary>
        public string LookedBecause { get; private set; } = "";

        /// <summary>The flag this moment is a change in, or null for a signal.</summary>
        public Reading Flag { get; private set; }

        /// <summary>Which change in <see cref="Flag" /> this moment is.</summary>
        public Edge Edge { get; private set; }

        /// <summary>
        /// How this reads inside a sentence, e.g. "finishes opening". Phrased to
        /// complete "When the hangar door ___", never as a technical name.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>How the block being in this moment reads, e.g. "is closed", to complete "if the hangar door ___"; empty where it is no state.</summary>
        public string State { get; private set; } = "";

        /// <summary>How the block not being in this moment reads, e.g. "is not closed".</summary>
        public string Unstate { get; private set; } = "";

        /// <summary>Whether this is a state a block is in or not, which a test can read.</summary>
        public bool IsState => State.Length > 0;

        /// <summary>The id of Sprocket's own value this is a change in, or empty where it is a change in nothing a block has.</summary>
        /// <remarks>A door finishing closing is its status coming to be closed; a test on the door reads that value.</remarks>
        public string Value { get; private set; } = "";

        /// <summary>What <see cref="Value" /> comes to be as this happens.</summary>
        public Literal Becomes { get; private set; }

        /// <summary>Says this is <paramref name="value" /> coming to be <paramref name="becomes" />.</summary>
        public void Changes(string value, Literal becomes)
        {
            Value = value;
            Becomes = becomes;
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
