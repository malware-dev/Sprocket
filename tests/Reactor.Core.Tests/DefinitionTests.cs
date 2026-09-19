using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Reactor.Host.SpaceEngineers.World;
using Xunit;

namespace Reactor.Tests;

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
<automation-definitions>
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
        <parameter name=""Red"" />
        <parameter name=""Green"" />
        <parameter name=""Blue"" />
      </group>
    </action>
  </actions>
</automation-definitions>";

    [Fact]
    public void TheExampleIsWhatTheSchemaSaysItShouldBe()
    {
        var trouble = "";

        var settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas.Add(null, Path.Combine(Root(), "AutomationDefinitions.xsd"));
        settings.ValidationEventHandler += (sender, e) => trouble += e.Message + " ";

        using (var reader = XmlReader.Create(new StringReader(Example), settings))
            while (reader.Read()) { }

        Assert.Equal("", trouble);
    }

    [Fact]
    public void AndIsReadAsWhatItSays()
    {
        Descriptions said = Read(Example);

        Assert.Equal(3, said.Actions.Count);

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

    /// <summary>The names come out flattened, because the form asks for one value at a time.</summary>
    [Fact]
    public void TheNamesComeOutInOrder()
    {
        Description glow = Read(Example).Actions[2];

        var names = new System.Collections.Generic.List<string>();
        glow.Names(names);

        Assert.Equal(new[] { "Red", "Green", "Blue" }, names);
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
        while (at != null && !File.Exists(Path.Combine(at.FullName, "AutomationDefinitions.xsd")))
            at = at.Parent;

        Assert.NotNull(at);
        return at.FullName;
    }
}
