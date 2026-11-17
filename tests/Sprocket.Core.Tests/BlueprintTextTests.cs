using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A blueprint copied as text comes back as it went, from Custom Data that holds other things too.</summary>
public class BlueprintTextTests
{
    static string Blueprint(string name)
    {
        return ProgramWriter.Blueprint(new AutomationSystem(1, Guid.NewGuid()) { Name = name, Namespace = "Shop", IsBlueprint = true });
    }

    /// <summary>A blueprint written as text reads back whole, with its name.</summary>
    [Fact]
    public void ABlueprintComesBackAsItWent()
    {
        var blueprint = Blueprint("Hangar Doors");

        var read = Assert.Single(BlueprintText.Read(BlueprintText.Write("Hangar Doors", blueprint)));

        Assert.Equal("Hangar Doors", read.Name);
        Assert.Equal(blueprint, Rewritten(read.Blueprint!));
    }

    /// <summary>Any bytes pack and unpack: a blueprint, text outside ASCII, and long runs.</summary>
    [Fact]
    public void AnyBytesPackAndUnpack()
    {
        foreach (var text in new[] { "", "a", Unseen, "Luftsøke æøå 🚀 中文", new string('x', 70000) })
            Assert.Equal(Utf8.Bytes(text), BytePacker.Unpack(BytePacker.Pack(Utf8.Bytes(text))));
    }

    /// <summary>A blueprint reads the same after its tokens are coded and decoded.</summary>
    [Fact]
    public void TokensComeBackAsTheyWent()
    {
        var decoded = TokenCode.Decode(TokenCode.Encode(Unseen))!;

        Assert.Equal(TokenCode.Encode(Unseen), TokenCode.Encode(decoded));
        Assert.Equal(Rewritten(Unseen.Replace("\r\n", "\n")), Rewritten(TokenCode.Decode(TokenCode.Encode(Unseen))!));
    }

    /// <summary>Bytes written as typed characters read back whole, zeros at either end included.</summary>
    [Fact]
    public void TypedBytesReadBackWhole()
    {
        foreach (var bytes in new[] { new byte[0], new byte[] { 0 }, new byte[] { 0, 0, 7, 0 }, new byte[] { 255, 255 }, Utf8.Bytes(Unseen) })
            Assert.Equal(bytes, TypedBytes.Read(TypedBytes.Write(bytes)));
    }

    /// <summary>Literals come back as their values, spelled the way the writer spells them.</summary>
    [Fact]
    public void LiteralsComeBackAsTheirValues()
    {
        const string literals = "set x to 0 7 1234567890123 2.5 0.125 2.50 1e3 1e-30 1e300 0.1 600t 1.5s #E8B323FF #80ff80";

        Assert.Equal("set x to 0 7 1234567890123 2.5 0.125 2.5 1000 1E-30 1E+300 0.1 600t 1.5s #E8B323FF #80FF80FF",
            TokenCode.Decode(TokenCode.Encode(literals)));
    }

