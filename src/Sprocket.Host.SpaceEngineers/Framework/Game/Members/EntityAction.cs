using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// Something a sequence can tell an entity to do.
    /// </summary>
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

        /// <summary>The action <paramref name="id" />, said as <paramref name="text" />, standing for the game's member <paramref name="gameActionId" />, which the game will not perform until it is given <paramref name="wants" /> values.</summary>
        public EntityAction(string id, string text, string gameActionId, int wants)
        {
            Id = id;
            Text = text;
            GameActionId = gameActionId;
            Named = Ui.Parts.SentenceText.Capitalised(text);
            Declared = text;
            Why = UnreadyReason.None;
            Wants = wants;
            Category = Categories.Of(gameActionId);
        }

        /// <summary>Whether reaching the state it leads to takes time, rather than happening in the tick it is done.</summary>
        public bool Lasts { get; private set; }

        /// <summary>Whether this can be made into a step, which nearly everything can.</summary>
        public Readiness Readiness { get; private set; }

        /// <summary>Why it cannot be made into a step, for the person looking at it, or <see cref="UnreadyReason.None" /> where it can.</summary>
        public UnreadyReason Why { get; private set; }

        /// <summary>Keeps this from being made into a step, saying why.</summary>
        public void Withhold(Readiness readiness, UnreadyReason why)
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

        /// <summary>Whether this works through the grid's conveyors rather than the block's own machinery, so it goes on while the block is switched off.</summary>
        public bool Conveyed { get; private set; }

        /// <summary>Says this works through the grid's conveyors, which carry items through a switched-off block.</summary>
        public EntityAction ThroughConveyors()
        {
            Conveyed = true;
            return this;
        }

        /// <summary>Whether the block announces getting to the state this action leads to, so a step waiting on it is looked at then.</summary>
        public bool ArrivalAnnounced { get; private set; }

        /// <summary>Says the block announces getting to the state this action leads to.</summary>
        public void AnnounceArrival()
        {
            ArrivalAnnounced = true;
        }

        /// <summary>Whether this is offered only for a slot of its type, where the block has its own member saying the same.</summary>
        public bool InSlotsOnly { get; set; }

        /// <summary>The word a sentence says between this and the blocks it acts on, or empty for none: "from".</summary>
        public string Join { get; set; } = "";

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        string _namespace;

        /// <summary>
        /// The host's namespace this is in: Sprocket's own for its own actions and values, a mod's for one the mod adds, and
        /// <c>game</c> for the game's own.
        /// </summary>
        public string Namespace => _namespace ?? (Form == ReachKind.Native || OwnValue != null ? BlockKinds.Own : BlockKinds.Game);

        /// <summary>What names this in memory: its namespace and its id.</summary>
        public MemberKey Key => new MemberKey(Namespace, Id);

        /// <summary>Puts this in the host's namespace <paramref name="space" />, such as a mod's, in place of the one its form gives it.</summary>
        public EntityAction InNamespace(string space)
        {
            _namespace = space ?? "";
            return this;
        }

        /// <summary>How it reads in a sentence, as the instruction that completes "___ the hangar door": "open".</summary>
        public string Text { get; private set; }

        /// <summary>
        /// What it is called in a list of things to choose from.
        /// </summary>
        public string Named { get; private set; }

        /// <summary>What the block itself calls it, which the terminal view shows as written.</summary>
        public string Declared { get; private set; }

        /// <summary>What a list calls it, where that is not what a sentence calls it.</summary>
        public void List(string words)
        {
            Named = Ui.Parts.SentenceText.Capitalised(words);
        }

        /// <summary>Whether Sprocket has given one of the block's own members a word of its own.</summary>
        public bool Renamed { get; private set; }

        /// <summary>
        /// Gives one of the block's own members Sprocket's word for it, and nothing else.
        /// </summary>
        /// <remarks>The id, and what a program writes for it, stay the block's.</remarks>
        public void Rename(string word)
        {
            Renamed = true;
            Named = Ui.Parts.SentenceText.Capitalised(word);
            Text = word;
        }

        /// <summary>
        /// What this does as one of Sprocket's own, what it waits for and how it finishes; null for one of the game's own
        /// members, which the game performs.
        /// </summary>
        public OwnAction OwnAction { get; private set; }

        /// <summary>What it waits for, or null where it is done the moment it is told.</summary>
        public Reading Until => OwnAction?.Until;

        /// <summary>Gives it what it does as one of Sprocket's own.</summary>
        public EntityAction Performing(OwnAction own)
        {
            OwnAction = own;
            return this;
        }

        /// <summary>
        /// The game's own name for this act, or null where the game has no single action for it.
        /// </summary>
        public string GameActionId { get; private set; }

        /// <summary>
        /// The block's own value this writes, or null where it is an act rather than a value.
        /// </summary>
        public string GamePropertyId { get; private set; }

        /// <summary>Whether this is a value a step can write, which a program says with <c>set</c>: one of the block's own, or of Sprocket's own that can be set.</summary>
        public bool Sets => GamePropertyId != null || (OwnValue != null && OwnValue.Settable);

        /// <summary>What this reads as one of Sprocket's own values, or null for anything else.</summary>
        /// <remarks>Written in a program qualified by its namespace.</remarks>
        public OwnValue OwnValue { get; private set; }

        /// <summary>Whether the block says when this value of Sprocket's own may have changed, so it is looked at then rather than only on a cadence.</summary>
        public bool Announced { get; private set; }

        /// <summary>The flag that, while it reads true, rules out a change in this value, so the block need not be looked at then; null where nothing does.</summary>
        public Reading RuledOutBy { get; private set; }

        /// <summary>Why this value is looked at on a cadence, where the block neither announces it nor anything that rules it out; empty where it is watched another way.</summary>
        public string LookedBecause { get; private set; } = "";

        /// <summary>Which of the three ways a step reaches this one.</summary>
        public ReachKind Form => Sets || OwnValue != null ? ReachKind.Value : Unaltered ? ReachKind.Action : ReachKind.Native;

        /// <summary>Whether this stands as the game declared it, in the game's own words.</summary>
        public bool Unaltered { get; set; }

        /// <summary>Whether this is something Sprocket vouches for, which is what the organized view lists.</summary>
        public bool Vetted => !Unaltered || Renamed || _vouched;

        bool _vouched;

        /// <summary>Whether this is left out of the list somebody chooses from.</summary>
        /// <remarks>A hidden entry still answers anything that names it.</remarks>
        public bool Hidden { get; private set; }

        /// <summary>Whether this flag is also flipped, by the game's toggle beside it, chosen as <see cref="Toggle" /> on its switch.</summary>
        public bool Flips => FlippedBy != null;

        /// <summary>The id of the game's toggle that flips this flag, or null where nothing does.</summary>
        public string FlippedBy { get; private set; }

        /// <summary>
        /// What this member is about, which is the group it is listed under.
        /// </summary>
        public Category Category { get; set; }

        /// <summary>The heading of the group of its own a mod stands the member in, in the player's language, or null where it stands in its <see cref="Category" />.</summary>
        public string Group { get; set; }

        /// <summary>What the mod that adds the member says of it, in the player's language, or null for a member the codex describes.</summary>
        public string About { get; set; }

        /// <summary>The type the host provides that this is a member of, or null for a member of the block itself.</summary>
        /// <remarks>Its options receive the number of the thing it acts on first, from 1, ahead of the step's own values.</remarks>
        public HostType Type { get; set; }

        /// <summary>Whether this value is the status of the block, which a check in the organized view reads as the block itself: "Airlock Door is open".</summary>
        public bool IsStatus { get; set; }

        /// <summary>The kind of value this holds, where it is a value.</summary>
        public ValueKind Holds => _holds;

        /// <summary>Vouches for this one of the block's own members as the game declares it.</summary>
        public void Vouch()
        {
            _vouched = true;
        }

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

            // The id it writes is the only id the game gives a value.
            if (Category == Category.Other)
                Category = Categories.Of(propertyId);

            Named = Ui.Parts.SentenceText.Capitalised(named);
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

        /// <summary>Holds what a step sets this value to no higher than <paramref name="most" />, the bottom bound as it was.</summary>
        public void Capped(string most)
        {
            _writes.Most = most ?? "";
        }

        /// <summary>
        /// Says this value is in <paramref name="unit" />, said after its number, shown <paramref name="scale" /> times what it
        /// holds, and moving in <paramref name="step" /> as shown where that is more than 0.
        /// </summary>
        public void Measured(string unit, double scale, double step)
        {
            _writes.Unit = unit;
            _writes.Scale = scale;
            _writes.Step = step;
        }

        /// <summary>Has a step set this value as text written over several lines, up to <paramref name="longest" /> characters.</summary>
        public void Lines(int longest)
        {
            _writes.Edits = FieldEditor.Lines;
            _writes.Longest = longest;
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

        /// <summary>A copy of this value that asks for each part of its blocks by number.</summary>
        public EntityAction WithPartsNumbered()
        {
            var copy = (EntityAction)MemberwiseClone();
            copy.OwnValue = OwnValue.WithPartsNumbered();
            return copy;
        }

        /// <summary>Makes this one of Sprocket's own values, read by <paramref name="own" /> and shown by <paramref name="field" />.</summary>
        public EntityAction Reads(OwnValue own, Field field)
        {
            OwnValue = own;
            _holds = own.Kind;
            _writes = field;
            Wants = 1;
            return this;
        }

        /// <summary>
        /// What somebody has said its parameters are, or null where nobody has.
        /// </summary>
        public Description Description { get; private set; }

        /// <summary>Gives it the description of its values that a block's definition or a capability declares.</summary>
        public EntityAction Described(Description description)
        {
            Description = description;
            return this;
        }

        readonly List<Field> _described = new List<Field>();

        /// <summary>Says what type each of the values it is handed is taken as, one per value.</summary>
        /// <remarks>For one of the game's actions, whose declared types are often wrong.</remarks>
        public EntityAction Taking(IList<Primitive> types)
        {
            _takes = types;
            return this;
        }

        /// <summary>What type each value of one of the game's actions is taken as, or null for anything else.</summary>
        public IList<Primitive> Primitives => _takes;

        /// <summary>Gives it the values it asks for, one field per value, in the order they are written.</summary>
        /// <remarks>For an action of Sprocket's own.</remarks>
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
        /// <remarks>Asked of every step of every sequence being looked at; the caller keeps no list of fields.</remarks>
        public bool Needs(int given)
        {
            return Missing(given) != null;
        }

        /// <summary>
        /// What the first option among those <paramref name="given" /> leaves out is called; empty where nothing names it, and
        /// null where none is missing.
        /// </summary>
        public string Missing(int given)
        {
            _wanted.Clear();
            Fields(_wanted);

            var covered = 0;
            for (var i = 0; i < _wanted.Count; i++)
            {
                covered += _wanted[i].Covers;
                if (given < covered && !_wanted[i].MayBeLeftOut)
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
            return Form == ReachKind.Value && value.Becomes(_holds, out became);
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
        /// <remarks>Asked when a program is bound, never while it runs. Given null for <paramref name="into" />, it only answers.</remarks>
        public bool Converts(IList<Literal> given, List<Literal> into)
        {
            var from = into?.Count ?? 0;
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
            if (given.Count > Wants)
                return false;

            Literal became;
            if (GamePropertyId != null || OwnValue != null)
            {
                for (var i = 0; i < given.Count; i++)
                {
                    if (!given[i].Becomes(_holds, out became))
                        return false;

                    if (_holds == ValueKind.Name && OwnValue != null && OwnValue.Position(became.Text) < 0)
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

        /// <summary>The value among <paramref name="actions" /> that <paramref name="key" />, as a program wrote it, names; null where none does, or a bare name meets more than one.</summary>
        public static EntityAction Value(IList<EntityAction> actions, MemberKey key)
        {
            return Find(actions, key, true);
        }

        /// <summary>
        /// The member among <paramref name="actions" /> that <paramref name="key" />, as a program wrote it, names: a value where
        /// <paramref name="value" /> says so, as a <c>set</c> or a reading does, and an action otherwise, as a <c>call</c> does.
        /// Null where none does, or where a bare name meets more than one.
        /// </summary>
        /// <remarks>Asked where a program is bound or edited, never on a tick.</remarks>
        public static EntityAction Find(IList<EntityAction> actions, MemberKey key, bool value)
        {
            EntityAction found = null;
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if ((action.Form == ReachKind.Value) != value || !key.Names(action.Key))
                    continue;

                if (found != null)
                    return null;

                found = action;
            }

            return found;
        }

        /// <summary>
        /// Whether <paramref name="key" />, as a program wrote it, is a bare name that more than one of <paramref name="actions" />
        /// answers to, a value where <paramref name="value" /> says so and an action otherwise, which tells it no one member.
        /// </summary>
        public static bool Ambiguous(IList<EntityAction> actions, MemberKey key, bool value)
        {
            if (!key.IsBare)
                return false;

            var met = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                if ((actions[i].Form == ReachKind.Value) == value && key.Names(actions[i].Key) && ++met > 1)
                    return true;
            }

            return false;
        }

        /// <summary>What may be typed into a box for a value of this kind.</summary>
        public static FieldInput Entered(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Integer:
                    return FieldInput.Integer;
                case ValueKind.Real:
                    return FieldInput.Real;
                case ValueKind.Text:
                    return FieldInput.Text;
            }

            return FieldInput.Any;
        }

        /// <summary>The kind a box of this sort holds, or false where it may hold anything.</summary>
        public static bool Kind(FieldInput entry, out ValueKind kind)
        {
            kind = ValueKind.Text;
            switch (entry)
            {
                case FieldInput.Integer:
                    kind = ValueKind.Integer;
                    return true;
                case FieldInput.Real:
                    kind = ValueKind.Real;
                    return true;
                case FieldInput.Text:
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
                if (field.Covers == 1 && field.Entry == FieldInput.Any && at < _takes.Count)
                {
                    field.Entry = Entered(_takes[at]);
                    Bound(ref field, _takes[at]);
                    into[i] = field;
                }

                at += field.Covers;
            }
        }

        /// <summary>What may be typed into a box for a value taken as this type.</summary>
        static FieldInput Entered(Primitive type)
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
                    return FieldInput.Integer;
                case Primitive.Single:
                case Primitive.Double:
                    return FieldInput.Real;
                case Primitive.Char:
                case Primitive.String:
                    return FieldInput.Text;
            }

            return FieldInput.Any;
        }

        /// <summary>
        /// Gives a box the bounds of a small integer type, where nobody has given it any.
        /// </summary>
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

            if (GamePropertyId != null || OwnValue != null)
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

            // Counted by what each field covers: a color covers three values.
            var covered = 0;
            for (var i = 0; i < _described.Count && covered < Wants; i++)
            {
                into.Add(_described[i]);
                covered += _described[i].Covers;
            }

            for (var i = covered; i < Wants; i++)
                into.Add(Field.Unnamed());
        }

        /// <summary>
        /// How many values it has to be given before it will do anything.
        /// </summary>
        public int Wants { get; private set; }
    }
}
