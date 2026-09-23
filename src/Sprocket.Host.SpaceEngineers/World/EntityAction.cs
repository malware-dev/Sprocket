using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Something a sequence can tell an entity to do.
    /// </summary>
    /// <remarks>
    /// Most carry no values at all. The ones that do are the game's own, which declare how many they
    /// want and nothing else about them: no names, and no set of what a value may be one of.
    /// </remarks>
    public sealed class EntityAction
    {
        public EntityAction(string id, string text, string leadsToStateId, string gameActionId)
            : this(id, text, leadsToStateId, gameActionId, 0)
        {
        }

        /// <summary>What the value it writes is called, and what it will take.</summary>
        Field _writes;

        public EntityAction(string id, string text, string leadsToStateId, string gameActionId,
            int wants)
        {
            Id = id;
            Text = text;
            LeadsToStateId = leadsToStateId;
            GameActionId = gameActionId;
            Named = Capitalised(text);
            Why = "";
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

        /// <summary>Why it cannot be made into a step, for the person looking at it, or empty where it can.</summary>
        public string Why { get; private set; }

        /// <summary>Keeps this from being made into a step, saying why.</summary>
        public void Withhold(Readiness readiness, string why)
        {
            Readiness = readiness;
            Why = why ?? "";
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

        /// <summary>
        /// The <see cref="EntityState"/> this action is trying to bring about, or
        /// null if it does not settle into a state.
        /// </summary>
        /// <remarks>
        /// This is what makes interlocks possible. Knowing that "open" is an attempt
        /// to reach "is open" lets the runtime refuse the action while a promise says
        /// that state must not hold - and, far more importantly, say so afterwards.
        /// An action that declares nothing here simply cannot be interlocked.
        /// </remarks>
        public string LeadsToStateId { get; private set; }

        /// <summary>
        /// The game's own name for this act, or null where the game has no single action for it.
        /// </summary>
        /// <remarks>
        /// What lets a trigger that does one plain thing be written into a toolbar slot as the game's
        /// action rather than as Sprocket's, so the button keeps working with the mod removed. The
        /// game's convention is that an on/off switch called <c>X</c> offers <c>X_On</c> and
        /// <c>X_Off</c>; a door's switch is <c>Open</c>, a vent's is <c>Depressurize</c>, and every
        /// functional block has <c>OnOff</c>.
        /// <para>
        /// Null for anything Sprocket performs out of several property writes, and for any block whose
        /// own actions have not been mapped yet. A name is never trusted: the block is asked for it
        /// before a slot is written, so a wrong or missing one costs a Sprocket trigger rather than a
        /// dead button.
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

        /// <summary>Whether this writes one of the block's own values, which a program says with <c>set</c>.</summary>
        public bool Sets => GamePropertyId != null;

        /// <summary>Whether this stands as the game declared it, in the game's own words.</summary>
        /// <remarks>
        /// What the terminal view lists, whether or not the organized one has replaced it. An
        /// extension's entry is Sprocket's; one the block declares and nothing has reworded is this.
        /// </remarks>
        public bool Unaltered { get; internal set; }

        /// <summary>Whether this is left out of the list somebody chooses from.</summary>
        /// <remarks>
        /// Only where an extension stands in for it, naming it as the game's own form of what the
        /// extension's entry does. Still there for anything that names it, and for a list that shows
        /// everything.
        /// </remarks>
        public bool Hidden { get; private set; }

        /// <summary>
        /// What this member is about, which is the group it is listed under.
        /// </summary>
        /// <remarks>
        /// Worked out from the game's own id where there is one - see <see cref="Categories" /> - and
        /// said outright by an extension for anything Sprocket invented, which has no game id to ask
        /// about.
        /// </remarks>
        public Category Category { get; internal set; }

        /// <summary>Leaves this out of the list somebody chooses from.</summary>
        public void Hide()
        {
            Hidden = true;
        }

        public EntityAction Writes(string propertyId, Field field, string named)
        {
            GamePropertyId = propertyId;

            // A value is asked about by the id it writes, since that is the only id the game gave it.
            if (Category == Category.Other)
                Category = Categories.Of(propertyId);

            Named = Capitalised(named);
            _writes = field;
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

        public EntityAction Described(Description description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        /// What each value it wants asks for, one per value, told nothing where nobody has said.
        /// </summary>
        /// <remarks>
        /// One per value, in order, because the form asks for them one at a time whatever the
        /// grouping says. A group's parameters are asked for individually until there is an editor
        /// for that group's kind, at which point the group is asked for as one thing instead.
        /// </remarks>
        readonly List<Field> _described = new List<Field>();

        /// <summary>Gives it the values it asks for, one field per value, in the order they are written.</summary>
        /// <remarks>For an action of Sprocket's own, which describes itself rather than being described.</remarks>
        public EntityAction Asking(IList<Field> fields)
        {
            _asks = fields;
            Wants = fields.Count;
            return this;
        }

        /// <summary>The fields an action of Sprocket's own asks with, or null.</summary>
        IList<Field> _asks;

        /// <summary>Whether a value it cannot do without is among the ones it has not been given.</summary>
        /// <remarks>
        /// Asked of the action rather than worked out from a list of its fields, so nothing has to keep
        /// a list to ask the question - which is asked of every step of every sequence being looked at.
        /// </remarks>
        public bool Needs(int given)
        {
            _wanted.Clear();
            Fields(_wanted);

            for (var i = 0; i < _wanted.Count; i++)
            {
                if (_wanted[i].Needed && i >= given)
                    return true;
            }

            return false;
        }

        /// <summary>What it asks for, while it is being asked whether it has it. Reused, never held.</summary>
        readonly List<Field> _wanted = new List<Field>();

        public void Fields(IList<Field> into)
        {
            if (_asks != null)
            {
                for (var i = 0; i < _asks.Count; i++)
                    into.Add(_asks[i]);

                return;
            }

            // A value describes itself, so nothing has to have been written about it.
            if (GamePropertyId != null)
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
        /// Zero for everything of Sprocket's own. The game declares one default per value it wants, so
        /// this is the count of those - which is all it declares: what each one is for is not said,
        /// and the type of each is the runtime type of the default it was declared with.
        /// </remarks>
        public int Wants { get; private set; }
    }
}
