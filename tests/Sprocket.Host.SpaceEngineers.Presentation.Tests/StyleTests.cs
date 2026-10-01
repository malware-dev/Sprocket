using Lumel;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>How a style is applied to an element, layer by layer, over the style it is based on.</summary>
public class StyleTests
{
    static readonly Color Panel = new Color(41, 54, 62);
    static readonly Color PanelHot = new Color(60, 76, 82);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Dim = new Color(95, 114, 120);

    /// <summary>A style writes what it sets and nothing else, so what a place sets that the style leaves alone stays.</summary>
    [Fact]
    public void AStyleLeavesAloneWhatItDoesNotSet()
    {
        var label = new Label
        {
            Font = "Monospace", Padding = new Thickness(5f), Style = new TextStyle { Base = { Color = Ink } }
        };

        Assert.Equal(Ink, label.Color);
        Assert.Equal("Monospace", label.Font);
        Assert.Equal(24f, label.FontSize);
        Assert.Equal(new Thickness(5f), label.Padding);
    }

    /// <summary>What a place sets and the style leaves alone outlasts a change of state, which applies the style again.</summary>
    [Fact]
    public void WhatAPlaceSetsOutlastsAChangeOfState()
    {
        var row = new HStack { Style = Row(), Padding = new Thickness(0f, 0f, 0f, 4f) };

        row.IsHot = true;
        row.IsHot = false;

        Assert.Equal(new Thickness(0f, 0f, 0f, 4f), row.Padding);
    }

    /// <summary>A state that has ended takes back what it set, where the base sets nothing of its own.</summary>
    [Fact]
    public void AnEndedStateTakesBackWhatItSet()
    {
        var row = new HStack { Style = Row() };

        row.IsHot = true;
        row.IsHot = false;

        Assert.Equal(Panel, row.Background);

        row.IsEnabled = false;
        row.IsEnabled = true;

        Assert.Equal(Color.Transparent, row.Edge);
    }

    /// <summary>An element given another style loses what the old one set and the new one does not.</summary>
    [Fact]
    public void AReplacedStyleTakesBackWhatItSet()
    {
        var label = new Label { Style = new TextStyle { Base = { Color = Ink, Background = Panel } } };

        label.Style = new TextStyle { Base = { Color = Dim } };

        Assert.Equal(Dim, label.Color);
        Assert.Equal(Color.Transparent, label.Background);
    }

    /// <summary>A state the element is in is laid over the base, and a later state wins over an earlier one.</summary>
    [Fact]
    public void ALaterStateWins()
    {
        var row = new HStack { Style = Row() };

        row.IsHot = true;
        Assert.Equal(PanelHot, row.Background);

        row.IsEnabled = false;
        Assert.Equal(Color.Transparent, row.Background);
        Assert.Equal(Dim, row.Edge);
    }

    /// <summary>An element that cannot be used is never hot, whatever the pointer rests on.</summary>
    [Fact]
    public void ADisabledElementTakesNoHotLayer()
    {
        var row = new HStack { Style = new ElementStyle { Hot = { Edge = Ink } } };

        row.IsEnabled = false;
        row.IsHot = true;

        Assert.Equal(Color.Transparent, row.Edge);
    }

    /// <summary>Leaving a state puts back what is beneath it, even a value only the state set.</summary>
    [Fact]
    public void LeavingAStatePutsBackWhatIsBeneathIt()
    {
        var row = new HStack { Style = new Pressable { Hot = { Edge = Ink } } };

        row.IsHot = true;
        Assert.Equal(Ink, row.Edge);

        row.IsHot = false;
        Assert.Equal(Color.Transparent, row.Edge);
    }

    /// <summary>A style based on another is everything the other is, with its own layers laid over it, and leaves the other as it was.</summary>
    [Fact]
    public void AStyleBasedOnAnotherLaysItsOwnOverIt()
    {
        var row = Row();
        var quiet = new Pressable(row) { Base = { Background = Dim } };
        var element = new HStack { Style = quiet };

        Assert.Equal(Dim, element.Background);

        element.IsHot = true;
        Assert.Equal(PanelHot, element.Background);

        element.IsEnabled = false;
        Assert.Equal(Dim, element.Edge);
        Assert.Null(row.Base.Edge);
        Assert.Equal(Panel, row.Base.Background);
    }

    /// <summary>A change to a style shows once the element is restyled, on elements given the style and on those given a style based on it.</summary>
    [Fact]
    public void AChangeToAStyleShowsOnceRestyled()
    {
        var row = Row();
        var plain = new HStack { Style = row };
        var quiet = new HStack { Style = new Pressable(row) { Base = { Edge = Dim } } };
        var screen = new VStack { Children = { plain, quiet } };

        row.Base.Background = Ink;
        Assert.Equal(Panel, plain.Background);

        screen.Restyle();

        Assert.Equal(Ink, plain.Background);
        Assert.Equal(Ink, quiet.Background);
        Assert.Equal(Dim, quiet.Edge);
    }

    /// <summary>A style based on nothing is refused, which is what a style declared above its base receives.</summary>
    [Fact]
    public void AStyleBasedOnNothingIsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => new TextStyle(null!));
    }

    /// <summary>A label with a font size of 24 is one line 24 units tall.</summary>
    [Fact]
    public void AFontSizeIsTheHeightOfALine()
    {
        var label = new Label { Text = "Airlock", FontSize = 24f };
        _ = new Easel(new VStack { Children = { label } }, 400f, 200f);

        Assert.Equal(24f, label.DesiredSize.Y, 3);
    }

    /// <summary>A change of state that only changes colours measures no text.</summary>
    [Fact]
    public void AChangeOfStateInColourOnlyMeasuresNoText()
    {
        var row = new HStack { Style = Row(), Children = { new Label { Text = "Done" } } };
        var easel = new Easel(new VStack { Children = { row } }, 400f, 200f);
        var settled = easel.Measured;

        row.IsHot = true;
        easel.Draw(400f, 200f);
        row.IsHot = false;
        easel.Draw(400f, 200f);

        Assert.Equal(settled, easel.Measured);
    }

    static Pressable Row()
    {
        return new Pressable
        {
            Base = { Background = Panel },
            Hot = { Background = PanelHot },
            Disabled = { Background = Color.Transparent, Edge = Dim }
        };
    }

    /// <summary>A style whose element can be hot and disabled, the way a control's own style declares its states.</summary>
    sealed class Pressable : Style<ElementState>
    {
        public Pressable()
        {
            Hot = Layer();
            Disabled = Layer();
        }

        public Pressable(Pressable basedOn) : base(basedOn)
        {
            Hot = Layer();
            Disabled = Layer();
        }

        public ElementState Hot { get; }

        public ElementState Disabled { get; }

        protected override void LayStates(Element element)
        {
            if (element.IsHot)
                Lay(Hot, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