    /// <summary>Every double comes back as exactly the same double.</summary>
    [Fact]
    public void RealsComeBackExactly()
    {
        var random = new Random(17);
        for (var i = 0; i < 10000; i++)
        {
            var value = BitConverter.Int64BitsToDouble(((long)random.Next() << 32) | (uint)random.Next());
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                continue;

            var spelled = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            var back = TokenCode.Decode(TokenCode.Encode("set x to " + spelled))!.Substring(9);
            Assert.Equal(value, double.Parse(back, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Every keyword the language has is coded in one byte.</summary>
    [Fact]
    public void EveryKeywordIsInTheTable()
    {
        foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
        {
            if (keyword != Keyword.None)
                Assert.Contains(Keywords.Name(keyword), TokenCode.Spellings);
        }
    }

    /// <summary>The table keeps its order: every shared text ever written depends on it, so an entry is only ever added at the end.</summary>
    [Fact]
    public void TheTableKeepsItsOrder()
    {
        var released = new[]
        {
            "{", "}", "[", "]", "(", ")", ",", ".", ":", "=", "|", "//", "#@",
            "sprocket", "block", "system", "instance", "of", "sequence",
            "when", "every", "then", "trigger", "to", "run", "set",
            "on", "off", "all", "any", "wait", "await", "required", "expects", "changed", "as", "last",
            "running", "step", "stop", "call", "alpha", "optional", "let", "blueprint", "slot", "if", "else", "==",
            "signal", "sum", "!=", "<", ">", "<=", ">=", "whenever", "from", "average", "type"
        };

        Assert.Equal(released, TokenCode.Spellings.Take(released.Length));
    }

    /// <summary>A name used a second time takes two bytes.</summary>
    [Fact]
    public void ARepeatedNameIsShort()
    {
        var once = TokenCode.Encode("call sprocket.move_items on [ 'Cargo One' ]");
        var twice = TokenCode.Encode("call sprocket.move_items on [ 'Cargo One' ] on [ 'Cargo One' ]");

        // on, [, the name pointed back to in two bytes, and ].
        Assert.Equal(once.Length + 5, twice.Length);
        Assert.Equal("call sprocket . move_items on [ 'Cargo One' ] on [ 'Cargo One' ]", TokenCode.Decode(twice));
    }

    /// <summary><paramref name="script" /> read as a blueprint and written again, as the library stores it.</summary>
    static string Rewritten(string script)
    {
        Assert.Equal("", ProgramReader.Lone(script, new Values(), out var system));
        return ProgramWriter.Blueprint(system);
    }

    /// <summary>A text wrapped over several lines, with the section among other scripts' sections, still reads.</summary>
    [Fact]
    public void AWrappedTextAmongOthersReads()
    {
        var blueprint = Blueprint("Airlock");
        var written = BlueprintText.Write("Airlock", blueprint).TrimEnd('\n');
        var data = written.Substring(written.IndexOf("data=", StringComparison.Ordinal) + 5);
        var wrapped = written.Substring(0, written.Length - data.Length) + string.Join("\n  ", Chunks(data, 20));

        var read = BlueprintText.Read("[other-script]\nspeed=4\n\n" + wrapped + "\n\n[more]\nkey=value\n");

        Assert.Equal(blueprint, Rewritten(Assert.Single(read).Blueprint!));
    }

    /// <summary>Two sections give two blueprints, in the order they are written.</summary>
    [Fact]
    public void TwoSectionsGiveTwoBlueprints()
    {
        var read = BlueprintText.Read(BlueprintText.Write("One", Blueprint("One")) + BlueprintText.Write("Two", Blueprint("Two")));

        Assert.Equal(new[] { "One", "Two" }, read.Select(text => text.Name));
        Assert.All(read, text => Assert.NotNull(text.Blueprint));
    }

    /// <summary>A section of another version, or one whose data was cut short, gives its name but no blueprint.</summary>
    [Fact]
    public void ADamagedOrNewerSectionGivesNoBlueprint()
    {
        var written = BlueprintText.Write("Airlock", Blueprint("Airlock"));

        var newer = Assert.Single(BlueprintText.Read(written.Replace("version=1", "version=2")));
        var cut = Assert.Single(BlueprintText.Read(written.Substring(0, written.Length - 24)));

        Assert.Equal("Airlock", newer.Name);
        Assert.Null(newer.Blueprint);
        Assert.Null(cut.Blueprint);
    }

    // A typical blueprint, as a test scenario writes it.
    const string Unseen = @"sprocket 1 alpha 28

system blueprint ""4f739dd7-7258-4165-b562-d13b22f56316"" as Engineer.Amounts
changed ""2026-11-17T06:31:16Z""
{
    required slot 'signal slot 1' expects signal
    required slot 'inventory slot 2' expects sprocket.inventory
    required slot 'inventory slot 1' expects sprocket.inventory
    required slot 'signal slot 2' expects signal
    required slot 'signal slot 3' expects signal
    required slot 'signal slot 4' expects signal
    required slot 'block slot 1' expects InteriorLight

    whenever 'signal slot 1' then run 1

    whenever 'signal slot 2' then run 2

    whenever 'signal slot 3' then run 3

    whenever 'signal slot 4' then run 4

    sequence 1 as 'Sequence 1' {
        call sprocket.move_items([ 'inventory slot 2' ], ""Ingot/Iron"", 100, 0) on [ 'inventory slot 1' ]
        await
    }

    sequence 2 as 'Sequence 2' {
        call sprocket.move_items([ 'inventory slot 2' ], ""Component/SteelPlate"", 2.5, 0) on [ 'inventory slot 1' ]
        await
    }

    sequence 3 as 'Sequence 3' {
        call sprocket.move_items([ 'inventory slot 2' ], """", """", 0) on [ 'inventory slot 1' ]
        await
    }

    sequence 4 as 'Sequence 4' {
        call sprocket.move_items([ 'inventory slot 2' ], ""Ingot/Iron"", 100, 1) on [ 'inventory slot 1' ]
        await
        set sprocket.power on [ 'block slot 1' ] to on
    }
}
";

    /// <summary>A typical blueprint packs to well under a third of its length in typed characters.</summary>
    [Fact]
    public void ABlueprintPacksSmall()
    {
        var blueprint = Unseen.Replace("\r\n", "\n");

        var written = BlueprintText.Write("Amounts", blueprint);
        var data = written.Substring(written.IndexOf("data=", StringComparison.Ordinal) + 5).Replace("|", "").Replace("\n", "");

        Assert.True(data.Length * 3 < blueprint.Length, data.Length + " characters for " + blueprint.Length);
    }

    /// <summary>
    /// The blueprint's identity names its section and is not in the data, so two blueprints' sections never share a name, and
    /// the identity comes back with the section.
    /// </summary>
    [Fact]
    public void TheIdentityNamesTheSection()
    {
        var one = BlueprintText.Write("One", Blueprint("One"));
        var two = BlueprintText.Write("Two", Blueprint("Two"));

        var read = BlueprintText.Read(one + two);

        Assert.NotEqual(one.Substring(0, one.IndexOf('\n')), two.Substring(0, two.IndexOf('\n')));
        Assert.Equal(2, read.Count);
        Assert.All(read, text => Assert.Equal(text.Identity, Guid.Parse(one.Contains(text.Name!) ? one.Substring(20, 36) : two.Substring(20, 36))));
        Assert.All(read, text => Assert.Contains(text.Identity!.Value.ToString("D"), text.Blueprint));
    }

    /// <summary>A plain blueprint, the whole of the text, reads as itself.</summary>
    [Fact]
    public void APlainBlueprintReadsAsItself()
    {
        var blueprint = Blueprint("Airlock");

        var read = Assert.Single(BlueprintText.Read("\n" + blueprint + "\n"));

        Assert.Equal(blueprint.Trim(), read.Blueprint);
        Assert.Null(read.Identity);
    }

    /// <summary>When the blueprint was last changed is a key of the section, out of the data, and comes back with it.</summary>
    [Fact]
    public void TheChangedTimeIsAKeyOfItsOwn()
    {
        var written = BlueprintText.Write("Amounts", Unseen.Replace("\r\n", "\n"));

        Assert.Contains("\nchanged=2026-11-17T06:31:16Z\n", written);
        var read = Assert.Single(BlueprintText.Read(written));
        Assert.Equal(new DateTime(2026, 11, 17, 6, 31, 16, DateTimeKind.Utc), read.Changed);
        Assert.DoesNotContain("changed", read.Blueprint);
    }

    /// <summary>The data is MyIni's multi-line value: every line after <c>data=</c> starts with | and is no wider than the editor takes.</summary>
    [Fact]
    public void TheDataIsInNarrowLines()
    {
        var written = BlueprintText.Write("Amounts", Unseen.Replace("\r\n", "\n"));
        var lines = written.Substring(written.IndexOf("data=\n", StringComparison.Ordinal) + 6).TrimEnd('\n').Split('\n');

        Assert.True(lines.Length > 1);
        Assert.All(lines, line => Assert.StartsWith("|", line));
        Assert.All(lines, line => Assert.InRange(line.Length, 2, BlueprintText.Width + 1));
    }

    static IEnumerable<string> Chunks(string text, int size)
    {
        for (var at = 0; at < text.Length; at += size)
            yield return text.Substring(at, Math.Min(size, text.Length - at));
    }
}
