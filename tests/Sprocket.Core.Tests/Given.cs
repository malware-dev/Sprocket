using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Tests;

/// <summary>What a step a test writes by hand is given, as the editor would write it.</summary>
public static class Given
{
    /// <summary>
    /// <paramref name="act" />, given what the actions with options are written with where a test says nothing else: a vent's
    /// hold, and a tank's stockpile waiting until full.
    /// </summary>
    /// <remarks>A member is given an argument for every parameter, so one of these written with none is a fault.</remarks>
    public static Act Defaults(Act act)
    {
        if (act.Arguments.Count == 0 && (act.ActionId == Vent.Pressurize || act.ActionId == Vent.Depressurize))
            act.Arguments.Add(Literal.Of(Vent.Hold));

        if (act.Arguments.Count == 0 && act.ActionId == Tank.Stockpile)
            act.Arguments.Add(Literal.Of(true));

        return act;
    }

    /// <summary>A step that switches <paramref name="targets" /> on or off, as the editor writes one: by setting their power.</summary>
    public static Act Power(bool on, params string[] targets)
    {
        var act = new Act("power") { Form = ReachKind.Value, Provider = BlockKinds.Own };
        act.Arguments.Add(Literal.Of(on));
        foreach (var target in targets)
            act.Targets.Add(target);
        return act;
    }
}
