using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Tests;

/// <summary>What a step a test writes by hand is given, as the editor would write it.</summary>
public static class Given
{
    /// <summary><paramref name="act" />, given what a vent's own actions are written with where a test says nothing else: hold.</summary>
    /// <remarks>A member is given an argument for every parameter, so a vent's action written with none is a fault.</remarks>
    public static Act Defaults(Act act)
    {
        if (act.Arguments.Count == 0 && (act.ActionId == Blocks.Vent.Pressurize || act.ActionId == Blocks.Vent.Depressurize))
            act.Arguments.Add(Literal.Of(Blocks.Vent.Hold));

        return act;
    }
}
