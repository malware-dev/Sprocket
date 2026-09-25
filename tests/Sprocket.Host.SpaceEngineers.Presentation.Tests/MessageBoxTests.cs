using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Rich;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Windows;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A message box: its standard buttons in the game's order, each with whatever words it is given, and Enter and Escape doing what it says.</summary>
public class MessageBoxTests
{
    /// <summary>The buttons stand Yes, No, Cancel from left to right, each with its own words.</summary>
    [Fact]
    public void TheButtonsStandInTheGamesOrder()
    {
        var box = Box(out _);
        box.YesText = "Replace it";
        box.NoText = "Leave it alone";
        box.CancelText = "Not now";
        Laid(box);

        var buttons = Buttons(box);

        Assert.Equal(new[] { "Replace it", "Leave it alone", "Not now" }, buttons.Select(b => b.Text));
        Assert.True(buttons[0].Bounds.X < buttons[1].Bounds.X && buttons[1].Bounds.X < buttons[2].Bounds.X);
    }

    /// <summary>A button with nothing to do is not shown, and Cancel can be left to Escape alone.</summary>
    [Fact]
    public void OnlyButtonsWithSomethingToDoAreShown()
    {
        var box = Box(out _);
        box.NoCommand = null;
        box.ShowsCancel = false;
        Laid(box);

        Assert.Equal(new[] { "Yes" }, Buttons(box).Select(b => b.Text));
        Assert.True(box.TakeCancel(), "Escape no longer cancels");
    }

    /// <summary>Enter gives the default answer, and with no default Enter gives nothing.</summary>
    [Theory]
    [InlineData(MessageButton.Yes, "yes")]
    [InlineData(MessageButton.No, "no")]
    [InlineData(MessageButton.Cancel, "cancel")]
    [InlineData(null, "")]
    public void EnterGivesTheDefault(MessageButton? given, string answered)
    {
        var box = Box(out var said);
        box.Default = given;

        Assert.Equal(given.HasValue, box.TakeAccept());
        Assert.Equal(answered, said.ToString());
    }

    /// <summary>The default answer looks like the default; the others look alike.</summary>
    [Fact]
    public void TheDefaultStandsOut()
    {
        var plain = new ButtonStyle { Base = { Edge = Color.Gray } }.Frozen();
        var standing = new ButtonStyle { Base = { Background = Color.White } }.Frozen();
        var box = Box(out _);
        box.ButtonStyle = plain;
        box.DefaultStyle = standing;
        box.Default = MessageButton.No;
        Laid(box);

        var buttons = Buttons(box);

        Assert.Same(plain, buttons[0].Style);
        Assert.Same(standing, buttons[1].Style);
        Assert.Same(plain, buttons[2].Style);
    }

    static MessageBox Box(out System.Text.StringBuilder said)
    {
        var heard = new System.Text.StringBuilder();
        said = heard;

        return new MessageBox
        {
            Message = Document.Plain("Replace the program?"),
            YesCommand = new Relay(() => heard.Append("yes")),
            NoCommand = new Relay(() => heard.Append("no")),
            Cancel = new Relay(() => heard.Append("cancel"))
        };
    }

    static void Laid(MessageBox box)
    {
        var layer = new WindowLayer();
        layer.Open(box);
        layer.Draw(new Counting(), new RectangleF(0f, 0f, 1000f, 800f));
    }

    static List<Button> Buttons(MessageBox box)
    {
        var found = new List<Button>();
        Collect(box, found);
        return found;
    }

    static void Collect(Element element, List<Button> found)
    {
        if (element.Collapsed)
            return;

        if (element is Button button)
            found.Add(button);

        for (var i = 0; i < element.VisualChildCount; i++)
            Collect(element.GetVisualChild(i), found);
    }
}
