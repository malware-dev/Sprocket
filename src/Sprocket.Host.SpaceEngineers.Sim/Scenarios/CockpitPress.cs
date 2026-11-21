using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A cockpit whose first toolbar slot runs a sequence that switches a light off.</summary>
public sealed class CockpitPress : Scenario
{
    /// <summary>The cockpit whose toolbar is pressed.</summary>
    public Seat Cockpit { get; private set; } = null!;

    /// <summary>The light the press switches off.</summary>
    public Lamp Light { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Cockpit Press";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A cockpit's toolbar slot on a dedicated server",
            "Sprocket Cockpit Press pasted on a dedicated server, owned by the player, joined from a game client.",
            new[] { "Sit in Press Cockpit and press its toolbar slot 1." },
            "Press Light goes off.",
            "A ship's toolbar runs its slot on the pressing player's own machine, so only a client joined to a dedicated server shows whether the press reaches the server.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Press Automation";
        Cockpit = rig.Cockpit(2, "Press Cockpit");
        Light = rig.Light(3, "Press Light");
        deck.Lay(new List<Fitting> { Cockpit, Light, rig.Sprocket, rig.Battery(4, "Press Battery") }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Press");
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Press Cockpit");
        editor.PressChoice("toolbar slot is pressed");
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();

        Power(sim, "Press Light", "off");
        editor.SwitchOn();
        sim.Tick();
    }
}
