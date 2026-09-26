using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Sim.Simulation;
using VRageMath;
using Xunit;
using Sprocket.Host.SpaceEngineers.Ui.Viewing;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Containers and padding: room inside what holds others and inside the leaves that have a use for it, and clipping where it belongs.</summary>
public class ContainerTests
{
    const float Wide = 1440f;
    const float High = 1080f;

    /// <summary>A container's padding is room its children are placed inside.</summary>
    [Fact]
    public void AContainersPaddingIsRoomInsideIt()
    {
        var child = new Label { Placement = Fixed.Size(100f, 40f) };
        var holder = new VStack { Padding = new Thickness(10f, 20f), Children = { child } };
        _ = new Easel(new VStack { Children = { holder } }, Wide, High);

        Assert.Equal(new RectangleF(10f, 20f, 100f, 40f), child.Bounds);
        Assert.Equal(new Vector2(120f, 80f), holder.DesiredSize);
    }

    /// <summary>A style's padding reaches whatever it styles that has padding: a leaf and a container alike.</summary>
    [Fact]
    public void AStylesPaddingReachesLeavesAndContainers()
    {
        var style = new ElementStyle { Base = { Padding = new Thickness(6f) } };
        var text = new TextStyle { Base = { Padding = new Thickness(4f) } };

        var holder = new VStack { Style = style };
        var words = new Label { Style = text };

        Assert.Equal(new Thickness(6f), holder.Padding);
        Assert.Equal(new Thickness(4f), words.Padding);
    }

    /// <summary>A container does not clip: what it holds may draw outside it.</summary>
    [Fact]
    public void AContainerDoesNotClip()
    {
        var holder = new VStack { Children = { new Label { Text = "Inside" } } };
        var easel = new Easel(new VStack { Children = { holder } }, Wide, High);

        Assert.DoesNotContain(easel.Commands, command => command.Kind == DrawKind.SetClip);
    }

    /// <summary>A content control holds one piece of content: given another, it lets go of the first.</summary>
    [Fact]
    public void AContentControlHoldsOneThing()
    {
        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        var reveal = new Reveal { Content = first, Openness = 1f };

        reveal.Content = second;

        Assert.Null(first.Parent);
        Assert.Same(reveal, second.Parent);
        Assert.Same(second, reveal.Content);
    }

    /// <summary>A content control's content fills the room inside its padding, unless the kind of control says otherwise.</summary>
    [Fact]
    public void ContentFillsTheRoomInsidePadding()
    {
        var content = new Label { Text = "Held" };
        var window = new Ui.Windows.Window
        {
            Padding = new Thickness(8f),
            Content = content,
            Placement = Fixed.Size(200f, 100f)
        };
        _ = new Easel(new VStack { Children = { window } }, Wide, High);

        Assert.Equal(new RectangleF(8f, 8f, 184f, 84f), content.Bounds);
    }

    /// <summary>What must clip to work does so on its own, with nothing to switch.</summary>
    [Fact]
    public void WhatMustClipClipsOnItsOwn()
    {
        var scroll = new ScrollView { Placement = Fixed.Size(200f, 100f), Content = new Label { Text = "Held" } };
        var easel = new Easel(new VStack { Children = { scroll } }, Wide, High);
        Assert.Contains(easel.Commands, command => command.Kind == DrawKind.SetClip);

        var box = new TextBox { Placement = Fixed.Size(200f, 40f), Text = "Typed" };
        easel = new Easel(new VStack { Children = { box } }, Wide, High);
        Assert.Contains(easel.Commands, command => command.Kind == DrawKind.SetClip);
    }
}
