using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// Everything anybody has said about the parameters of the game's own actions.
    /// </summary>
    /// <remarks>
    /// Filled from the files the loaded mods ship, in the order the game loads them, so the last
    /// word wins - the rule the mods themselves follow and the one a player already knows from the
    /// order of their list. This mod's own descriptions go in first and are therefore the ones
    /// anybody may override.
    /// <para>
    /// Read once, as a world comes up. Nothing here is asked on a tick: what it answers is used
    /// while a kind is built, which happens once per block type.
    /// </para>
    /// </remarks>
    public sealed class Catalogue
    {
        readonly Dictionary<string, Description> _byBlock = new Dictionary<string, Description>();
        readonly Dictionary<string, Description> _byAction = new Dictionary<string, Description>();

        /// <summary>Takes in what one file says, overriding anything said before it.</summary>
        public void Add(Descriptions said, string mod)
        {
            if (said == null || said.Actions == null)
                return;

            for (var i = 0; i < said.Actions.Count; i++)
                Add(said.Actions[i], mod);
        }

        /// <summary>
        /// Takes in one action's description.
        /// </summary>
        /// <remarks>
        /// Kept under the block where it names one, and never under the bare id as well, so a
        /// description of one light's colour setter cannot answer for every block in the game that
        /// happens to call something <c>SetColor</c>.
        /// </remarks>
        public void Add(Description description, string mod)
        {
            if (description == null || string.IsNullOrEmpty(description.Id))
            {
                Told.Say(mod, "an action was described with no id, so nothing can be done with it");
                return;
            }

            if (string.IsNullOrEmpty(description.Block))
            {
                _byAction[description.Id] = description;
                return;
            }

            _byBlock[Key(description.Block, description.Id)] = description;
        }

        /// <summary>
        /// What has been said about this action on this block, or null.
        /// </summary>
        /// <remarks>
        /// The exact claim first. A description naming the block is somebody who knew which block
        /// they meant; one naming only the action is saying "wherever this turns up", which is the
        /// weaker thing to say and loses to the stronger one.
        /// </remarks>
        public Description For(string blockId, string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return null;

            Description found;
            if (!string.IsNullOrEmpty(blockId)
                && _byBlock.TryGetValue(Key(blockId, actionId), out found))
            {
                return found;
            }

            return _byAction.TryGetValue(actionId, out found) ? found : null;
        }

        /// <summary>
        /// The same, refused and reported where it does not account for every parameter.
        /// </summary>
        /// <remarks>
        /// A description that covers four parameters of a three-parameter action is one that has
        /// fallen out of step with the block it is about - the action changed, or it was written
        /// against a different block. Using half of it would put names against the wrong values,
        /// which is worse than no names at all, so it is dropped and said out loud.
        /// </remarks>
        public Description Fitting(string blockId, string actionId, int parameters)
        {
            Description found = For(blockId, actionId);
            if (found == null || found.Covers == parameters)
                return found;

            Told.Say(actionId + " on " + blockId + " takes " + parameters
                + " parameters and is described with " + found.Covers
                + ", so the description is being ignored");

            return null;
        }

        static string Key(string blockId, string actionId)
        {
            return blockId + "/" + actionId;
        }
    }
}
