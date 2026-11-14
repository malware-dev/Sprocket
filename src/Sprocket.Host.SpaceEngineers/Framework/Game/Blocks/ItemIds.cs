using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>The game's items as a step or a test names one: the item's type and subtype, such as <c>Ingot/Iron</c>.</summary>
    /// <remarks>Spelled as the definition id without the game's prefix.</remarks>
    public static class ItemIds
    {
        const string GamePrefix = "MyObjectBuilder_";

        // Static because the game has one list of items, and the harness puts its own in for the length of a test.
        static IItemCatalog _catalog = new GameItemCatalog();

        /// <summary>A field choosing one of the game's public items, named <paramref name="name" />, by the names the game shows them by, of those <paramref name="block" /> can hold where it is given.</summary>
        /// <remarks>Where the catalog lists no items, a box that takes any item written out.</remarks>
        public static Field Asked(string name, IMyTerminalBlock block = null)
        {
            var names = new List<string>();
            var ids = new List<string>();
            _catalog.Public(names, ids);

            var held = block == null || ids.Count == 0 ? null : Held(block);
            var words = new List<string>();
            var means = new List<string>();
            for (var i = 0; i < ids.Count; i++)
            {
                if (held != null && !held.Contains(ids[i]))
                    continue;

                words.Add(names[i]);
                means.Add(ids[i]);
            }

            return Choosing(name, words, means);
        }

        /// <summary>Puts something else behind every list of the game's items.</summary>
        /// <remarks>For the test harness; null puts the game back.</remarks>
        public static void Use(IItemCatalog catalog)
        {
            _catalog = catalog ?? new GameItemCatalog();
        }

        /// <summary>The items any inventory of <paramref name="block" /> can hold, as a step or a test names them.</summary>
        public static HashSet<string> Held(IMyTerminalBlock block)
        {
            var held = new HashSet<string>();
            var accepted = new List<MyItemType>();
            for (var i = 0; i < block.InventoryCount; i++)
            {
                accepted.Clear();
                block.GetInventory(i).GetAcceptedItems(accepted);
                foreach (var item in accepted)
                    held.Add(Said(item));
            }

            return held;
        }

        /// <summary>A field choosing among <paramref name="words" />, each standing for the item in <paramref name="means" /> beside it; a box for any item where there are none.</summary>
        public static Field Choosing(string name, IList<string> words, IList<string> means)
        {
            var field = words.Count == 0 ? Field.Called(name) : Field.Among(name, words, means);
            field.Entry = FieldInput.Text;
            field.Needed = true;
            field.Searched = true;
            return field;
        }

        /// <summary>Whether the item <paramref name="said" /> is counted by weight, as ore and ingots are, rather than by the piece.</summary>
        public static bool IsWeighed(string said)
        {
            return said.StartsWith("Ore/", System.StringComparison.Ordinal) || said.StartsWith("Ingot/", System.StringComparison.Ordinal);
        }

        /// <summary>How a step or a test names the item <paramref name="id" />.</summary>
        public static string Said(MyDefinitionId id)
        {
            return $"{Plain(id.TypeId.ToString())}/{id.SubtypeName}";
        }

        /// <summary>The item <paramref name="said" /> names, or false where it names none the game has.</summary>
        public static bool Read(string said, out MyDefinitionId id)
        {
            id = default(MyDefinitionId);
            if (string.IsNullOrEmpty(said))
                return false;

            var slash = said.IndexOf('/');
            if (slash <= 0 || slash == said.Length - 1)
                return false;

            return MyDefinitionId.TryParse(GamePrefix + said.Substring(0, slash), said.Substring(slash + 1), out id);
        }

        static string Plain(string type)
        {
            return type.StartsWith(GamePrefix, System.StringComparison.Ordinal) ? type.Substring(GamePrefix.Length) : type;
        }
    }
}
