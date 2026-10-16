using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// How the construct's own wiring reads: what presses it, and what it then does.
    /// </summary>
    /// <remarks>
    /// The same two halves a sequence's row has, so a list of Sprocket's triggers and a list of the
    /// game's own read as one kind of thing. What differs is where the words come from: the game
    /// names its action, and Sprocket translates it back into the word it would have used itself
    /// where it knows one.
    /// </remarks>
    public static class VanillaText
    {
        /// <summary>What presses it: the panel, and which of its buttons.</summary>
        /// <remarks>
        /// Numbered from one, the way the terminal numbers them, because the slot's own index counts
        /// from zero and nobody reading a panel thinks of a button nought.
        /// </remarks>
        public static string Pressed(GridWorld world, Vanilla.Entry entry)
        {
            GridEntity panel = world?.FindEntity(entry.PanelId);
            var name = panel == null ? "a block that is gone" : panel.Name;

            return $"When {name} button {(entry.Slot + 1)} is pressed";
        }

        /// <summary>
        /// What it does: Sprocket's own word for the action where it has one, and the game's otherwise.
        /// </summary>
        /// <remarks>
        /// A block Sprocket understands gives back "open the inner door", which reads like everything
        /// else on the screen. Anything else is said in the game's own words rather than guessed at,
        /// because a wrong translation of somebody's wiring is worse than an unfamiliar one.
        /// </remarks>
        public static string Does(GridWorld world, Vanilla.Entry entry)
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
