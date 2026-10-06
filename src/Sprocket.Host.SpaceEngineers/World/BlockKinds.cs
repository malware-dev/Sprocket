using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Game.ModAPI;

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
    public sealed class BlockKinds
    {
        /// <summary>The action that switches a block on.</summary>
        public const string SwitchOn = "on";

        /// <summary>The action that switches a block off.</summary>
        public const string SwitchOff = "off";

        /// <summary>What Sprocket's own members and moments are qualified by: <c>ext.close</c>.</summary>
        /// <remarks>Persisted in every program that uses one, so it cannot change without changing what those programs say.</remarks>
        public const string Own = "ext";

        const string GamePrefix = "MyObjectBuilder_";

        readonly Dictionary<string, EntityKind> _kinds = new Dictionary<string, EntityKind>();
        readonly Catalogue _catalogue;
        readonly BlockExtensions _extensions;

        /// <summary>The kinds of block, described from <paramref name="extensions" /> and what <paramref name="catalogue" /> says of each block's actions.</summary>
        public BlockKinds(Catalogue catalogue, BlockExtensions extensions)
        {
            _catalogue = catalogue;
            _extensions = extensions;
        }

        /// <summary>The kind of <paramref name="block" />, described the first time a block of that type is asked about and kept.</summary>
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
            kind.Evaluation = Evaluations.Of(block);

            // What this block is, before what every block is, so a door offers "open" above "switch on".
            IBlockExtension extension = _extensions.For(block);
            if (extension != null)
                extension.Describe(kind, block);

            Triggers(kind, block, _extensions.TriggersFor(block));

            kind.WithProperty(GridEntity.WorkingProperty, "working")
                .WithState("is-working", GridEntity.WorkingProperty, true, "is working")
                .WithState("is-not-working", GridEntity.WorkingProperty, false, "is not working");


            // Only a block with a switch gets the words for one. A cargo container has no on and off, and
            // offering them would be offering a sentence that cannot come true.
            if (block is IMyFunctionalBlock)
            {
                kind.WithProperty(GridEntity.OnProperty, "switched on")
                    .WithEvent("switched-on", GridEntity.OnProperty, Edge.Rising, "is switched on").Reading("is on", "is off").Announced()
                    .WithEvent("switched-off", GridEntity.OnProperty, Edge.Falling, "is switched off").Reading("is off", "is on").Announced()
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
            Meanings.Settle(kind, block);
            Hide(kind);

            _kinds.Add(id, kind);
            return kind;
        }

        /// <summary>Adds the block's triggers: as <paramref name="layout" /> says they run, or every slot of every toolbar it has, by where it is.</summary>
        /// <remarks>
        /// A trigger is a toolbar slot, whatever block it is on, so a block nothing knows about offers its slots all the same.
        /// Asked once per block type, which has the same toolbars whichever block is asked.
        /// </remarks>
        static void Triggers(EntityKind kind, IMyTerminalBlock block, ITriggers layout)
        {
            if (layout != null)
            {
                layout.Describe(kind, block);
                return;
            }

            var toolbars = 0;
            while (Toolbars.Of(block, toolbars + 1) != null)
                toolbars++;

            for (var t = 1; t <= toolbars; t++)
            {
                IMyToolbar toolbar = Toolbars.Of(block, t);
                for (var p = 1; p <= toolbar.PageCount; p++)
                {
                    for (var s = 1; s <= toolbar.SlotCount; s++)
                    {
                        var at = new ToolbarSlot(t, p, s);
                        kind.WithTrigger(at, Placed(at, toolbars, toolbar.PageCount));
                    }
                }
            }
        }

        /// <summary>A trigger named by where its slot is, saying no more of the place than the block has to tell apart.</summary>
        static string Placed(ToolbarSlot at, int toolbars, int pages)
        {
            if (toolbars > 1)
                return "toolbar " + at.Toolbar + ", page " + at.Page + ", slot " + at.Slot + " runs";

            return pages > 1 ? "toolbar page " + at.Page + ", slot " + at.Slot + " runs" : "toolbar slot " + at.Slot + " runs";
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
                if (string.IsNullOrEmpty(offer.Id) || kind.FindAction(offer.Id, Form.Action) != null)
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                Act(kind, offer, offer.Id, said);
                Last(kind).Unaltered = true;

                string word;
                if (kind.Renames(offer.Id, false, out word))
                    Last(kind).Rename(word);
            }
        }

        /// <summary>One of the block's own actions, under the given id and words, performing the game's.</summary>
        void Act(EntityKind kind, Offer offer, string id, string said)
        {
            if (offer.Wants > 0)
            {
                var described = Described(kind, offer);
                kind.WithAction(id, said, offer.Id, offer.Wants, described, Takes(offer, described));
            }
            else
            {
                kind.WithAction(id, said, null, offer.Id);
            }
        }

        /// <summary>
        /// What type each value of an action is: what the action declares, corrected by what has been
        /// said about it.
        /// </summary>
        static IList<Primitive> Takes(Offer offer, Description described)
        {
            var types = new Primitive[offer.Wants];
            for (var i = 0; offer.Declares != null && i < types.Length && i < offer.Declares.Count; i++)
                types[i] = offer.Declares[i];

            if (described != null)
                described.Correct(types);

            return types;
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

                if (string.IsNullOrEmpty(offer.Id) || kind.FindAction(offer.Id, Form.Value) != null)
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                string word;
                var renamed = kind.Renames(offer.Id, true, out word);

                kind.WithValue(offer.Id, said, offer.Id, Asks(offer, renamed ? word : said), offer.Sets);
                Last(kind).Unaltered = true;
                Unread(Last(kind), offer);

                if (renamed)
                    Last(kind).Rename(word);
            }
        }

        /// <summary>
        /// Keeps a value out of reach where it is one of a listed few and the list could not be read.
        /// </summary>
        /// <remarks>
        /// Offered as its number it would be a box somebody types a number into with nothing saying
        /// what the number means, and a wrong one declined in silence when the step runs. Pending
        /// rather than unsupported, because a list the game will not hand over can often be found
        /// another way - a camera list is the cameras on the ship - and nothing here knows which.
        /// </remarks>
        static void Unread(EntityAction one, Offer offer)
        {
            if (offer.Unreadable)
                one.Withhold(Readiness.Pending, Holdback.Unread);
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

            // What is typed is held to the kind the block says the value is, whatever its bounds look
            // like: a real number takes a fraction and a whole one refuses it.
            var between = Field.Between(said, offer.Least, offer.Most);
            between.Entry = EntityAction.Entered(offer.Sets);
            return between;
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
        /// what the extension's entry does - a door's open is the game's <c>Open_On</c> said better - and
        /// where the kind names it outright. Runs once the list is complete, because the entry that stands
        /// in and the one it stands in for are added at different times.
        /// </remarks>
        static void Hide(EntityKind kind)
        {
            var actions = kind.Actions;

            // Values first, because whether a value is listed decides whether the actions the game
            // made out of it are.
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.Sets && (Turned(actions, one) || kind.Hides(one.GamePropertyId)))
                    one.Hide();
            }

            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (!one.Sets && (StoodIn(actions, one) || kind.Hides(one.GameActionId)))
                    one.Hide();
            }
        }

        static EntityAction Last(EntityKind kind)
        {
            return kind.Actions[kind.Actions.Count - 1];
        }

        /// <summary>Whether something else offered says what this one of the block's own actions says.</summary>
        /// <remarks>
        /// One of Sprocket's own entries that performs it, or a value the game made it out of. A
        /// switch gets a pair that turns it each way and an act that flips it, and a slider a step up,
        /// a step down and sometimes a reset; the value says what the pair and the steps say. The act
        /// that flips a switch and the reset stay, being the two things a value cannot say: whichever
        /// way it is not, and whatever the block starts at.
        /// <para>
        /// Decided by the ids alone. A value that is not itself listed stands in for nothing, so a
        /// block that keeps a value out of the list keeps the actions made out of it.
        /// </para>
        /// </remarks>
        static bool StoodIn(IList<EntityAction> actions, EntityAction one)
        {
            if (!one.Unaltered)
                return false;

            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction other = actions[i];
                if (ReferenceEquals(other, one))
                    continue;

                if (!other.Unaltered && other.GameActionId == one.Id)
                    return true;

                if (other.Sets && !other.Hidden && Generated(one.GameActionId, other.GamePropertyId))
                    return true;
            }

            return false;
        }

        /// <summary>Whether Sprocket's own entries turn this switch each way.</summary>
        /// <remarks>
        /// Then the value is the third way of saying the same thing: a vent that reads pressurize and
        /// depressurize has no use for a Depressurize to be told true.
        /// </remarks>
        static bool Turned(IList<EntityAction> actions, EntityAction value)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction other = actions[i];
                if (!other.Unaltered && Turns(other.GameActionId, value.Id))
                    return true;
            }

            return false;
        }

        /// <summary>Whether the game made the action out of that value, and the value says it too.</summary>
        static bool Generated(string gameActionId, string propertyId)
        {
            if (gameActionId == null || propertyId == null)
                return false;

            return Turns(gameActionId, propertyId)
                || Steps(gameActionId, "Increase", propertyId)
                || Steps(gameActionId, "Decrease", propertyId);
        }

        /// <summary>Whether the action is the value's id with that in front, as a slider's steps are.</summary>
        /// <remarks>
        /// Only what the game's own controls make, which follows the value's id exactly. A setter
        /// somebody wrote by hand beside a value is a different thing, and is said per block - see
        /// <c>Meanings</c>.
        /// </remarks>
        static bool Steps(string gameActionId, string step, string propertyId)
        {
            return gameActionId.Length == step.Length + propertyId.Length
                && string.CompareOrdinal(gameActionId, 0, step, 0, step.Length) == 0
                && string.CompareOrdinal(gameActionId, step.Length, propertyId, 0, propertyId.Length) == 0;
        }

        /// <summary>Whether the action is one half of the pair the game makes of that switch.</summary>
        /// <remarks>
        /// The game's convention, and the only thing tying the two together: a switch called
        /// <c>X</c> is turned by <c>X_On</c> and <c>X_Off</c>.
        /// </remarks>
        static bool Turns(string gameActionId, string propertyId)
        {
            if (gameActionId == null || propertyId == null)
                return false;

            int over = gameActionId.Length - propertyId.Length;
            if (over != 3 && over != 4)
                return false;

            if (string.CompareOrdinal(gameActionId, 0, propertyId, 0, propertyId.Length) != 0)
                return false;

            return over == 3
                ? string.CompareOrdinal(gameActionId, propertyId.Length, "_On", 0, 3) == 0
                : string.CompareOrdinal(gameActionId, propertyId.Length, "_Off", 0, 4) == 0;
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
        public static string Sort(string definition)
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
        public static string Definition(string sort)
        {
            if (string.IsNullOrEmpty(sort) || sort.IndexOf('/') < 0)
                return sort ?? "";

            return sort.StartsWith(GamePrefix, StringComparison.Ordinal) ? sort : GamePrefix + sort;
        }
    }
}
