using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The game's items as a step or a test names one: the item's type and subtype, such as <c>Ingot/Iron</c>.</summary>
    /// <remarks>Spelled as the definition id without the game's prefix.</remarks>
    public static class ItemIds
    {
        const string GamePrefix = "MyObjectBuilder_";

        /// <summary>A field choosing one of the game's public items, named <paramref name="name" />, by the names the game shows them by, of those <paramref name="block" /> can hold where it is given.</summary>
        /// <remarks>Where no definitions are loaded, as in the test harness, a box that takes any item written out.</remarks>
        public static Field Asked(string name, IMyTerminalBlock block = null)
        {
            var words = new List<string>();
            var means = new List<string>();

            var definitions = MyDefinitionManager.Static;
            if (definitions != null)
            {
                var held = block == null ? null : Held(block);
                foreach (var item in definitions.GetPhysicalItemDefinitions())
                {
                    if (!item.Public || (held != null && !held.Contains(Said(item.Id))))
                        continue;

                    words.Add(item.DisplayNameText);
                    means.Add(Said(item.Id));
                }
            }

            
            
            
            
            
            
            
            return Choosing(name, words, means);
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
