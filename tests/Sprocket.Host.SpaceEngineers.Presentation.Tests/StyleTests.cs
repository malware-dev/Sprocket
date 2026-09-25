using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Game;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>How a style is applied to an element, layer by layer, and held still once frozen.</summary>
public class StyleTests
{
    static readonly Color Panel = new Color(41, 54, 62);
    static readonly Color PanelHot = new Color(60, 76, 82);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Dim = new Color(95, 114, 120);

    /// <summary>A style applies its base, and the neutral values fill in what it leaves out.</summary>
    [Fact]
    public void AStyleFillsWhatItLeavesOutWithTheNeutralValues()
    {
        var label = new Label { Padding = new Thickness(5f), Style = new TextStyle { Base = { Color = Ink } } };

        Assert.Equal(Ink, label.Color);
        Assert.Equal(MyFontEnum.White, label.Font);
        Assert.Equal(24f, label.FontSize);
        Assert.Equal(Color.Transparent, label.Background);
        Assert.True(label.Padding.IsEmpty);
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

    /// <summary>A frozen style refuses any change, to its base and to its states alike.</summary>
    [Fact]
    public void AFrozenStyleRefusesAChange()
    {
        var style = Row();

        Assert.True(style.IsFrozen);
        Assert.Throws<InvalidOperationException>(() => style.Base.Background = Dim);
        Assert.Throws<InvalidOperationException>(() => style.Hot.Background = Panel);
    }

    /// <summary>A style based on another starts from a copy of its base and its states, and changing it leaves the base as it was.</summary>
    [Fact]
    public void AStyleBasedOnAnotherStartsFromACopy()
    {
        var row = Row();
        var quiet = new Pressable(row) { Base = { Background = Dim } }.Frozen();

        Assert.Equal(Dim, quiet.Base.Background);
        Assert.Equal(PanelHot, quiet.Hot.Background);
        Assert.Equal(Dim, quiet.Disabled.Edge);
        Assert.Equal(Panel, row.Base.Background);
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
        }.Frozen();
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
