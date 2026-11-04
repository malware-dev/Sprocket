using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A line testing blocks says how it reads them beside the blocks where it names a value, and at the front where the blocks
/// are its subject; a sum and an average always open it.
/// </summary>
public class TestShapeTests
{
    /// <summary>A line naming a value says "the" before it, and how it reads its blocks beside them.</summary>
    [Theory]
    [InlineData(Quantifier.Any, false, false, "the radius on Lamp")]
    [InlineData(Quantifier.Any, true, false, "the radius on any of Lamp")]
    [InlineData(Quantifier.All, true, false, "the radius on each of Lamp")]
    [InlineData(Quantifier.Sum, true, false, "the sum of the radius on Lamp")]
    [InlineData(Quantifier.Average, true, false, "the average of the radius on Lamp")]
    [InlineData(Quantifier.Any, true, true, "the radius on any in Lamp")]
    [InlineData(Quantifier.All, true, true, "the radius on each in Lamp")]
    [InlineData(Quantifier.Sum, true, true, "the sum of the radius on Lamp")]
    public void ALineNamingAValueReadsItsBlocksBesideThem(Quantifier quantifier, bool many, bool slotted, string said)
    {
        var shape = TestShape.Of(quantifier, true, many, slotted);

        Assert.Equal(said, shape.Says("radius", "Lamp"));
        Assert.True(shape.JoinsAll);
        Assert.False(shape.IsPlural);
    }

    /// <summary>A line whose blocks are its subject says how it reads them first, in the plural where all of them must.</summary>
    [Theory]
    [InlineData(Quantifier.Any, false, false, "Door", false, false)]
    [InlineData(Quantifier.Any, true, false, "any Door", false, false)]
    [InlineData(Quantifier.All, true, false, "all Door", true, true)]
    [InlineData(Quantifier.Any, true, true, "any in Door", false, false)]
    [InlineData(Quantifier.All, true, true, "all in Door", true, true)]
    public void ALineAboutTheBlocksReadsThemFirst(Quantifier quantifier, bool many, bool slotted, string said, bool joinsAll,
        bool plural)
    {
        var shape = TestShape.Of(quantifier, false, many, slotted);

        Assert.Equal(said, shape.Says(null, "Door"));
        Assert.Equal(joinsAll, shape.JoinsAll);
        Assert.Equal(plural, shape.IsPlural);
    }

    /// <summary>A row's parts, with the words joining them, read exactly as the line's plain words do, for every shape, with and without arguments.</summary>
    [Fact]
    public void ARowAndItsWordsAlwaysAgree()
    {
        foreach (var quantifier in new[] { Quantifier.Any, Quantifier.All, Quantifier.Sum, Quantifier.Average })
        foreach (var namesValue in new[] { false, true })
        foreach (var many in new[] { false, true })
        foreach (var slotted in new[] { false, true })
        foreach (var takes in new[] { false, true })
        {
            var shape = TestShape.Of(quantifier, namesValue, many, slotted);
            var reading = new ActionSlot(null!, null!);
            var value = namesValue ? new ActionSlot(null!, null!) : null;
            var blocks = new ActionSlot(null!, null!);
            var taking = takes ? new ActionSlot(null!, null!) : null;
            var parts = new List<SentenceSlot>();
            shape.Lay(parts, shape.IsRead ? reading : null, value, blocks, taking);

            var row = new List<string>();
            foreach (var part in parts)
            {
                if (part.Joined.Length != 0)
                    row.Add(part.Joined);

                row.Add(part == reading ? shape.Reading : part == value ? "radius" : part == taking ? "2" : "Lamp");
            }

            Assert.Equal(shape.Says(namesValue ? "radius" : null, "Lamp", takes ? "2" : null), string.Join(" ", row));
        }
    }

    /// <summary>The parts of a row stand in the order the line says them.</summary>
    [Theory]
    [InlineData(Quantifier.Sum, "reading value blocks")]
    [InlineData(Quantifier.Any, "value reading blocks")]
    [InlineData(Quantifier.All, "value reading blocks")]
    public void APartsRowFollowsTheLine(Quantifier quantifier, string order)
    {
        var reading = new ActionSlot(null!, null!);
        var value = new ActionSlot(null!, null!);
        var blocks = new ActionSlot(null!, null!);
        var parts = new List<SentenceSlot>();

        TestShape.Of(quantifier, true, true, false).Lay(parts, reading, value, blocks);

        Assert.Equal(order, string.Join(" ", parts.Select(part => part == reading ? "reading" : part == value ? "value" : "blocks")));
    }
}
