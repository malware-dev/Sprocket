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
            Named = text;
            Wants = wants;
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

        public EntityAction Writes(string propertyId, Field field, string named)
        {
            GamePropertyId = propertyId;
            Named = named;
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

        public void Fields(IList<Field> into)
        {
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
