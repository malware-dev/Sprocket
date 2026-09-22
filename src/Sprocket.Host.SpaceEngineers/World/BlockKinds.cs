using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Layout;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One <see cref="EntityKind" /> per block definition, built the first time one of that block is seen.
    /// </summary>
    /// <remarks>
    /// Per definition rather than one kind for everything, because the ship list groups by kind and a list
    /// where every row says "Block" is not a list anybody can use. It is also what makes asking the block
    /// what it can do affordable: a definition declares the same actions whichever one of it is asked, so
    /// the question is put once however many of them are on the ship.
    /// </remarks>
    sealed class BlockKinds
    {
        internal const string SwitchOn = "on";
        internal const string SwitchOff = "off";

        /// <summary>What the game puts in front of every type id, and a program never writes.</summary>
        const string GamePrefix = "MyObjectBuilder_";

        readonly Dictionary<string, EntityKind> _kinds = new Dictionary<string, EntityKind>();
        readonly Catalogue _catalogue;

        public BlockKinds(Catalogue catalogue)
        {
            _catalogue = catalogue;
        }

        public EntityKind For(IMyTerminalBlock block)
        {
            string id = block.BlockDefinition.TypeIdString + "/" + block.BlockDefinition.SubtypeName;

            EntityKind kind;
            if (_kinds.TryGetValue(id, out kind))
                return kind;

            string name = string.IsNullOrEmpty(block.DefinitionDisplayNameText)
                ? block.BlockDefinition.SubtypeName
                : block.DefinitionDisplayNameText;

            // The block's own definition id doubles as its icon: the screen-space sprite API resolves
            // one straight to that block's picture, so nothing has to be declared, extracted or shipped.
            // It is the same string as the kind id, which is not a coincidence - both are "which block
            // type is this".
            kind = new EntityKind(id, name).WithIcon(id);

            // What this block is, before what every block is, so a door offers "open" above "switch on".
            IBlockExtension extension = BlockExtensions.For(block);
            if (extension != null)
                extension.Describe(kind, block);

            kind.WithProperty(GridEntity.WorkingProperty, "working")
                .WithState("is-working", GridEntity.WorkingProperty, true, "is working")
                .WithState("is-not-working", GridEntity.WorkingProperty, false, "is not working");


            // Only a block with a switch gets the words for one. A cargo container has no on and off, and
            // offering them would be offering a sentence that cannot come true.
            if (block is IMyFunctionalBlock)
            {
                kind.WithProperty(GridEntity.OnProperty, "switched on")
                    .WithEvent("switched-on", GridEntity.OnProperty, Edge.Rising, "is switched on")
                    .WithEvent("switched-off", GridEntity.OnProperty, Edge.Falling, "is switched off")
                    .WithState("is-on", GridEntity.OnProperty, true, "is on")
                    .WithState("is-off", GridEntity.OnProperty, false, "is off")
                    .WithAction(SwitchOn, "switch on", "is-on", "OnOff_On")
                    .WithAction(SwitchOff, "switch off", "is-off", "OnOff_Off");
            }

            // And then everything the block says it can do, which is most of what Sprocket can offer
            // about most blocks. Last, so the words chosen for a sentence come first where there are
            // any: a door reads "open" above "Open/Closed".
            Rest(kind, block);

            // And last of all the values it carries, because an act reads better than telling a
            // value what to be wherever there is one that does the same thing.
            Values(kind, block);
            Hide(kind);

            _kinds.Add(id, kind);
            return kind;
        }

        /// <summary>
        /// Adds everything the block declares and nothing has said better.
        /// </summary>
        /// <remarks>
        /// <b>The floor under the whole vocabulary.</b> Without it Sprocket can tell a piston nothing,
        /// because nobody has written a piston extension - and it can tell a modded block nothing
        /// ever, because nobody can write an extension for a block that does not exist yet. Asking
        /// the block instead means every block in the game works, and so does every block anybody
        /// adds to it afterwards.
        /// <para>
        /// The words are the game's own, which are written for a button in the terminal rather than
        /// for the middle of a sentence. They are what the player has already read, which is worth
        /// more than a guess at a better phrasing - and where a better one exists, an extension has
        /// said it, and this one is still added but left out of the list somebody chooses from.
        /// </para>
        /// <para>
        /// No state is claimed: an action here says nothing about what it is trying to bring about,
        /// so nothing generic can be waited for or interlocked. That is a real limit and not an
        /// oversight - see the extensions, which are where that knowledge lives.
        /// </para>
        /// </remarks>
        void Rest(EntityKind kind, IMyTerminalBlock block)
        {
            _offered.Clear();
            Toolbars.Offered(block, _offered);

            for (var i = 0; i < _offered.Count; i++)
            {
                Offer offer = _offered[i];
                if (string.IsNullOrEmpty(offer.Id) || kind.FindAction(offer.Id, false) != null)
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                string id;
                string named;
                if (kind.Promoted(offer.Id, false, out id, out named))
                    Act(kind, offer, id, named);

                Act(kind, offer, offer.Id, said);
                Last(kind).Unaltered = true;
            }
        }

        /// <summary>One of the block's own actions, under the given id and words, performing the game's.</summary>
        void Act(EntityKind kind, Offer offer, string id, string said)
        {
            if (offer.Wants > 0)
                kind.WithAction(id, said, offer.Id, offer.Wants, Described(kind, offer));
            else
                kind.WithAction(id, said, null, offer.Id);
        }

        /// <summary>
        /// Adds every value the block carries that nothing else already covers.
        /// </summary>
        /// <remarks>
        /// The other half of the floor. Actions are the small hand-picked half of what a block can
        /// do; the values are the large half, and they describe themselves - a name in the player's
        /// own language, a kind, and the bounds the block itself will accept.
        /// <para>
        /// Not held to the rule that hides a bare toggle. That rule is about an <em>action</em> -
        /// "toggle it" settles on a state whoever stored it could not see - and the explicit form
        /// it pushes people towards is this one. A switch is offered as a value it can be set to.
        /// </para>
        /// <para>
        /// A value is passed over only where another value already has that id, since a program
        /// could not tell the two apart.
        /// </para>
        /// </remarks>
        void Values(EntityKind kind, IMyTerminalBlock block)
        {
            _offered.Clear();
            Toolbars.Settable(block, _offered);

            for (var i = 0; i < _offered.Count; i++)
            {
                Offer offer = _offered[i];

                if (string.IsNullOrEmpty(offer.Id) || kind.FindAction(offer.Id, true) != null)
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                string id;
                string named;
                if (kind.Promoted(offer.Id, true, out id, out named))
                    kind.WithValue(id, named, offer.Id, Asks(offer, named));

                kind.WithValue(offer.Id, said, offer.Id, Asks(offer, said));
                Last(kind).Unaltered = true;
            }
        }

        /// <summary>
        /// What the form asks for one of a block's values, from what the block said about it.
        /// </summary>
        /// <remarks>
        /// The kind matters here and nowhere else. A switch takes two words and a slider takes a
        /// number between two others, and a form told neither accepts whatever is typed - which the
        /// block then refuses when the step runs, silently, because a value of the wrong kind is
        /// simply not written.
        /// </remarks>
        static Field Asks(Offer offer, string said)
        {
            // What the block says it may be, where it says: a charge mode is Auto, Recharge or
            // Discharge and the block is told a number, so the words and the values are two lists.
            if (offer.Choices != null && offer.Choices.Count > 0)
                return Among(said, offer.Choices);

            if (offer.Sets == ValueKind.Flag)
            {
                // The same thing with two of them, and the one case where each word is its own
                // value: a flag is written on or off, which is what the block is told.
                Field either = Field.OneOf(said, Literal.Of(true).Said, Literal.Of(false).Said);
                either.Edits = Editor.Chosen;
                return either;
            }

            // A colour says so itself, which nothing else here does. Where three parameters of an
            // action are a colour only because somebody wrote that down, a value declared as one is
            // one, so it gets the same control without anybody having to describe it.
            if (offer.Sets == ValueKind.Color)
                return Field.Holding(said, Editor.Colour, 1);

            return Field.Between(said, offer.Least, offer.Most);
        }

        /// <summary>One field over everything a value says it may be, kept in the order it said them.</summary>
        static Field Among(string said, IList<Choice> choices)
        {
            var words = new string[choices.Count];
            var means = new string[choices.Count];

            for (var i = 0; i < choices.Count; i++)
            {
                words[i] = choices[i].Word;
                means[i] = choices[i].Means;
            }

            return Field.Among(said, words, means);
        }

        readonly List<Offer> _offered = new List<Offer>();

        /// <summary>
        /// What anybody has said this action's parameters are, or null.
        /// </summary>
        /// <remarks>
        /// Asked with the kind's own id, which is the block's definition, so a description naming
        /// one block answers for that block and no other - and anything claimed about the bare
        /// action id is the fallback. Asked with the count as well, because a description that does
        /// not account for every parameter is one that has fallen out of step with the block.
        /// </remarks>
        Description Described(EntityKind kind, Offer offer)
        {
            return _catalogue == null ? null : _catalogue.Fitting(kind.Id, offer.Id, offer.Wants);
        }

        /// <summary>Marks what is left out of the list somebody chooses from.</summary>
        /// <remarks>
        /// An entry is left out where an extension stands in for it, naming it as the game's own form of
        /// what the extension's entry does: a door's open is the game's <c>Open_On</c> said better.
        /// Nothing else is. Runs once the list is complete, because the entry that stands in and the one
        /// it stands in for are added at different times.
        /// </remarks>
        static void Hide(EntityKind kind)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                if (StoodIn(actions, actions[i]))
                    actions[i].Hide();
            }
        }

        static EntityAction Last(EntityKind kind)
        {
            return kind.Actions[kind.Actions.Count - 1];
        }

        /// <summary>Whether another entry names this one as the game's own form of what it does.</summary>
        static bool StoodIn(IList<EntityAction> actions, EntityAction one)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction other = actions[i];
                if (ReferenceEquals(other, one))
                    continue;

                if (one.Sets ? other.GamePropertyId == one.Id : other.GameActionId == one.Id || other.Replaces == one.Id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The sort of block a program writes: a definition id without the game's prefix.
        /// </summary>
        /// <remarks>
        /// <b>For a script and nothing else.</b> Everything in memory holds the definition id whole,
        /// because that is what the game's own names are and what anything asking the game about a
        /// block has to hand it. The prefix is the game's noise, it says nothing to a person, and the
        /// language leaves it out - the same spelling a part's <c>expects</c> has always used.
        /// </remarks>
        internal static string Sort(string definition)
        {
            if (definition == null)
                return "";

            return definition.StartsWith(GamePrefix, StringComparison.Ordinal)
                ? definition.Substring(GamePrefix.Length)
                : definition;
        }

        /// <summary>
        /// The definition id a sort means, which is <see cref="Sort" /> read backwards.
        /// </summary>
        /// <remarks>
        /// A definition id is a type and a subtype, so a sort with no <c>/</c> in it is not one: it is
        /// a kind some other world invented, and it is left exactly as it was found. One that already
        /// carries the prefix is left alone too, so a program saved with it still finds its blocks.
        /// </remarks>
        internal static string Definition(string sort)
        {
            if (string.IsNullOrEmpty(sort) || sort.IndexOf('/') < 0)
                return sort ?? "";

            return sort.StartsWith(GamePrefix, StringComparison.Ordinal) ? sort : GamePrefix + sort;
        }
    }
}
