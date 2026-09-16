using Reactor.Host.SpaceEngineers.Core.World;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// What a value is, and what may be asked of one.
/// </summary>
/// <remarks>
/// The precision tests are the reason the kinds are told apart at all. The rest is the operator table
/// saying no where it has to, and the store refusing to read one kind as another.
/// </remarks>
public class ValueTests
{
    readonly Values _values = new Values();

    /// <summary>
    /// Two entity ids that a double cannot tell apart.
    /// </summary>
    /// <remarks>
    /// A double holds integers exactly only to 2^53, about 9.0x10^15. These two are 7.5x10^16 and one
    /// apart, so they land on the same double - which is what an id looks like after a round trip
    /// through a numeric type that is not wide enough. Nothing about that failure is visible: the id
    /// resolves to nothing, or to the wrong block.
    /// </remarks>
    const long OneId = 74857693847562934L;

    const long TheNextIdAlong = 74857693847562935L;

    [Fact]
    public void ADoubleCannotTellTwoEntityIdsApart()
    {
        // The premise. If this ever fails, the tests below are testing nothing.
        Assert.Equal((double)OneId, (double)TheNextIdAlong);
    }

    [Fact]
    public void TwoWholeNumbersAreComparedAsWholeNumbers()
    {
        var one = EntityValue.Of(OneId);
        var other = EntityValue.Of(TheNextIdAlong);

        Assert.False(Holds(ValueKind.Integer, one, Relation.Is, other));
        Assert.True(Holds(ValueKind.Integer, one, Relation.IsNot, other));
        Assert.True(Holds(ValueKind.Integer, other, Relation.Above, one));
    }

    [Fact]
    public void AWholeNumberKeepsEveryDigit()
    {
        long back;
        Assert.True(_values.TryInteger(ValueKind.Integer, EntityValue.Of(OneId), out back));
        Assert.Equal(OneId, back);
    }

    /// <summary>A real survives sharing one field with whole numbers and colours.</summary>
    [Fact]
    public void ARealKeepsItsValue()
    {
        double back;
        Assert.True(_values.TryReal(ValueKind.Real, EntityValue.Of(0.1), out back));
        Assert.Equal(0.1, back);

        // Negative zero has bits set, and is still nothing.
        Assert.False(_values.Flag(ValueKind.Real, EntityValue.Of(-0.0)));
    }

    /// <summary>A value cannot be read as something it is not, which is what stops silent nonsense.</summary>
    [Fact]
    public void AValueRefusesToBeReadAsAnotherKind()
    {
        var words = _values.Text("Airlock Inner Door");

        Assert.False(_values.TryInteger(ValueKind.Text, words, out _));
        Assert.False(_values.TryReal(ValueKind.Text, words, out _));
        Assert.False(_values.TryColor(ValueKind.Text, words, out _, out _, out _, out _));

        Assert.False(_values.TryText(ValueKind.Color, EntityValue.Of(255, 136, 0, 255), out _));
    }

    [Fact]
    public void AnythingCanBeAskedWhetherItIsEqual()
    {
        foreach (ValueKind kind in new[]
                 {
                     ValueKind.Flag, ValueKind.Integer, ValueKind.Real,
                     ValueKind.Text, ValueKind.Color
                 })
        {
            Assert.True(Relations.Suits(Relation.Is, kind));
            Assert.True(Relations.Suits(Relation.IsNot, kind));
        }
    }

    [Fact]
    public void OnlyANumberCanBeAskedWhetherItIsAbove()
    {
        Assert.True(Relations.Suits(Relation.Above, ValueKind.Integer));
        Assert.True(Relations.Suits(Relation.Below, ValueKind.Real));

        Assert.False(Relations.Suits(Relation.Above, ValueKind.Color));
        Assert.False(Relations.Suits(Relation.Below, ValueKind.Color));

        Assert.False(Relations.Suits(Relation.AtLeast, ValueKind.Text));
        Assert.False(Relations.Suits(Relation.AtMost, ValueKind.Flag));
    }

