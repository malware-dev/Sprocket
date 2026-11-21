using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// One <see cref="EntityKind" /> per block definition, built the first time one of that block is seen.
    /// </summary>
    public sealed class BlockKinds
    {
        /// <summary>What Sprocket's own members and moments are qualified by: <c>sprocket.close</c>.</summary>
        /// <remarks>Persisted in every program that uses one.</remarks>
        public const string Own = "sprocket";

        /// <summary>What the game's own members are qualified by, where a program writes one qualified: <c>game.Radius</c>.</summary>
        /// <remarks>Persisted in every program that writes one so.</remarks>
        public const string Game = "game";

        const string GamePrefix = "MyObjectBuilder_";

        readonly Dictionary<string, EntityKind> _kinds = new Dictionary<string, EntityKind>();
        readonly ParameterCatalog _catalogue = Catalogue();
        readonly Capabilities _capabilities = new Capabilities();

        /// <summary>
        /// The kinds of block in a world, described from Sprocket's capabilities and what Sprocket and every mod loaded say
        /// of each block's actions.
        /// </summary>
        /// <remarks>
        /// One for the world, which every construct in it shares: each mod's folder is read once, and each type of block
        /// described once, however many constructs there are.
        /// </remarks>
        public BlockKinds()
        {
        }

        /// <summary>What Sprocket and then every mod loaded say of the actions blocks have.</summary>
        static ParameterCatalog Catalogue()
        {
            // Sprocket's own descriptions go in first, so a mod may override them.
            var catalogue = new ParameterCatalog();
            catalogue.Add(KnownActions.Descriptions(), "");
            ModActions.Read(catalogue);
            return catalogue;
        }

        /// <summary>The kind of <paramref name="block" />, described the first time a block of that type is asked about and kept.</summary>
        public EntityKind For(IMyTerminalBlock block)
        {
            string id = $"{block.BlockDefinition.TypeIdString}/{block.BlockDefinition.SubtypeName}";

            EntityKind kind;
            if (_kinds.TryGetValue(id, out kind))
                return kind;

            string name = string.IsNullOrEmpty(block.DefinitionDisplayNameText)
                ? block.BlockDefinition.SubtypeName
                : block.DefinitionDisplayNameText;

            // The definition id is also the icon: the game's screen-space sprite API resolves it to that block's picture.
            kind = new EntityKind(id, name).WithIcon(id).WithFamily(Families.Of(block.BlockDefinition.TypeIdString));
            Lineage(block.BlockDefinition.TypeId, Sort(id), kind.Lineage);

            // What this block is, before what every block is, so a door offers "open" above "power".
            Describe(kind, block, _capabilities.Blocks);

            if (!Signalled(kind))
                Signals(kind, block);

            Describe(kind, block, _capabilities.Floor);

            Describe(kind, block, HostTypes.All);

            // After the capabilities, so the words chosen for a sentence come first: a door reads "open" above "Open/Closed".
            Rest(kind, block);

            // After the actions, so an act is listed above a value that does the same thing.
            Values(kind, block);
            MemberMeanings.Settle(kind, block);
            Hide(kind);
            Flips(kind);

            _kinds.Add(id, kind);
            return kind;
        }

        /// <summary>Has every one of <paramref name="capabilities" /> that claims <paramref name="block" /> describe it, in order.</summary>
        static void Describe(EntityKind kind, IMyTerminalBlock block, IList<Capability> capabilities)
        {
            for (var i = 0; i < capabilities.Count; i++)
            {
                if (!capabilities[i].Claims(block))
                    continue;

                kind.Known(capabilities[i]);
                capabilities[i].Describe(kind, block);
            }
        }

        /// <summary>Has every one of <paramref name="types" /> that <paramref name="block" /> has anything of describe its members, and keeps what the block has of it.</summary>
        static void Describe(EntityKind kind, IMyTerminalBlock block, IList<HostType> types)
        {
            var facets = new List<HostTypeFacet>();
            foreach (var type in types)
            {
                if (!type.Claims(block))
                    continue;

                facets.Clear();
                type.Offered(block, facets);
                kind.Offering(type, facets);
                type.Describe(kind, block);
            }
        }

        /// <summary>Whether something that knows the block has said what its toolbar slots signal.</summary>
        static bool Signalled(EntityKind kind)
        {
            for (var i = 0; i < kind.Events.Count; i++)
            {
                if (kind.Events[i].Answered)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds each toolbar the block has as one signal, its page and slot chosen beside it, as the terminal view alone lists
        /// it: nothing knows what the block runs its slots for.
        /// </summary>
        /// <remarks>Asked once per block type.</remarks>
        static void Signals(EntityKind kind, IMyTerminalBlock block)
        {
            var toolbars = 0;
            while (Toolbars.Of(block, toolbars + 1) != null)
                toolbars++;

            for (var t = 1; t <= toolbars; t++)
            {
                IMyToolbar toolbar = Toolbars.Of(block, t);
                var pages = toolbar.PageCount;
                var named = toolbars > 1 ? $"runs a slot of toolbar {t}" : "runs a toolbar slot";
                kind.WithPressed(t, named, pages, toolbar.SlotCount, at => Placed(at, toolbars, pages)).Unvouched();
            }
        }

        /// <summary>A toolbar slot the block runs, named by where it is, saying no more of the place than the block has to tell apart.</summary>
        static string Placed(ToolbarSlot at, int toolbars, int pages)
        {
            if (toolbars > 1)
                return $"runs toolbar {at.Toolbar}, page {at.Page}, slot {at.Slot}";

            return pages > 1 ? $"runs toolbar page {at.Page}, slot {at.Slot}" : $"runs toolbar slot {at.Slot}";
        }

        /// <summary>
        /// Adds everything the block declares and nothing has said better.
        /// </summary>
        void Rest(EntityKind kind, IMyTerminalBlock block)
        {
            _offered.Clear();
            Toolbars.Offered(block, _offered);

            foreach (Offer offer in _offered)
            {
                if (string.IsNullOrEmpty(offer.Id) || kind.FindAction(offer.Id, ReachKind.Action) != null || kind.Lacks(offer.Id))
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                Act(kind, offer, offer.Id, said);
                Last(kind).Unaltered = true;

                string word;
                if (kind.Renames(offer.Id, false, out word))
                    Last(kind).Rename(word);

                if (kind.Vets(offer.Id, false))
                    Last(kind).Vouch();
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
                kind.WithAction(id, said, offer.Id);
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
        void Values(EntityKind kind, IMyTerminalBlock block)
        {
            _offered.Clear();
            Toolbars.Settable(block, _offered);

            foreach (Offer offer in _offered)
            {

                if (string.IsNullOrEmpty(offer.Id) || kind.FindAction(offer.Id, ReachKind.Value) != null || kind.Lacks(offer.Id))
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                string word;
                var renamed = kind.Renames(offer.Id, true, out word);

                kind.WithValue(offer.Id, said, offer.Id, Asks(offer, renamed ? word : said), offer.Sets);
                Last(kind).Unaltered = true;
                Unread(Last(kind), offer);

                if (renamed)
                    Last(kind).Rename(word);

                string unit, most;
                double scale, step;
                if (kind.Measures(offer.Id, out unit, out scale, out step, out most))
                {
                    Last(kind).Measured(unit, scale, step);
                    if (most != null)
                        Last(kind).Capped(most);
                }

                if (kind.Vets(offer.Id, true))
                    Last(kind).Vouch();

                if (kind.States(offer.Id) && offer.Sets == ValueKind.Flag)
                    Last(kind).IsStatus = true;
            }
        }

        /// <summary>
        /// Keeps a value out of reach where it is one of a listed few and the list could not be read.
        /// </summary>
        static void Unread(EntityAction one, Offer offer)
        {
            if (offer.Unreadable)
                one.Withhold(Readiness.Pending, UnreadyReason.Unread);
        }

        /// <summary>
        /// What the form asks for one of a block's values, from what the block said about it.
        /// </summary>
        static Field Asks(Offer offer, string said)
        {
            // A charge mode is Auto, Recharge or Discharge and the block is told a number, so the words and the values are two lists.
            if (offer.Choices != null && offer.Choices.Count > 0)
                return Among(said, offer.Choices);

            if (offer.Sets == ValueKind.Flag)
            {
                // Each word is its own value: the block is told on or off.
                Field either = Field.OneOf(said, Literal.Of(true).Said, Literal.Of(false).Said);
                either.Edits = FieldEditor.Chosen;
                return either;
            }

            if (offer.Sets == ValueKind.Color)
                return Field.Holding(said, FieldEditor.Color, 1);

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
        Description Described(EntityKind kind, Offer offer)
        {
            return _catalogue?.Fitting(kind.Id, offer.Id, offer.Wants);
        }

        /// <summary>Marks what is left out of the list somebody chooses from.</summary>
        /// <remarks>Called once the kind's list is complete.</remarks>
        static void Hide(EntityKind kind)
        {
            var actions = kind.Actions;

            // Values first: whether a value is listed decides whether the actions the game made out of it are.
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.GamePropertyId != null && ((!one.Renamed && Turned(actions, one)) || kind.Hides(one.GamePropertyId)))
                    one.Hide();
            }

            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (!one.Sets && (StoodIn(actions, one) || kind.Hides(one.GameActionId)))
                    one.Hide();
            }
        }

        /// <summary>
        /// Gives each switch the organized view lists a third choice, flip, where the game's toggle for it is listed beside it,
        /// and takes that toggle out of the organized view.
        /// </summary>
        /// <remarks>Called after <see cref="Hide" />.</remarks>
        static void Flips(EntityKind kind)
        {
            var fields = new List<Field>();
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.Form != ReachKind.Value || !one.Sets || !one.Vetted || one.Hidden)
                    continue;

                var toggleId = one.GamePropertyId ?? kind.Flipper(one.Id);
                if (toggleId == null)
                    continue;

                fields.Clear();
                one.Fields(fields);
                if (fields.Count != 1 || !fields[0].IsOnOff)
                    continue;

                var toggle = kind.FindAction(toggleId, ReachKind.Action);
                if (toggle == null || toggle.Hidden || !toggle.Vetted)
                    continue;

                one.Flipped(toggleId);
                toggle.Hide();
            }
        }

        static EntityAction Last(EntityKind kind)
        {
            return kind.Actions[kind.Actions.Count - 1];
        }

        /// <summary>Whether something else offered says what this one of the block's own actions says.</summary>
        static bool StoodIn(IList<EntityAction> actions, EntityAction one)
        {
            if (!one.Unaltered || one.Renamed)
                return false;

            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction other = actions[i];
                if (ReferenceEquals(other, one))
                    continue;

                if (!other.Unaltered && other.GameActionId == one.Id)
                    return true;

                if (other.GamePropertyId != null && !other.Hidden && Generated(one.GameActionId, other.GamePropertyId))
                    return true;
            }

            return false;
        }

        /// <summary>Whether Sprocket's own entries turn this switch each way.</summary>
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
        static bool Steps(string gameActionId, string step, string propertyId)
        {
            return gameActionId.Length == step.Length + propertyId.Length
                && string.CompareOrdinal(gameActionId, 0, step, 0, step.Length) == 0
                && string.CompareOrdinal(gameActionId, step.Length, propertyId, 0, propertyId.Length) == 0;
        }

        /// <summary>Whether the action is one half of the pair the game makes of that switch.</summary>
        /// <remarks>The game's convention: a switch called <c>X</c> is turned by <c>X_On</c> and <c>X_Off</c>.</remarks>
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
        /// <remarks>For a script only: everything in memory holds the definition id whole.</remarks>
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
        /// <remarks>A sort with no <c>/</c> in it, or one that already carries the prefix, comes back as it was given.</remarks>
        public static string Definition(string sort)
        {
            if (string.IsNullOrEmpty(sort) || sort.IndexOf('/') < 0)
                return sort ?? "";

            return sort.StartsWith(GamePrefix, StringComparison.Ordinal) ? sort : GamePrefix + sort;
        }

        /// <summary>
        /// Whether <paramref name="name" />, written without its <c>MyObjectBuilder_</c> prefix, is one of the game's block types:
        /// a name in the root namespace, as a slot expecting it writes it, <c>Door</c>.
        /// </summary>
        /// <remarks>Asked by the game's register of its kinds of object, which it fills as it starts; a name it lacks is none.</remarks>
        public static bool IsGameType(string name)
        {
            MyObjectBuilderType type;
            if (string.IsNullOrEmpty(name) || name.IndexOf('/') >= 0 || !MyObjectBuilderType.TryParse(GamePrefix + name, out type))
                return false;

            Type at = type;
            var reflection = MyAPIGateway.Reflection;
            while (at != null && reflection != null)
            {
                if (at == typeof(MyObjectBuilder_CubeBlock))
                    return true;

                at = reflection.BaseTypeOf(at);
            }

            return reflection == null;
        }

        /// <summary>
        /// Adds to <paramref name="into" /> the block <paramref name="exact" />, written as a slot writes it, then its type
        /// <paramref name="type" /> and each type the game builds that one on, nearest first, short of the terminal block.
        /// </summary>
        /// <remarks>Called once for each kind, when it is first described.</remarks>
        static void Lineage(MyObjectBuilderType type, string exact, IList<string> into)
        {
            into.Add(exact);
            Type at = type;
            while (at != null && at != typeof(MyObjectBuilder_TerminalBlock))
            {
                into.Add(Sort(at.Name));
                at = MyAPIGateway.Reflection.BaseTypeOf(at);
            }
        }
    }
}
