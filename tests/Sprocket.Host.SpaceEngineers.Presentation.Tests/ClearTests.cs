using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What is typed into a box that narrows a list can be thrown away without deleting it.
/// </summary>
/// <remarks>
/// Both boxes, because they are two questions asked in the same shape: one narrows the blocks and
/// one narrows the kinds, and either can be left holding a word somebody has finished with.
/// </remarks>
public class ClearTests
{
    const string OpenTheMenu = "60 133 type Airlock enter 60 237 60 244 60 285 type door";
    const string SearchTheBlocks = "60 130 type Airlock enter 60 237 150 200 type door";

    [Fact]
    public void TheCrossBesideTheMenusBoxEmptiesIt()
    {
        var picker = Screen.Fresh(OpenTheMenu.Split(' '));

        Assert.True(picker.Shows("door"), "the menu was not narrowed to begin with");

        picker.Do("155", "297");

        Assert.False(picker.Shows("door"), "the cross did not empty the box");
        Assert.True(picker.Shows("Button Panel"), "the kinds did not come back");
    }

    [Fact]
    public void AndTheCrossBesideTheBlockBoxEmptiesThatOne()
    {
        var picker = Screen.Fresh(SearchTheBlocks.Split(' '));

        Assert.True(picker.Shows("3 of 14"), "the blocks were not narrowed to begin with");

        picker.Do("452", "206");

        Assert.True(picker.Shows("14 blocks"), "the cross did not empty the box");
    }

    /// <summary>And there is nothing to press while there is nothing written.</summary>
    /// <remarks>
    /// A way out that does nothing is a control somebody presses twice before believing it.
    /// </remarks>
    [Fact]
    public void AndNeitherIsThereWhileItsBoxIsEmpty()
    {
        var picker = Screen.Fresh("60 130 type Airlock enter 60 237".Split(' '));

        Element? at = picker.Scene.Under(new Vector2(452f, 230f));

        Assert.False(at is Glyph, "the cross is offered over an empty box");
    }
}
