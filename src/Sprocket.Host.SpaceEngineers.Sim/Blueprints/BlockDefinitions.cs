using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using VRageMath;
using Lumel.Sim;

namespace Sprocket.Sim.Blueprints;

/// <summary>The game's block definitions, read from its content.</summary>
public sealed class BlockDefinitions
{
    const string Folder = "Data/CubeBlocks";

    readonly Dictionary<string, BlockDefinition> _found = new Dictionary<string, BlockDefinition>();

    /// <summary>The definitions in the <c>.sbc</c> files <paramref name="files" /> name.</summary>
    public BlockDefinitions(IEnumerable<string> files)
    {
        foreach (var file in files)
            Read(XDocument.Load(file));
    }

    /// <summary>How many definitions were read.</summary>
    public int Count => _found.Count;

    /// <summary>The game's own definitions, from the game or the harness's cache of its content.</summary>
    public static BlockDefinitions FromGame()
    {
        var content = GameContent.ContentFolder;
        var root = content ?? GameContent.CacheDirectory;
        var folder = Path.Combine(root, Folder);
        if (!Directory.Exists(folder))
            return new BlockDefinitions(new string[0]);

        var files = new List<string>();
        foreach (var file in Directory.GetFiles(folder, "*.sbc"))
            files.Add(content == null ? file : GameContent.Resolve(Path.Combine(Folder, Path.GetFileName(file))));

        return new BlockDefinitions(files);
    }

    /// <summary>The definition of the block <paramref name="type" /> and <paramref name="subtype" />, or null.</summary>
    public BlockDefinition? Find(string type, string subtype)
    {
        BlockDefinition? found;
        return _found.TryGetValue(Key(type, subtype), out found) ? found : null;
    }

    void Read(XDocument document)
    {
        foreach (var definition in document.Descendants("Definition"))
        {
            var id = definition.Element("Id");
            var size = definition.Element("Size");
            if (id == null || size == null)
                continue;

            var type = Plain((string)id.Element("TypeId") ?? "");
            var subtype = (string)id.Element("SubtypeId") ?? "";
            var mounts = new List<Mount>();
            foreach (var mount in definition.Descendants("MountPoint"))
            {
                Side side;
                if (Enum.TryParse((string)mount.Attribute("Side"), out side))
                    mounts.Add(new Mount(side, (string)mount.Attribute("Default") == "true"));
            }

            var classes = new List<string>();
            foreach (var made in definition.Element("BlueprintClasses")?.Elements("Class") ?? new XElement[0])
                classes.Add(made.Value.Trim());

            var turn = (string)definition.Element("RotationAngleStepDeg");
            var center = definition.Element("Center");
            _found[Key(type, subtype)] = new BlockDefinition(type, subtype, Cell(size),
                center == null ? Vector3I.Zero : Cell(center), mounts, (string)definition.Element("MaxStoredPower"), classes,
                turn == null ? null : int.Parse(turn, CultureInfo.InvariantCulture),
                (string)definition.Element("AllowScaling") == "true");
        }
    }

    static Vector3I Cell(XElement element)
    {
        return new Vector3I(Integer(element, "x"), Integer(element, "y"), Integer(element, "z"));
    }

    static int Integer(XElement element, string axis)
    {
        var said = (string)element.Attribute(axis);
        return said == null ? 0 : int.Parse(said, CultureInfo.InvariantCulture);
    }

    static string Key(string type, string subtype)
    {
        return Plain(type) + "/" + (subtype ?? "");
    }

    static string Plain(string type)
    {
        const string prefix = "MyObjectBuilder_";
        return type.StartsWith(prefix, StringComparison.Ordinal) ? type.Substring(prefix.Length) : type;
    }
}
