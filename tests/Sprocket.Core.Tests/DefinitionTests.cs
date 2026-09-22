using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A mod's own file says what its actions' parameters are, and the schema beside it agrees.
/// </summary>
/// <remarks>
/// The file is a contract with people we will never speak to: once a mod ships one, the element and
/// attribute names are fixed forever. So the example here is validated against the schema in the
/// repository root and then read with the same code the game uses - which is the only way the three
/// cannot drift apart. A schema that has stopped describing what we read is worse than none.
/// </remarks>
public class DefinitionTests
{
    const string Example = @"<?xml version=""1.0"" encoding=""utf-8""?>
<sprocket-definitions>
  <actions>
    <action id=""SetPressure"" block=""MyObjectBuilder_OxygenTank/BigTankOfDoom"">
      <parameter name=""Bar"">How hard to squeeze it, between 0 and 50.</parameter>
    </action>
    <action id=""SetSpin"">
      <parameter name=""Revolutions per minute"" />
      <parameter name=""Clockwise"" />
    </action>
    <action id=""SetGlow"" block=""MyObjectBuilder_InteriorLight/GlowyThing"">
      <group kind=""color"">
        <parameter name=""Red"" min=""0"" max=""255"" />
        <parameter name=""Green"" min=""0"" max=""255"" />
        <parameter name=""Blue"" min=""0"" max=""255"" />
      </group>
    </action>
    <action id=""SetMood"">
      <group kind=""a-kind-nobody-has-drawn"">
        <parameter name=""Red"" min=""0"" max=""255"" />
        <parameter name=""Green"" min=""0"" max=""255"" />
        <parameter name=""Blue"" min=""0"" max=""255"" />
      </group>
    </action>
  </actions>
</sprocket-definitions>";

    [Fact]
    public void TheExampleIsWhatTheSchemaSaysItShouldBe()
    {
        var trouble = "";

        var settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas.Add(null, Path.Combine(Root(), "SprocketDefinitions.xsd"));
        settings.ValidationEventHandler += (_, e) => trouble += e.Message + " ";

        using (var reader = XmlReader.Create(new StringReader(Example), settings))
            while (reader.Read()) { }

        Assert.Equal("", trouble);
    }

    [Fact]
    public void AndIsReadAsWhatItSays()
    {
        Descriptions said = Read(Example);

        Assert.Equal(4, said.Actions.Count);

        // A parameter of its own, with a note, against one exact block.
        Description pressure = said.Actions[0];
        Assert.Equal("SetPressure", pressure.Id);
        Assert.Equal("MyObjectBuilder_OxygenTank/BigTankOfDoom", pressure.Block);
        Assert.Equal(1, pressure.Covers);
        Assert.Contains("squeeze", ((Parameter)pressure.Pieces[0]).Note);

        // Two, on no block at all, which is the loose claim.
        Description spin = said.Actions[1];
        Assert.Equal("", spin.Block ?? "");
        Assert.Equal(2, spin.Covers);

        // A group, which covers as many as it holds and says what they are together.
        Description glow = said.Actions[2];
        Assert.Equal(3, glow.Covers);

        var group = Assert.IsType<Group>(glow.Pieces[0]);
        Assert.Equal("color", group.Kind);
        Assert.Equal(3, group.Parameters.Count);
    }

    /// <summary>
    /// A group Sprocket can draw comes out as one field standing over all of its parameters.
    /// </summary>
    /// <remarks>
    /// Which is the whole point of grouping them: three numbers are one colour to a person, and the
    /// form shows the control for that rather than three boxes with nothing to tell them apart.
    /// </remarks>
    [Fact]
    public void AGroupItCanDrawComesOutAsOneField()
    {
        var fields = Fields(2);

        Field whole = Assert.Single(fields);
        Assert.Equal(Editor.Colour, whole.Edits);
        Assert.Equal(3, whole.Covers);
    }

    /// <summary>
    /// A group nothing can draw comes out flattened, which is what it would have been ungrouped.
    /// </summary>
    /// <remarks>
    /// So a file written against a later Sprocket still works here. It loses the control and keeps
    /// everything else: the names, the order and the bounds.
    /// </remarks>
    [Fact]
    public void AndOneItCannotComesOutInOrder()
    {
        var fields = Fields(3);

        // Each says what it will accept, because the moment that is worth knowing is the moment
        // somebody is deciding what to type - and the action itself never says.
        Assert.Equal(new[] { "Red (0 - 255)", "Green (0 - 255)", "Blue (0 - 255)" },
            fields.ConvertAll(field => field.Label));

        foreach (Field field in fields)
            Assert.Equal(1, field.Covers);
    }

    /// <summary>And what a file said it accepts is what the field holds anybody to.</summary>
    [Fact]
    public void AndWhatTheyTakeIsWhatTheFileSaid()
    {
        var fields = Fields(3);

        Assert.Equal("", fields[0].Refuses("Red", "255"));
        Assert.Equal("Red has to be between 0 and 255.", fields[0].Refuses("Red", "256"));
    }

    static List<Field> Fields(int action)
    {
        Description said = Read(Example).Actions[action];

        var fields = new List<Field>();
        said.Fields(fields);

        return fields;
    }

    static Descriptions Read(string text)
    {
        var reader = new XmlSerializer(typeof(Descriptions));
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            return (Descriptions)reader.Deserialize(stream);
    }

    /// <summary>The repository root, found from where the tests are running.</summary>
    static string Root()
    {
        var at = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (at != null && !File.Exists(Path.Combine(at.FullName, "SprocketDefinitions.xsd")))
            at = at.Parent;

        Assert.NotNull(at);
        return at.FullName;
    }
}
