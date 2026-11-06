using System.Xml.Linq;
using Mal.SeScenario.Grids;
using Mal.SeScenario.Worlds;

namespace Sprocket.GalleryWorld;

/// <summary>
///     Writes Scenario - Sprocket - Gallery from an untouched empty world the game saved: one block of every terminal type
///     the game defines, on a large grid and a small one, so a developer can check what the automation offers for each.
/// </summary>
public static class Program
{
    const int RowCells = 40;

    /// <summary>
    ///     Reads the template world in the first argument and writes the gallery into the saves folder in the second;
    ///     --replace writes over the one generated before.
    /// </summary>
    public static int Main(string[] args)
    {
        var replace = args.Contains("--replace");
        var paths = args.Where(a => a != "--replace").ToArray();
        if (paths.Length != 2)
        {
            Console.Error.WriteLine("Usage: Sprocket.GalleryWorld <template world folder> <saves folder> [--replace]");
            return 1;
        }

        var content = GameContent.Find();
        if (content == null)
        {
            Console.Error.WriteLine("The game's Content folder was not found; set " + GameContent.Variable + " to it.");
            return 1;
        }

        var definitions = BlockDefinitions.FromContent(content);
        var name = World.ScenarioName("Sprocket", "Gallery");
        var folder = Path.Combine(paths[1], name);
        var world = World.Load(paths[0]);
        world.Name = name;
        world.UseMods(
            Mod.Local("Sprocket Automation (Bleeding Edge)"),
            Mod.Workshop(3807241468, "Maltek Symbols"),
            Mod.Workshop(3807240295, "Maltek Mono"),
            Mod.Workshop(3814677776, "Maltek Sans"),
            Mod.Workshop(3814731076, "Space Engineers Font Styles"),
            Mod.Workshop(3822424591, "Door Auto-Close"));
        world.Set("GameMode", "Creative");
        world.Set("EnableEncounters", false);
        world.Set("CargoShipsEnabled", false);
        world.Set("EnableDrones", false);
        world.Set("EnvironmentHostility", "SAFE");
        world.Set("AutoSaveInMinutes", 0);

        var player = world.Player;
        var ids = world.NewIds();
        world.RemoveGrids();

        var listed = Terminal(content);
        var large = Gallery("Gallery Large", "Large", listed, definitions, ids, player);
        large.Position = new WorldPoint(0, 0, 0);
        var small = Gallery("Gallery Small", "Small", listed, definitions, ids, player);
        small.Position = new WorldPoint(0, 60, 0);

        world.AddGrid(large);
        world.AddGrid(small);
        world.PlaceCharacter(new WorldPoint(-10, 10, -10));
        world.Save(folder, replace);

        Console.WriteLine($"Wrote {folder}: {large.Blocks.Count} large and {small.Blocks.Count} small blocks.");
        return 0;
    }

    /// <summary>The type, subtype and cube size of every public block definition in the game's content.</summary>
    static List<(string Type, string Subtype, string Size)> Terminal(string content)
    {
        var found = new List<(string, string, string)>();
        foreach (var file in Directory.GetFiles(Path.Combine(content, "Data/CubeBlocks"), "*.sbc"))
        {
            foreach (var definition in XDocument.Load(file).Descendants("Definition"))
            {
                var id = definition.Element("Id");
                var size = (string?)definition.Element("CubeSize");
                if (id == null || size == null || (string?)definition.Element("Public") == "false")
                    continue;

                var type = ((string?)id.Element("TypeId") ?? "").Replace("MyObjectBuilder_", "");
                found.Add((type, (string?)id.Element("SubtypeId") ?? "", size));
            }
        }

        return found;
    }

    static Grid Gallery(string name, string size, List<(string Type, string Subtype, string Size)> listed, BlockDefinitions definitions,
        EntityIds ids, long owner)
    {
        var grid = new Grid(name, definitions, ids) { Size = size == "Large" ? GridSize.Large : GridSize.Small };
        var blocks = listed
            .Where(block => block.Size == size && definitions.Find(block.Type, block.Subtype)?.HasComputer == true)
            .GroupBy(block => block.Type)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.OrderBy(block => block.Subtype, StringComparer.Ordinal).First())
            .Select(block => new Block(block.Type, block.Subtype, block.Type) { Owner = owner })
            .ToList();

        grid.Lay(blocks, Cell.Zero, RowCells);
        return grid;
    }
}
