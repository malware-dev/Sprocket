using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Everything anybody has said about the parameters of the game's own actions.
    /// </summary>
    /// <remarks>Read once, as a world comes up, and asked once per block type, never on a tick.</remarks>
    public sealed class ParameterCatalog
    {
        readonly Dictionary<string, Description> _byBlock = new Dictionary<string, Description>();
        readonly Dictionary<string, Description> _byAction = new Dictionary<string, Description>();

        /// <summary>Takes in what one file says, overriding anything said before it.</summary>
        public void Add(Descriptions said, string mod)
        {
            if (said == null || said.Actions == null)
                return;

            foreach (var action in said.Actions)
                Add(action, mod);
        }

        /// <summary>
        /// Takes in one action's description.
        /// </summary>
        public void Add(Description description, string mod)
        {
            if (description == null || string.IsNullOrEmpty(description.Id))
            {
                GameLog.Say(mod, "an action was described with no id, so nothing can be done with it");
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
        /// <remarks>A description naming the block wins over one naming only the action.</remarks>
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
        /// What has been said about this action on this block, or null where nothing has or it does not account for every
        /// parameter, which is reported.
        /// </summary>
        public Description Fitting(string blockId, string actionId, int parameters)
        {
            Description found = For(blockId, actionId);
            if (found == null || found.Covers == parameters)
                return found;

            GameLog.Say($"{actionId} on {blockId} takes {parameters} parameters and is described with {found.Covers}, so the description is being ignored");

            return null;
        }

        static string Key(string blockId, string actionId)
        {
            return $"{blockId}/{actionId}";
        }
    }
}
