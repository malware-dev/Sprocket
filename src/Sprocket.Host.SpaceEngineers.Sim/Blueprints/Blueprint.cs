using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Scenarios;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Sim.Blueprints;

/// <summary>A deck written as a game blueprint, <c>bp.sbc</c>.</summary>
/// <remarks>Its shapes are copied from a save the game wrote.</remarks>
public static class Blueprint
{
    /// <summary>The file a blueprint is kept in.</summary>
    public const string File = "bp.sbc";

    /// <summary>The binary the game writes beside a blueprint, which wins over the text.</summary>
    public const string Binary = "bp.sbcB5";

    static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    /// <summary>The blueprint called <paramref name="name" /> holding <paramref name="deck" /> as one static grid.</summary>
    public static string Write(string name, Deck deck)
    {
        var blocks = new XElement("CubeBlocks");
        foreach (var cell in deck.Armor)
            blocks.Add(Armor(cell));

        foreach (var fitting in deck.Blocks)
            blocks.Add(Block(fitting, deck));

        var grid = new XElement("CubeGrid",
            new XElement("SubtypeName"),
            new XElement("EntityId", 1000000L),
            new XElement("PersistentFlags", "CastShadows InScene"),
            new XElement("PositionAndOrientation",
                Vector("Position", Vector3D.Zero),
                Vector("Forward", new Vector3D(0, 0, -1)),
                Vector("Up", new Vector3D(0, 1, 0)),
                new XElement("Orientation", new XElement("X", 0), new XElement("Y", 0), new XElement("Z", 0), new XElement("W", 1))),
            new XElement("GridSizeEnum", "Large"),
            blocks,
            new XElement("IsStatic", true),
            new XElement("DisplayName", name),
            new XElement("DestructibleBlocks", true));

        var document = new XDocument(new XDeclaration("1.0", null, null),
            new XElement("Definitions",
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd),
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi),
                new XElement("ShipBlueprints",
                    new XElement("ShipBlueprint",
                        new XAttribute(Xsi + "type", "MyObjectBuilder_ShipBlueprintDefinition"),
                        new XElement("Id", new XAttribute("Type", "MyObjectBuilder_ShipBlueprintDefinition"), new XAttribute("Subtype", name)),
                        new XElement("DisplayName", name),
                        new XElement("CubeGrids", grid),
                        new XElement("WorkshopId", 0),
                        new XElement("OwnerSteamId", 0),
                        new XElement("Points", 0)))));

        // Through bytes rather than a string, so the declaration says the UTF-8 the file is written in.
        var encoding = new UTF8Encoding(false);
        using (var bytes = new MemoryStream())
        {
            using (var writer = XmlWriter.Create(bytes, new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = encoding }))
                document.Save(writer);

            return encoding.GetString(bytes.ToArray());
        }
    }

    /// <summary>Writes <paramref name="text" /> as the blueprint called <paramref name="name" /> in <paramref name="folder" />, deleting the binary beside it.</summary>
    public static string Save(string folder, string name, string text)
    {
        var into = Path.Combine(folder, name);
        Directory.CreateDirectory(into);
        var binary = Path.Combine(into, Binary);
        if (System.IO.File.Exists(binary))
            System.IO.File.Delete(binary);

        var path = Path.Combine(into, File);
        System.IO.File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    static XElement Armor(Vector3I cell)
    {
        return new XElement("MyObjectBuilder_CubeBlock",
            new XAttribute(Xsi + "type", "MyObjectBuilder_" + Deck.ArmorType),
            new XElement("SubtypeName", Deck.ArmorSubtype),
            Min(cell),
            Color());
    }

    static XElement Block(Fitting fitting, Deck deck)
    {
        var definition = deck.Defined(fitting);
        var placement = deck[fitting];
        var block = new XElement("MyObjectBuilder_CubeBlock",
            new XAttribute(Xsi + "type", "MyObjectBuilder_" + definition.Type),
            new XElement("SubtypeName", definition.Subtype),
            new XElement("EntityId", fitting.Id),
            Min(placement.Min));

        if (placement.Forward != "Forward" || placement.Up != "Up")
            block.Add(new XElement("BlockOrientation", new XAttribute("Forward", placement.Forward), new XAttribute("Up", placement.Up)));

        block.Add(Color());

        var components = new XElement("Components");
        var storage = Storage(fitting.Storage);
        if (storage != null)
            components.Add(storage);

        var text = deck.Text(fitting);
        if (text != null)
            components.Add(Screen(text));

        if (components.HasElements)
            block.Add(new XElement("ComponentContainer", components));

        block.Add(new XElement("CustomName", fitting.Name));

        if (Toolbared(definition.Type))
            block.Add(Toolbar(fitting.Toolbar));

        // Charged full, so the construct has power the moment it is pasted.
        if (definition.Stored != null)
            block.Add(new XElement("CurrentStoredPower", definition.Stored));

        return block;
    }

    /// <summary>Whether a block of <paramref name="type" /> fails to paste without a toolbar.</summary>
    static bool Toolbared(string type)
    {
        switch (type)
        {
            case "ButtonPanel":
            case "TimerBlock":
            case "SensorBlock":
            case "TargetDummyBlock":
            case "TurretControlBlock":
            case "EventControllerBlock":
                return true;
            default:
                return false;
        }
    }

    /// <summary>A text panel's surface showing <paramref name="text" />.</summary>
    /// <remarks>Rotation index 1 stands the text upright on a sign, as seen in the game.</remarks>
    static XElement Screen(string text)
    {
        return new XElement("ComponentData",
            new XElement("TypeId", "MyLcdSurfaceComponent"),
            new XElement("Component",
                new XAttribute(Xsi + "type", "MyObjectBuilder_LcdSurfaceComponent"),
                new XElement("SelectedRotationIndex", 1),
                new XElement("TextPanelContent",
                    new XElement("Font", new XAttribute("Type", "MyObjectBuilder_FontDefinition"), new XAttribute("Subtype", "Debug")),
                    new XElement("FontSize", Instructions.FontSize),
                    new XElement("Text", text),
                    new XElement("ContentType", "TEXT_AND_IMAGE"),
                    new XElement("TextPadding", 2))));
    }

    static XElement Storage(MyModStorageComponentBase storage)
    {
        if (storage == null)
            return null;

        var items = new XElement("dictionary");
        foreach (var key in new[] { BlockStorage.ProgramKey, BlockStorage.StateKey })
        {
            string value;
            if (storage.TryGetValue(key, out value) && value != null)
                items.Add(new XElement("item", new XElement("Key", key.ToString("D")), new XElement("Value", value)));
        }

        if (!items.HasElements)
            return null;

        return new XElement("ComponentData",
            new XElement("TypeId", "MyModStorageComponent"),
            new XElement("Component",
                new XAttribute(Xsi + "type", "MyObjectBuilder_ModStorageComponent"),
                new XElement("Storage", items)));
    }

    // Written even when empty: the paste needs the element.
    static XElement Toolbar(Toolbar toolbar)
    {
        var slots = new XElement("Slots");
        var parameters = new List<TerminalActionParameter>();
        for (var index = 0; toolbar != null && index < toolbar.ItemCount; index++)
        {
            MyToolbarItemInfo item;
            if (!toolbar.TryGetItem(index, out item))
                continue;

            parameters.Clear();
            toolbar.Parameters(index, parameters);
            var written = new XElement("Parameters");
            foreach (var parameter in parameters)
                written.Add(new XElement("MyObjectBuilder_ToolbarItemActionParameter",
                    new XElement("TypeCode", parameter.TypeCode.ToString()),
                    new XElement("Value", Convert.ToString(parameter.Value, CultureInfo.InvariantCulture))));

            slots.Add(new XElement("Slot",
                new XElement("Index", index),
                new XElement("Item"),
                new XElement("Data",
                    new XAttribute(Xsi + "type", "MyObjectBuilder_ToolbarItemTerminalBlock"),
                    new XElement("Action", item.ActionId),
                    written,
                    new XElement("BlockEntityId", item.BlockEntityId))));
        }

        return new XElement("Toolbar",
            new XElement("ToolbarType", "ButtonPanel"),
            new XElement("SelectedSlot", new XAttribute(Xsi + "nil", true)),
            slots,
            new XElement("SlotsGamepad"));
    }

    static XElement Min(Vector3I cell)
    {
        return new XElement("Min", new XAttribute("x", cell.X), new XAttribute("y", cell.Y), new XAttribute("z", cell.Z));
    }

    static XElement Color()
    {
        return new XElement("ColorMaskHSV", new XAttribute("x", 0), new XAttribute("y", -0.8), new XAttribute("z", 0.4));
    }

    static XElement Vector(string name, Vector3D value)
    {
        return new XElement(name,
            new XAttribute("x", value.X.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y", value.Y.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("z", value.Z.ToString(CultureInfo.InvariantCulture)));
    }
}
