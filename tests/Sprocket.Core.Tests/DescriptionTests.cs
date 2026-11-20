using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What Sprocket says of an action's parameters becomes the fields a step asks them by.</summary>
public class DescriptionTests
{
    /// <summary>
    /// A group Sprocket can draw comes out as one field standing over all of its parameters.
    /// </summary>
    /// <remarks>
    /// Which is the whole point of grouping them: three numbers are one color to a person, and the
    /// form shows the control for that rather than three boxes with nothing to tell them apart.
    /// </remarks>
    [Fact]
    public void AGroupItCanDrawComesOutAsOneField()
    {
        var fields = Fields(Channels("color"));

        Field whole = Assert.Single(fields);
        Assert.Equal(FieldEditor.Color, whole.Edits);
        Assert.Equal(3, whole.Covers);
    }

    /// <summary>
    /// A group nothing can draw comes out flattened, which is what it would have been ungrouped: it loses the control and
    /// keeps the names, the order and the bounds.
    /// </summary>
    [Fact]
    public void AndOneItCannotComesOutInOrder()
    {
        var fields = Fields(Channels("a-kind-nobody-has-drawn"));

        // Each says what it will accept, because the moment that is worth knowing is the moment
        // somebody is deciding what to type - and the action itself never says.
        Assert.Equal(new[] { "Red (0 - 255)", "Green (0 - 255)", "Blue (0 - 255)" },
            fields.ConvertAll(field => field.Label(new Texts())));

        foreach (Field field in fields)
            Assert.Equal(1, field.Covers);
    }

    /// <summary>What a description says a parameter accepts is what the field holds anybody to.</summary>
    [Fact]
    public void AndWhatTheyTakeIsWhatTheDescriptionSaid()
    {
        var fields = Fields(Channels("a-kind-nobody-has-drawn"));

        Assert.Equal("", fields[0].Refuses(new Texts(), "Red", "255"));
        Assert.Equal("Red must be between 0 and 255.", fields[0].Refuses(new Texts(), "Red", "256"));
    }

    /// <summary>A group of the kind <paramref name="kind" /> over three channels, each from 0 to 255.</summary>
    static ParameterGroup Channels(string kind)
    {
        var group = new ParameterGroup { Sort = kind };
        foreach (var name in new[] { "Red", "Green", "Blue" })
            group.Parameters.Add(new Parameter { Name = name, Least = "0", Most = "255" });
        return group;
    }

    static List<Field> Fields(ParameterPiece piece)
    {
        var said = new Description { Id = "SetGlow" };
        said.Pieces.Add(piece);

        var fields = new List<Field>();
        said.Fields(fields);

        return fields;
    }
}
