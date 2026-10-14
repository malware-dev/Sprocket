using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Something a sequence can tell an entity to do.
    /// </summary>
    /// <remarks>
    /// Most carry no values at all. Of the game's own that do, each declares how many it wants and a
    /// type for each, which is often wrong, and nothing else: no names, and no set of what a value may
    /// be one of.
    /// </remarks>
    public sealed class EntityAction
    {
        /// <summary>The third choice on a flag's switch, after on and off, which writes the game's toggle for it.</summary>
        public const string Toggle = "toggle";

        /// <summary>The action <paramref name="id" />, said as <paramref name="text" />, standing for the game's member <paramref name="gameActionId" /> where there is one.</summary>
        public EntityAction(string id, string text, string gameActionId)
            : this(id, text, gameActionId, 0)
        {
        }

        Field _writes;

        ValueKind _holds;

        IList<Primitive> _takes;

        /// <summary>The same, for one the game will not perform until it is given <paramref name="wants" /> values.</summary>
        public EntityAction(string id, string text, string gameActionId, int wants)
        {
            Id = id;
            Text = text;
            GameActionId = gameActionId;
            Named = Capitalised(text);
            Declared = text;
            Why = Holdback.None;
            Wants = wants;
            Category = Categories.Of(gameActionId);
        }

        /// <summary>Whether reaching the state it leads to takes time, rather than happening in the tick it is done.</summary>
        /// <remarks>
        /// What the list somebody chooses from marks as holding the sequence until finished. Switching
        /// a block on leads to its being on in the same tick and holds nothing; a door takes seconds.
        /// </remarks>
        public bool Lasts { get; private set; }

        /// <summary>
        /// The same word as a list of things to choose from spells it.
        /// </summary>
        /// <remarks>
        /// The game capitalises the first letter of every member it names and leaves the rest of the
        /// words as they are - <c>Open</c>, <c>Toggle block On/Off</c>. Sprocket's own are written
        /// for the middle of a sentence, so the list is where they are made to match.
        /// </remarks>
        static string Capitalised(string text)
        {
            if (string.IsNullOrEmpty(text) || char.IsUpper(text[0]))
                return text;

            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        /// <summary>Whether this can be made into a step, which nearly everything can.</summary>
        public Readiness Readiness { get; private set; }

        /// <summary>Why it cannot be made into a step, for the person looking at it, or <see cref="Holdback.None" /> where it can.</summary>
        public Holdback Why { get; private set; }

        /// <summary>Keeps this from being made into a step, saying why.</summary>
        public void Withhold(Readiness readiness, Holdback why)
        {
            Readiness = readiness;
            Why = why;
        }

        /// <summary>Says that reaching this action's state takes time.</summary>
        public EntityAction Lasting()
        {
            Lasts = true;
            return this;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>
        /// How it reads in a sentence, e.g. "open". Phrased as an instruction so it
        /// completes "___ the hangar door".
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// What it is called in a list of things to choose from.
        /// </summary>
        /// <remarks>
        /// The same words for nearly everything, and not for a value: a sentence needs the verb -
        /// "set Falloff" - where a list under a heading that already says Values needs the name on
        /// its own, so the names sort as themselves rather than all under S.
        /// </remarks>
        public string Named { get; private set; }

        /// <summary>What the block itself calls it, which the terminal view shows as written.</summary>
        public string Declared { get; private set; }

        /// <summary>What a list calls it, where that is not what a sentence calls it.</summary>
        public void List(string words)
        {
            Named = Capitalised(words);
        }

        /// <summary>Whether Sprocket has given one of the block's own members a word of its own.</summary>
        public bool Renamed { get; private set; }

        /// <summary>
        /// Gives one of the block's own members Sprocket's word for it, and nothing else.
        /// </summary>
        /// <remarks>
        /// The member stays the block's: the same id, performed the same way, written into a program
        /// the same way. Only what a list and a sentence call it changes, and the terminal view goes on
        /// showing what the block calls it.
        /// </remarks>
        public void Rename(string word)
        {
            Renamed = true;
            Named = Capitalised(word);
            Text = word;
        }

        /// <summary>
        /// What this does as one of Sprocket's own, what it waits for and how it finishes; null for one of the game's own
        /// members, which the game performs.
        /// </summary>
        public Doing Doing { get; private set; }

        /// <summary>What it waits for, or null where it is done the moment it is told.</summary>
        public Reading Until => Doing == null ? null : Doing.Until;

        /// <summary>Gives it what it does as one of Sprocket's own.</summary>
        public EntityAction Performing(Doing doing)
        {
            Doing = doing;
            return this;
        }

        /// <summary>
        /// The game's own name for this act, or null where the game has no single action for it.
        /// </summary>
        /// <remarks>
        /// What lets a toolbar slot somebody wired to the game's own action be said in Sprocket's words.
        /// The game's convention is that an on/off switch called <c>X</c> offers <c>X_On</c> and
        /// <c>X_Off</c>; a door's switch is <c>Open</c>, a vent's is <c>Depressurize</c>, and every
        /// functional block has <c>OnOff</c>.
        /// <para>
        /// Null for anything Sprocket performs out of several property writes, and for any block whose
        /// own actions have not been mapped yet.
        /// </para>
        /// </remarks>
        public string GameActionId { get; private set; }

        /// <summary>
        /// The block's own value this writes, or null where it is an act rather than a value.
        /// </summary>
        /// <remarks>
        /// The second of the two ways an act is carried out. A block declares a handful of actions
        /// and carries far more values than that, and a value says what it is and what it will take
        /// where an action says only how many values it wants.
        /// </remarks>
        public string GamePropertyId { get; private set; }

        /// <summary>Whether this is a value a step can write, which a program says with <c>set</c>: one of the block's own, or of Sprocket's own that can be set.</summary>
        public bool Sets => GamePropertyId != null || (Being != null && Being.Settable);

        /// <summary>What this reads as one of Sprocket's own values, or null for anything else.</summary>
        /// <remarks>Written qualified by its namespace; set where its Being can be set, and read only otherwise.</remarks>
        public Being Being { get; private set; }

        /// <summary>Whether the block says when this value of Sprocket's own may have changed, so it is looked at then rather than only on a cadence.</summary>
        /// <remarks>See <see cref="Capability.Listen" />.</remarks>
        public bool Announced { get; private set; }

        /// <summary>The flag that, while it reads true, rules out a change in this value, so the block need not be looked at then; null where nothing does.</summary>
        public Reading RuledOutBy { get; private set; }

        /// <summary>Why this value is looked at on a cadence, where the block neither announces it nor anything that rules it out; empty where it is watched another way.</summary>
        public string LookedBecause { get; private set; } = "";

        /// <summary>Which of the three ways a step reaches this one.</summary>
        /// <remarks>
        /// A value, the block's own or Sprocket's, is a value; anything the block itself declared is the game's action,
        /// whatever Sprocket calls it; and what is left is Sprocket's own.
        /// </remarks>
        public Form Form => Sets || Being != null ? Form.Value : Unaltered ? Form.Action : Form.Native;

        /// <summary>Whether this stands as the game declared it, in the game's own words.</summary>
        /// <remarks>
        /// What the terminal view lists, whether or not the organized one has replaced it. A
        /// capability's entry is Sprocket's; one the block declares and nothing has reworded is this.
        /// </remarks>
        public bool Unaltered { get; set; }

        /// <summary>Whether this is something Sprocket vouches for, which is what the organized view lists.</summary>
        /// <remarks>
        /// Sprocket's own members are vouched for by being written, and one of the block's own by a capability renaming it
        /// or naming it as vetted in the game's words. Anything else the block declares, a member a game update or a mod
        /// adds among them, is listed by the terminal view alone.
        /// </remarks>
        public bool Vetted => !Unaltered || Renamed || _vouched;

        bool _vouched;

        /// <summary>Whether this is left out of the list somebody chooses from.</summary>
        /// <remarks>
        /// Only where a capability's entry stands in for it, naming it as the game's own form of what
        /// that entry does. Still there for anything that names it, and for a list that shows
        /// everything.
        /// </remarks>
        public bool Hidden { get; private set; }

        /// <summary>Whether this flag is also flipped, by the game's toggle beside it, chosen as <see cref="Toggle" /> on its switch.</summary>
        public bool Flips => FlippedBy != null;

        /// <summary>The id of the game's toggle that flips this flag, or null where nothing does.</summary>
        public string FlippedBy { get; private set; }

        /// <summary>
        /// What this member is about, which is the group it is listed under.
        /// </summary>
        /// <remarks>
        /// Worked out from the game's own id where there is one - see <see cref="Categories" /> - and
        /// said outright by a capability for anything Sprocket invented, which has no game id to ask
        /// about.
        /// </remarks>
        public Category Category { get; set; }

        /// <summary>Whether this value is the status of the block, which a check in the organized view reads as the block itself: "Airlock Door is open".</summary>
        /// <remarks>The editor's wording alone; the script names the value as it names any other.</remarks>
        public bool IsStatus { get; set; }

        /// <summary>The kind of value this holds, where it is a value.</summary>
        public ValueKind Holds => _holds;

        /// <summary>Vouches for this one of the block's own members as the game declares it.</summary>
        public void Vouch()
        {
            _vouched = true;
        }

        /// <summary>Leaves this out of the list somebody chooses from.</summary>
        /// <summary>Says this flag is also flipped, by the game's toggle <paramref name="toggle" />: its switch offers on, off and toggle.</summary>
        public void Flipped(string toggle)
        {
            FlippedBy = toggle;
            _writes.Words = new[] { Literal.Of(true).Said, Literal.Of(false).Said, Toggle };
        }

        /// <summary>Keeps it out of the organized view.</summary>
        public void Hide()
        {
            Hidden = true;
        }

        /// <summary>Makes this the setting of the block's value <paramref name="propertyId" />, named <paramref name="named" />, holding a <paramref name="holds" /> asked for by <paramref name="field" />.</summary>
        public EntityAction Writes(string propertyId, Field field, string named, ValueKind holds)
        {
            GamePropertyId = propertyId;
            _holds = holds;

            // A value is asked about by the id it writes, since that is the only id the game gave it.
            if (Category == Category.Other)
                Category = Categories.Of(propertyId);

            Named = Capitalised(named);
            Declared = named;
            _writes = field;
            return this;
        }

        /// <summary>Holds what a step sets this value to between <paramref name="least" /> and <paramref name="most" />, each written as a number or empty for none.</summary>
        public void Between(string least, string most)
        {
            _writes.Least = least ?? "";
            _writes.Most = most ?? "";
        }

        /// <summary>Says the block announces when this value may have changed.</summary>
        public void Announce()
        {
            Announced = true;
        }

        /// <summary>Says <paramref name="flag" />, while it reads true, rules out a change in this value.</summary>
        public void RuleOutBy(Reading flag)
        {
            RuledOutBy = flag;
        }

        /// <summary>Says this value is looked at on a cadence, and <paramref name="reason" /> why nothing better is possible.</summary>
        public void Look(string reason)
        {
            LookedBecause = reason;
        }

        /// <summary>Makes this one of Sprocket's own values, read by <paramref name="being" /> and shown by <paramref name="field" />.</summary>
        public EntityAction Reads(Being being, Field field)
        {
            Being = being;
            _holds = being.Kind;
            _writes = field;
            Wants = 1;
            return this;
        }

        /// <summary>
        /// What somebody has said its parameters are, or null where nobody has.
        /// </summary>
        /// <remarks>
        /// The action declares how many parameters it takes; this is the only thing that can say
        /// what they are for. It comes from a file a mod ships, or from this mod's own description
        /// of the game's own blocks - see <see cref="Catalogue" />.
        /// </remarks>
        public Description Description { get; private set; }

        /// <summary>Gives it the description of its values that a block's definition or a capability declares.</summary>
        public EntityAction Described(Description description)
        {
            Description = description;
            return this;
        }

        readonly List<Field> _described = new List<Field>();

        /// <summary>Says what type each of the values it is handed is taken as, one per value.</summary>
        /// <remarks>For one of the game's actions, whose declaration says and is often wrong - see <see cref="Known" />.</remarks>
        public EntityAction Taking(IList<Primitive> types)
        {
            _takes = types;
            return this;
        }

        /// <summary>What type each value of one of the game's actions is taken as, or null for anything else.</summary>
        public IList<Primitive> Primitives
        {
            get { return _takes; }
        }

        /// <summary>Gives it the values it asks for, one field per value, in the order they are written.</summary>
        /// <remarks>For an action of Sprocket's own, which describes itself rather than being described.</remarks>
        public EntityAction Asking(IList<Field> fields)
        {
            _asks = fields;
            Wants = 0;
            for (var i = 0; i < fields.Count; i++)
                Wants += fields[i].Covers;

            return this;
        }

        IList<Field> _asks;

        /// <summary>Whether a value it takes is among the ones it has not been given, every one being needed.</summary>
        /// <remarks>
        /// Asked of the action rather than worked out from a list of its fields, so nothing has to keep
        /// a list to ask the question - which is asked of every step of every sequence being looked at.
        /// </remarks>
        public bool Needs(int given)
        {
            return Missing(given) != null;
        }

        /// <summary>
        /// What the first option among those <paramref name="given" /> leaves out is called; empty where nothing names it, and
        /// null where none is missing.
        /// </summary>
        /// <remarks>A member is given an argument for every parameter it declares, so each option is needed.</remarks>
        public string Missing(int given)
        {
            _wanted.Clear();
            Fields(_wanted);

            var covered = 0;
            for (var i = 0; i < _wanted.Count; i++)
            {
                covered += _wanted[i].Covers;
                if (given < covered)
                    return _wanted[i].Name ?? "";
            }

            return null;
        }

        /// <summary>
        /// What the first of <paramref name="given" /> that this cannot take is called; empty where nothing names it, and null
        /// where it takes them all.
        /// </summary>
        /// <remarks>Asked when a program is bound, never while it runs.</remarks>
        public string Refused(IList<Literal> given)
        {
            var some = new List<Literal>(given.Count);
            for (var i = 0; i < given.Count; i++)
            {
                some.Add(given[i]);
                if (!Converts(some, null))
                    return Called(i);
            }

            return null;
        }

        /// <summary>What the option at <paramref name="at" /> among the values it takes is called, or empty where nothing names it.</summary>
        string Called(int at)
        {
            _wanted.Clear();
            Fields(_wanted);

            var from = 0;
            for (var i = 0; i < _wanted.Count; i++)
            {
                from += _wanted[i].Covers;
                if (at < from)
                    return _wanted[i].Name ?? "";
            }

            return "";
        }

        readonly List<Field> _wanted = new List<Field>();

        readonly List<Literal> _converted = new List<Literal>();

        /// <summary><paramref name="value" /> as the kind this value holds, for a test to compare it with; false where it cannot be one, or this is no value.</summary>
        public bool Compares(Literal value, out Literal became)
        {
            became = value;
            return Form == Form.Value && value.Becomes(_holds, out became);
        }

        /// <summary>Whether every value a test compares this with is one it can be: its kind, and one of its words where it lists some.</summary>
        public bool Takes(IList<Literal> given)
        {
            _converted.Clear();
            return Convert(given, _converted) && Admits(_converted, 0, false);
        }

        /// <summary>
        /// Puts the values a step was given into the kinds this takes, or answers false where one
        /// cannot be.
        /// </summary>
        /// <remarks>
        /// Asked when a program is bound and never while it runs, so what a block is handed is already
        /// what it takes: the kind a value of the block's holds, the type one of the game's actions
        /// takes it as, the kind the box for one of Sprocket's own is for. A value nothing says the
        /// kind of goes as it was written. Given nowhere to put them, it only answers.
        /// </remarks>
        /// <remarks>Each value is also held to its field: one of its words where it lists some, and within its bounds.</remarks>
        public bool Converts(IList<Literal> given, List<Literal> into)
        {
            var from = into == null ? 0 : into.Count;
            if (into == null)
            {
                _converted.Clear();
                into = _converted;
            }

            return Convert(given, into) && Admits(into, from, true);
        }

        /// <summary>Whether each of <paramref name="values" /> from <paramref name="from" /> on is one its field admits.</summary>
        bool Admits(List<Literal> values, int from, bool bounded)
        {
            _wanted.Clear();
            Fields(_wanted);

            var at = from;
            for (var f = 0; f < _wanted.Count && at < values.Count; f++)
            {
                if (_wanted[f].Covers == 1 && !_wanted[f].Admits(values[at], bounded))
                    return false;

                at += _wanted[f].Covers;
            }

            return true;
        }

        /// <summary>Puts the values a step was given into the kinds this takes, or answers false where one cannot be.</summary>
        bool Convert(IList<Literal> given, List<Literal> into)
        {
            // An argument for a parameter it does not declare is one it cannot take.
            if (given.Count > Wants)
                return false;

            Literal became;
            if (GamePropertyId != null || Being != null)
            {
                for (var i = 0; i < given.Count; i++)
                {
                    if (!given[i].Becomes(_holds, out became))
                        return false;

                    if (_holds == ValueKind.Name && Being != null && Being.Position(became.Text) < 0)
                        return false;

                    if (into != null)
                        into.Add(became);
                }

                return true;
            }

            if (_takes != null)
            {
                for (var i = 0; i < given.Count; i++)
                {
                    if (!given[i].Fits(i < _takes.Count ? _takes[i] : Primitive.None, out became))
                        return false;

                    if (into != null)
                        into.Add(became);
                }

                return true;
            }

            _wanted.Clear();
            Fields(_wanted);

            var at = 0;
            for (var f = 0; f < _wanted.Count && at < given.Count; f++)
            {
                ValueKind kind;
                if (_wanted[f].Covers != 1 || !Kind(_wanted[f].Entry, out kind) || _wanted[f].Unfilled(given[at]))
                {
                    for (var c = 0; c < _wanted[f].Covers && at < given.Count; c++, at++)
                    {
                        if (into != null)
                            into.Add(given[at]);
                    }

                    continue;
                }

                if (!given[at].Becomes(kind, out became))
                    return false;

                if (into != null)
                    into.Add(became);

                at++;
            }

            for (; at < given.Count && into != null; at++)
                into.Add(given[at]);

            return true;
        }

        /// <summary>
        /// The value <paramref name="id" /> among <paramref name="actions" />: Sprocket's own where <paramref name="provider" />
        /// is <c>ext</c>, the block's own where it is empty; null where there is none.
        /// </summary>
        public static EntityAction Value(IList<EntityAction> actions, string provider, string id)
        {
            var own = provider == BlockKinds.Own;
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action.Id == id && action.Form == Form.Value && (action.Being != null) == own)
                    return action;
            }

            return null;
        }

        /// <summary>What may be typed into a box for a value of this kind.</summary>
        public static Entry Entered(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Integer:
                    return Entry.Whole;
                case ValueKind.Real:
                    return Entry.Decimal;
                case ValueKind.Text:
                    return Entry.Text;
            }

            return Entry.Any;
        }

        /// <summary>The kind a box of this sort holds, or false where it may hold anything.</summary>
        public static bool Kind(Entry entry, out ValueKind kind)
        {
            kind = ValueKind.Text;
            switch (entry)
            {
                case Entry.Whole:
                    kind = ValueKind.Integer;
                    return true;
                case Entry.Decimal:
                    kind = ValueKind.Real;
                    return true;
                case Entry.Text:
                    return true;
            }

            return false;
        }

        /// <summary>Adds to <paramref name="into" /> the field each of its options is asked for by, in the order they are given.</summary>
        public void Fields(IList<Field> into)
        {
            var from = into.Count;
            Listed(into);
            Entered(into, from);
        }

        /// <summary>Holds each box to the type its value is taken as, where the box says nothing of its own.</summary>
        void Entered(IList<Field> into, int from)
        {
            if (_takes == null)
                return;

            var at = 0;
            for (var i = from; i < into.Count; i++)
            {
                var field = into[i];
                if (field.Covers == 1 && field.Entry == Entry.Any && at < _takes.Count)
                {
                    field.Entry = Entered(_takes[at]);
                    Bound(ref field, _takes[at]);
                    into[i] = field;
                }

                at += field.Covers;
            }
        }

        /// <summary>What may be typed into a box for a value taken as this type.</summary>
        static Entry Entered(Primitive type)
        {
            switch (type)
            {
                case Primitive.Byte:
                case Primitive.SByte:
                case Primitive.Int16:
                case Primitive.UInt16:
                case Primitive.Int32:
                case Primitive.UInt32:
                case Primitive.Int64:
                case Primitive.UInt64:
                    return Entry.Whole;
                case Primitive.Single:
                case Primitive.Double:
                    return Entry.Decimal;
                case Primitive.Char:
                case Primitive.String:
                    return Entry.Text;
            }

            return Entry.Any;
        }

        /// <summary>
        /// Gives a box the bounds of a small whole-number type, where nobody has given it any.
        /// </summary>
        /// <remarks>Only the small ones: the bounds of a 32-bit number are nothing anybody wants to read beside a box.</remarks>
        static void Bound(ref Field field, Primitive type)
        {
            if (field.Least.Length > 0 || field.Most.Length > 0)
                return;

            switch (type)
            {
                case Primitive.Byte:
                    field.Least = "0";
                    field.Most = "255";
                    break;
                case Primitive.SByte:
                    field.Least = "-128";
                    field.Most = "127";
                    break;
                case Primitive.Int16:
                    field.Least = "-32768";
                    field.Most = "32767";
                    break;
                case Primitive.UInt16:
                    field.Least = "0";
                    field.Most = "65535";
                    break;
            }
        }

        void Listed(IList<Field> into)
        {
            if (_asks != null)
            {
                for (var i = 0; i < _asks.Count; i++)
                    into.Add(_asks[i]);

                return;
            }

            // A value describes itself, so nothing has to have been written about it.
            if (GamePropertyId != null || Being != null)
            {
                into.Add(_writes);
                return;
            }

            if (Description == null)
            {
                for (var i = 0; i < Wants; i++)
                    into.Add(Field.Unnamed());

                return;
            }

            _described.Clear();
            Description.Fields(_described);

            // Counted by what each field stands for rather than by how many fields there are. One of
            // them can cover several values - a colour covers three - so a description of two fields
            // can account for all four values an action wants.
            var covered = 0;
            for (var i = 0; i < _described.Count && covered < Wants; i++)
            {
                into.Add(_described[i]);
                covered += _described[i].Covers;
            }

            // Whatever the description did not reach is still asked for, told nothing. A description
            // that has fallen behind the block it is about loses the names and keeps the values.
            for (var i = covered; i < Wants; i++)
                into.Add(Field.Unnamed());
        }

        /// <summary>
        /// How many values it has to be given before it will do anything.
        /// </summary>
        /// <remarks>
        /// For one of the game's actions, the game declares one default per value it wants, so this is the count of those
        /// - which is all it declares: what each one is for is not said, and the type of each is the runtime type of the
        /// default it was declared with. For an action of Sprocket's own it is what its fields cover, and for a value
        /// one: what it is set to, or compared with.
        /// </remarks>
        public int Wants { get; private set; }
    }
}
