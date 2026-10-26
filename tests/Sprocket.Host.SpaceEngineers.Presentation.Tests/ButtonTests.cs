using Lumel.Input;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A button: its words or content given to it on its fill, styled whole, and pressed whole.</summary>
public class ButtonTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Accent = new Color(142, 188, 207);
    static readonly Color Chrome = new Color(27, 36, 42);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Well = new Color(55, 72, 81);
    static readonly Color Faint = new Color(133, 161, 169);

    static readonly ButtonStyle Go = new ButtonStyle
    {
        Base = { Background = Accent, Color = Chrome, Padding = new Thickness(16f, 8f), FontSize = 24f },
        Hot = { Background = Ink },
        Disabled = { Background = Well, Color = Faint }
    };

    /// <summary>A button's style fills the button and colors its words.</summary>
    [Fact]
    public void ItsStyleFillsItAndColorsItsWords()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal(Accent, button.Background);
        Assert.Equal(Chrome, Words(easel).Color);
    }

    /// <summary>Its words sit inside its padding, and the button is as big as they are with it.</summary>
    [Fact]
    public void ItsWordsSitInsideItsPadding()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new HStack { Children = { button } }, Wide, High);

        Assert.Equal(24f + 16f, button.DesiredSize.Y, 3);
        Assert.Equal(button.Bounds.X + 16f, Words(easel).Position.X / easel.Pixels, 2);
    }

    /// <summary>Given more room than its words need, it puts them in the middle of it.</summary>
    [Fact]
    public void ItsWordsSitInTheMiddleOfMoreRoom()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        var words = button.DesiredSize.X - 32f;
        Assert.Equal(button.Bounds.X + (button.Bounds.Width - words) / 2f, Words(easel).Position.X / easel.Pixels, 2);
    }

    /// <summary>Hot, it takes its hot fill; left, it goes back.</summary>
    [Fact]
    public void HotItTakesItsHotFill()
    {
        var button = new Button { Text = "Done", Style = Go };

        button.IsHot = true;
        Assert.Equal(Ink, button.Background);

        button.IsHot = false;
        Assert.Equal(Accent, button.Background);
    }

    /// <summary>Disabled, it looks disabled whether or not it is hot, words and all.</summary>
    [Fact]
    public void DisabledItLooksDisabledEvenWhenHot()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        button.IsHot = true;
        button.IsEnabled = false;
        easel.Draw(Wide, High);

        Assert.Equal(Well, button.Background);
        Assert.Equal(Faint, Words(easel).Color);
    }

    /// <summary>A press on its words reaches the button, not the words.</summary>
    [Fact]
    public void APressOnItsWordsReachesTheButton()
    {
        var button = new Button { Text = "Done", Style = Go, Command = new Relay(() => { }) };
        _ = new Easel(new VStack { Children = { button } }, Wide, High);

        var middle = new Vector2(button.Bounds.X + button.Bounds.Width / 2f, button.Bounds.Y + button.Bounds.Height / 2f);
        Assert.Same(button, button.Under(middle));
    }

    /// <summary>Going hot and back measures no text, since only colors change.</summary>
    [Fact]
    public void GoingHotMeasuresNoText()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);
        var settled = easel.Measured;

        button.IsHot = true;
        easel.Draw(Wide, High);
        button.IsHot = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>Content given to it takes the place of its words, and its style leaves that content as it was given.</summary>
    [Fact]
    public void GivenContentTakesThePlaceOfItsWords()
    {
        var given = new Lumel.Text.Label { Text = "Given", Color = Faint };
        var button = new Button { Text = "Done", Style = Go };
        button.Content = given;
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal("", button.Text);
        Assert.Equal("Given", Words(easel).Text);
        Assert.Equal(Faint, Words(easel).Color);
    }

    /// <summary>Given words again, it shows its own words in place of the content it was given.</summary>
    [Fact]
    public void GivenWordsItShowsItsOwnAgain()
    {
        var button = new Button { Style = Go, Content = new Lumel.Text.Label { Text = "Given" } };
        button.Text = "Done";
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal("Done", button.Text);
        Assert.Equal("Done", Words(easel).Text);
        Assert.Equal(Chrome, Words(easel).Color);
    }

    /// <summary>A press on content given to it reaches the button, not the content.</summary>
    [Fact]
    public void APressOnGivenContentReachesTheButton()
    {
        var given = new HStack { Children = { new Lumel.Text.Label { Text = "Given" } } };
        var button = new Button { Style = Go, Content = given, Command = new Relay(() => { }) };
        _ = new Easel(new VStack { Children = { button } }, Wide, High);

        var middle = new Vector2(given.Bounds.X + given.Bounds.Width / 2f, given.Bounds.Y + given.Bounds.Height / 2f);
        Assert.Same(button, button.Under(middle));
    }

    /// <summary>Content given to it sits in the middle of the room inside its padding.</summary>
    [Fact]
    public void GivenContentSitsInTheMiddle()
    {
        var given = new Lumel.Text.Label { Text = "Given" };
        var button = new Button { Style = Go, Content = given };
        _ = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal(button.Bounds.X + button.Bounds.Width / 2f, given.Bounds.X + given.Bounds.Width / 2f, 2);
        Assert.Equal(button.Bounds.Y + button.Bounds.Height / 2f, given.Bounds.Y + given.Bounds.Height / 2f, 2);
    }

    static DrawCommand Words(Easel easel)
    {
        return Assert.Single(easel.Written);
    }
}
