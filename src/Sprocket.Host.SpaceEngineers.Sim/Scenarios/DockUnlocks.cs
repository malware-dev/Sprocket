using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A connector and a magnetic plate, each with a light its logic system switches off when it unlocks.</summary>
public sealed class DockUnlocks : Scenario
{
    /// <summary>The connector the ship docks to.</summary>
    public Coupling Connector { get; private set; } = null!;

    /// <summary>The light switched off when the connector unlocks.</summary>
    public Lamp ConnectorLight { get; private set; } = null!;

    /// <summary>The magnetic plate a block is held to.</summary>
    public Clamp Plate { get; private set; } = null!;

    /// <summary>The light switched off when the plate unlocks.</summary>
    public Lamp PlateLight { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Dock Unlocks";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A connector and a magnetic plate that unlock",
            "Sprocket Dock Unlocks pasted, a ship docked to Dock Connector and a block against Dock Plate.",
            new[] { "Lock Dock Connector and Dock Plate, and read both lights.", "Unlock them, and read both lights again." },
            "both lights stay on while locking, and go off as each unlocks.",
            "The harness locks no real connector or plate, so only the game says that unlocking one is told to Sprocket.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Dock Automation";
        Connector = rig.Connector(2, "Dock Connector");
        ConnectorLight = rig.Light(3, "Connector Light");
        Plate = rig.MagneticPlate(4, "Dock Plate");
        PlateLight = rig.Light(5, "Plate Light");
        deck.Lay(new List<Fitting> { Connector, ConnectorLight, Plate, PlateLight, rig.Sprocket, rig.Battery(6, "Dock Battery") }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        Unlocking(sim, "Connector", "Dock Connector", "Connector Light");
        sim.Editor.GoToRoot();
        Unlocking(sim, "Plate", "Dock Plate", "Plate Light");
    }

    /// <summary>A logic system called <paramref name="system" />, switched on, that switches <paramref name="light" /> off when <paramref name="block" /> unlocks.</summary>
    static void Unlocking(Machine.Simulation sim, string system, string block, string light)
    {
        var editor = sim.Editor;
        editor.NewSystem(system);
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(block);
        editor.PressChoice("unlocks");
        editor.PressPrimary();
        sim.Tick();
        Power(sim, light, "off");
        editor.SwitchOn();
        sim.Tick();
    }
}
