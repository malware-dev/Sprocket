using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Which of a block's entries a step names, where its id alone does not say.
/// </summary>
/// <remarks>
/// A switch's value and the game's bare toggle for it share an id. A program writes the first as a
/// <c>set</c> and the second as an action.
/// </remarks>
public class SetTests
{
    readonly EntityAction _toggle = new EntityAction("OnOff", "Toggle block On/Off", null, "OnOff");

    readonly EntityAction _value = new EntityAction("OnOff", "set On", null, null, 1)
        .Writes("OnOff", Field.Called("On"), "On");

    [Fact]
    public void AnActionNamesTheToggle()
    {
        var flip = new Act("OnOff");

        Assert.Same(_toggle, flip.Names(Both()));
        Assert.False(flip.Sets);
    }

    [Fact]
    public void ASetNamesTheValue()
    {
        var off = new Act("OnOff") { Sets = true };
        off.Values.Add(Literal.Of(false));

        Assert.Same(_value, off.Names(Both()));
    }

    List<EntityAction> Both()
    {
        return new List<EntityAction> { _toggle, _value };
    }
}
