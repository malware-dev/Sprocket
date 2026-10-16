using System.Collections.Generic;
using Sandbox.Definitions;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The game's items as a step or a test names one: the item's type and subtype, such as <c>Ingot/Iron</c>.</summary>
    /// <remarks>
    /// The spelling the language's own examples use, and the definition id without the game's prefix, as
    /// <see cref="BlockKinds.Sort" /> spells a block.
    /// </remarks>
    public static class Items
    {
        const string GamePrefix = "MyObjectBuilder_";

        /// <summary>A field choosing one of the game's public items, named <paramref name="name" />, by the names the game shows them by.</summary>
        /// <remarks>
        /// Read from the definitions the game has loaded, which is every item a world can hold, mods' included. Where none
        /// are loaded - the test harness - a box that takes any item written out.
        /// </remarks>
        public static Field Asked(string name)
        {
            var words = new List<string>();
            var means = new List<string>();

            var definitions = MyDefinitionManager.Static;
            if (definitions != null)
            {
                foreach (var item in definitions.GetPhysicalItemDefinitions())
                {
                    if (!item.Public)
                        continue;

                    words.Add(item.DisplayNameText);
                    means.Add(Said(item.Id));
                }
            }

            return Choosing(name, words, means);
        }

        /// <summary>A field choosing among <paramref name="words" />, each standing for the item in <paramref name="means" /> beside it; a box for any item where there are none.</summary>
        public static Field Choosing(string name, IList<string> words, IList<string> means)
        {
            var field = words.Count == 0 ? Field.Called(name) : Field.Among(name, words, means);
            field.Entry = Entry.Text;
            field.Needed = true;
            return field;
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
