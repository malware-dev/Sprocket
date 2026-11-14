using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;

namespace Sprocket.Host.SpaceEngineers.Framework.Text
{
    /// <summary>
    /// How the construct's own wiring reads: what presses it, and what it then does.
    /// </summary>
    public static class VanillaText
    {
        /// <summary>What presses it: the panel, and which of its buttons, numbered from one as the terminal numbers them.</summary>
        public static string Pressed(GridWorld world, VanillaWiring.Entry entry)
        {
            GridEntity panel = world?.FindEntity(entry.PanelId);
            var name = panel == null ? "a block that is gone" : panel.Name;

            return $"When {name} button {(entry.Slot + 1)} is pressed";
        }

        /// <summary>
        /// What it does: Sprocket's own word for the action where it has one, and the game's otherwise.
        /// </summary>
        public static string Does(GridWorld world, VanillaWiring.Entry entry)
        {
            if (entry.GroupName.Length > 0)
                return $"{entry.ActionId} {entry.GroupName}";

            GridEntity acts = world?.FindEntity(entry.BlockId);
            if (acts == null)
                return entry.ActionId;

            var actions = acts.Kind.Actions;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].GameActionId == entry.ActionId)
                    return $"{actions[i].Text} {acts.Name}";
            }

            return $"{entry.ActionId} {acts.Name}";
        }
    }
}
