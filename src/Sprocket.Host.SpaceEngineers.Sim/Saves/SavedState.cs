using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using ProtoBuf;
using Sprocket.Sim.Machine;

namespace Sprocket.Sim.Saves;

/// <summary>What a saved world remembers of Sprocket's runtime, read out of its sector and written as text.</summary>
/// <remarks>
/// The state is binary in the save, by choice, and this is how a person reads it. Every remembered member is written by
/// its own name, found by its <see cref="ProtoMemberAttribute" />, so a member added to the state is written here with
/// nothing changed.
/// </remarks>
public static class SavedState
{
    /// <summary>The sector file a save keeps its entities in, as XML.</summary>
    const string Sector = "SANDBOX_0_0_0_.sbs";

    /// <summary>The state of the save at <paramref name="path" />, a save's folder or its sector file, as text.</summary>
    public static string Render(string path)
    {
        var sector = Directory.Exists(path) ? Path.Combine(path, Sector) : path;
        if (!File.Exists(sector))
            return "No sector file at " + sector + "\n";

        var said = new StringBuilder();
        said.Append("Read from ").Append(sector).Append('\n');
        var binary = sector + "B5";
        if (File.Exists(binary) && File.GetLastWriteTimeUtc(binary) > File.GetLastWriteTimeUtc(sector))
            said.Append("The binary beside it is newer, and the game reads that one: this may be behind what it loads.\n");

        var carried = Carried(sector);
        if (carried == null)
            return said.Append("The world has no carrier: Sprocket has saved nothing in it.\n").ToString();

        var files = new CarrierFiles();
        var trouble = files.Unpack(carried);
        if (trouble.Length > 0)
            return said.Append("The carrier's files will not read: ").Append(trouble).Append('\n').ToString();

        if (!files.Has(Memories.File))
            return said.Append("The carrier holds no state.\n").ToString();

        var memory = new ProtoBinary().Unpack<WorldMemory>(files.Read(Memories.File));
        Write(said, memory, 0);
        return said.ToString();
    }

    /// <summary>The text the carrier keeps under its key in <paramref name="sector" />, or null where the world has none.</summary>
    static string? Carried(string sector)
    {
        var key = Carrier.Key.ToString("D");
        var document = XDocument.Load(sector);
        var item = document.Descendants()
            .Where(element => element.Name.LocalName == "item")
            .FirstOrDefault(element => string.Equals((string?)element.Elements().FirstOrDefault(e => e.Name.LocalName == "Key"), key,
                StringComparison.OrdinalIgnoreCase));

        return (string?)item?.Elements().FirstOrDefault(element => element.Name.LocalName == "Value");
    }

    /// <summary>Writes every remembered member of <paramref name="value" />, by name, at <paramref name="depth" />.</summary>
    static void Write(StringBuilder said, object value, int depth)
    {
        var members = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => (property, tag: property.GetCustomAttribute<ProtoMemberAttribute>()))
            .Where(member => member.tag != null)
            .OrderBy(member => member.tag!.Tag);

        foreach (var (property, _) in members)
        {
            var member = property.GetValue(value);
            said.Append(' ', depth * 2).Append(property.Name);
            if (member is IList list && !(member is string))
            {
                said.Append(" (").Append(list.Count).Append(")\n");
                for (var i = 0; i < list.Count; i++)
                    Item(said, list[i], i, depth + 1);
            }
            else if (member != null && member.GetType().GetCustomAttribute<ProtoContractAttribute>() != null)
            {
                said.Append('\n');
                Write(said, member, depth + 1);
            }
            else
            {
                said.Append(": ").Append(Said(member)).Append('\n');
            }
        }
    }

    /// <summary>Writes the item at <paramref name="index" /> of a list, at <paramref name="depth" />.</summary>
    static void Item(StringBuilder said, object? item, int index, int depth)
    {
        said.Append(' ', depth * 2).Append('[').Append(index).Append(']');
        if (item != null && item.GetType().GetCustomAttribute<ProtoContractAttribute>() != null)
        {
            said.Append('\n');
            Write(said, item, depth + 1);
            return;
        }

        said.Append(' ').Append(Said(item)).Append('\n');
    }

    static string Said(object? value) => value switch
    {
        null => "none",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };
}