    [Fact]
    public void AColourIsEqualInEveryComponent()
    {
        var orange = EntityValue.Of(255, 136, 0, 255);

        Assert.True(Holds(ValueKind.Color, orange, Relation.Is, EntityValue.Of(255, 136, 0, 255)));
        Assert.False(Holds(ValueKind.Color, orange, Relation.Is, EntityValue.Of(255, 136, 0, 128)));
    }

    /// <summary>An ordering asked of something with no ordering answers false rather than throwing.</summary>
    /// <remarks>
    /// <see cref="Relations.Suits" /> is where that combination is refused. Refusing again here would
    /// turn a question nobody should have asked into a crash in the middle of somebody's airlock.
    /// </remarks>
    [Fact]
    public void AnOrderingAskedOfAColourIsSimplyFalse()
    {
        var orange = EntityValue.Of(255, 136, 0, 255);
        var black = EntityValue.Of(0, 0, 0, 255);

        Assert.False(Holds(ValueKind.Color, orange, Relation.Above, black));
        Assert.False(Holds(ValueKind.Color, orange, Relation.Below, black));
    }

    [Fact]
    public void AFlagStillReadsAsOneOrZero()
    {
        // Both sides of a comparison share one kind now, because the kind comes from the declaration
        // rather than from either value - so a flag is measured against a flag, and reads one or zero
        // on the way through.
        Assert.Equal(1.0, _values.AsNumber(ValueKind.Flag, EntityValue.Of(true)));
        Assert.Equal(0.0, _values.AsNumber(ValueKind.Flag, EntityValue.Of(false)));

        Assert.True(Holds(ValueKind.Flag, EntityValue.Of(true), Relation.Is, EntityValue.Of(true)));
        Assert.True(Holds(ValueKind.Flag, EntityValue.Of(true), Relation.IsNot, EntityValue.Of(false)));
    }

    /// <summary>
    /// A flag carries no words, because the words for true and false are the host's.
    /// </summary>
    /// <remarks>
    /// It used to answer "yes" and "no", which is core holding English - and worse, English a text
    /// comparison could match against, so a state declared against the word "yes" behaved differently
    /// from one declared against true.
    /// </remarks>
    [Fact]
    public void AFlagCarriesNoWords()
    {
        Assert.Equal("", _values.AsText(ValueKind.Flag, EntityValue.Of(true)));
        Assert.Equal("", _values.AsText(ValueKind.Flag, EntityValue.Of(false)));
    }

    [Fact]
    public void EverythingAnswersWhetherThereIsAnythingThere()
    {
        Assert.True(_values.Flag(ValueKind.Flag, EntityValue.Of(true)));
        Assert.False(_values.Flag(ValueKind.Flag, EntityValue.Of(false)));

        Assert.True(_values.Flag(ValueKind.Integer, EntityValue.Of(5L)));
        Assert.False(_values.Flag(ValueKind.Integer, EntityValue.Of(0L)));

        Assert.True(_values.Flag(ValueKind.Text, _values.Text("something")));
        Assert.False(_values.Flag(ValueKind.Text, _values.Text("")));

        Assert.True(_values.Flag(ValueKind.Color, EntityValue.Of(0, 0, 0, 255)));
        Assert.False(_values.Flag(ValueKind.Color, EntityValue.Of(255, 255, 255, 0)));
    }

    /// <summary>
    /// A reading writes into the slot it already owns, so the list stops growing.
    /// </summary>
    /// <remarks>
    /// The whole reason wide values are slots rather than references. A property read every tick would
    /// otherwise take a new slot sixty times a second, which is the allocation this design exists to
    /// avoid, wearing a different hat.
    /// </remarks>
    [Fact]
    public void AReadingKeepsItsSlot()
    {
        var slot = _values.Text("main");
        var again = _values.Text(slot, "spare");

        Assert.Equal("spare", _values.AsText(ValueKind.Text, again));
        Assert.Equal("spare", _values.AsText(ValueKind.Text, slot));
    }

    bool Holds(ValueKind kind, EntityValue reading, Relation relation, EntityValue operand)
    {
        return Relations.Holds(_values, kind, reading, relation, operand);
    }
}
