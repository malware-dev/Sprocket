using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

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
        /// said it and this adds nothing.
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
                if (string.IsNullOrEmpty(offer.Id) || Said(kind, offer.Id))
                    continue;

                var said = string.IsNullOrEmpty(offer.Text) ? offer.Id : offer.Text;

                if (offer.Wants > 0)
                    kind.WithAction(offer.Id, said, offer.Id, offer.Wants, Described(kind, offer));
                else
                    kind.WithAction(offer.Id, said, null, offer.Id);
            }
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

        /// <summary>Whether something in this kind already covers the game's action by that name.</summary>
        /// <remarks>
        /// Either an action of Sprocket's own is written as that one, or both halves of it are: the
        /// game's convention is that a switch called <c>X</c> offers <c>X_On</c>, <c>X_Off</c> and a
        /// bare <c>X</c> that flips whichever way it is, and "open or close the door depending" is
        /// not a thing anybody writing a procedure means. So a door offers open and close, and the
        /// toggle they came from is not offered a third time.
        /// </remarks>
        static bool Said(EntityKind kind, string gameActionId)
        {
            return Written(kind, gameActionId)
                || (Written(kind, gameActionId + "_On") && Written(kind, gameActionId + "_Off"));
        }

        static bool Written(EntityKind kind, string gameActionId)
        {
            var actions = kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].GameActionId == gameActionId)
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
