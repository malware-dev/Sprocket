using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A sound block on the rig, which writes down each sound it is told to play.</summary>
public sealed class Speaker : Fitting
{
    static readonly string[] Sounds = { "SoundBlockAlert1", "SoundBlockAlert2", "SoundBlockLightsOn" };

    /// <summary>A sound block on the rig, with no sound chosen.</summary>
    public Speaker(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMySoundBlock>()
            .Gives("SelectedSound", () => Selected)
            .Takes("SelectedSound", value => Selected = (string)value)
            .Does("GetSounds", args => Listed((List<string>)args[0]))
            .Does("Play", _ => Played())
            .Does("Stop", _ => Stopped()));
    }

    /// <summary>The sound it plays next, which is whichever was chosen last.</summary>
    public string Selected { get; set; } = "";

    /// <inheritdoc />
    public override bool Offers(string actionId)
    {
        return actionId == "PlaySound" || actionId == "StopSound" || base.Offers(actionId);
    }

    /// <inheritdoc />
    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("PlaySound", "Play"));
        into.Add(new Offer("StopSound", "Stop"));
        base.Offered(into);
    }

    /// <inheritdoc />
    public override void Perform(string actionId, IList<Literal> arguments)
    {
        if (actionId == "PlaySound")
            Played();
        else if (actionId == "StopSound")
            Stopped();
        else
            base.Perform(actionId, arguments);
    }

    /// <summary>Its sounds by id, which are a few of the game's own from its sound categories.</summary>
    static void Listed(List<string> into)
    {
        into.Clear();
        into.AddRange(Sounds);
    }

    void Played()
    {
        Told("play " + Selected);
    }

    void Stopped()
    {
        Told("stop");
    }
}
