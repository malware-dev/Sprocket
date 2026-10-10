using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.GUI.TextPanel;
using Xunit;
using CameraBlock = Sprocket.Host.SpaceEngineers.Blocks.Camera;
using EmotionController = Sprocket.Host.SpaceEngineers.Blocks.EmotionController;
using TextPanel = Sprocket.Host.SpaceEngineers.Blocks.TextPanel;

namespace Sprocket.Tests;

/// <summary>What a text panel shows and an emotion controller's face, and a camera's viewer.</summary>
public class ScreenBlockTests
{
    /// <summary>Showing an image replaces what the panel shows with it, and has it show text and images.</summary>
    [Fact]
    public void ShowingAnImageReplacesTheImagesShown()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        panel.Content = ContentType.SCRIPT;

        Ran.Step(ship, panel, TextPanel.ShowImage, Literal.Of("Arrow"));
        Ran.Step(ship, panel, TextPanel.ShowImage, Literal.Of("Danger"));

        Assert.Equal(new[] { "Danger" }, panel.Images);
        Assert.Equal(ContentType.TEXT_AND_IMAGE, panel.Content);
    }

    /// <summary>The text a panel shows and the app it runs are read off it.</summary>
    [Fact]
    public void WhatThePanelShowsIsRead()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        panel.Text = "DOCKED";
        panel.App = "TSS_Weather";

        Assert.Equal("DOCKED", Ran.Read(ship, panel, TextPanel.Shown).Text);
        var app = ship.Entity(panel.Id).Kind.FindAction(TextPanel.App, Form.Value)!.Being!;
        Assert.Equal(new[] { "TSS_ClockAnalog", "TSS_Weather" }, app.Names);
        Assert.True(app.Is(Literal.Name("TSS_Weather")).Holds(panel.Block, null!));
    }

    /// <summary>The panel's settings are said in Sprocket's words, and its title is left to the terminal.</summary>
    [Fact]
    public void ThePanelsSettingsAreSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        panel.Carrying("Title", "Title", ValueKind.Text)
            .Carrying("FontColor", "Font Color", ValueKind.Color)
            .Carrying("ScriptForegroundColor", "Font Color", ValueKind.Color)
            .Carrying("PreserveAspectRatio", "Preserve aspect ratio", ValueKind.Flag)
            .Offering("PreserveAspectRatio", "Preserve aspect ratio On/Off");
        var kind = ship.Entity(panel.Id).Kind;

        Assert.True(kind.FindAction("Title", Form.Value)!.Hidden);
        Assert.Equal("text color", kind.FindAction("FontColor", Form.Value)!.Text);
        Assert.Equal("app text color", kind.FindAction("ScriptForegroundColor", Form.Value)!.Text);
        Assert.Equal("keeps aspect ratio", kind.FindAction("PreserveAspectRatio", Form.Value)!.Text);
        Assert.Equal("toggle keeps aspect ratio", kind.FindAction("PreserveAspectRatio", Form.Action)!.Text);
    }

    /// <summary>Showing a face offers every face the block has by its own name, and runs the game's action for the one chosen.</summary>
    [Fact]
    public void ShowingAFaceRunsThatFacesAction()
    {
        var ship = new Rigged();
        var faces = ship.Rig.EmotionController(1, "Face");
        var kind = ship.Entity(faces.Id).Kind;

        var fields = new List<Field>();
        kind.FindAction(EmotionController.ShowFace, Form.Native)!.Fields(fields);
        var face = Assert.Single(fields);
        Assert.Equal(new[] { "Happy", "Sad" }, face.Words);
        Assert.True(kind.FindAction(@"Textures\Models\Emotes\Sad.dds", Form.Action)!.Hidden);
        Assert.False(kind.FindAction("Blink", Form.Action)!.Hidden);

        Ran.Step(ship, faces, EmotionController.ShowFace, Literal.Of(@"Textures\Models\Emotes\Sad.dds"));

        Assert.Contains(@"Face:Textures\Models\Emotes\Sad.dds", ship.Rig.Log);
    }

    /// <summary>A camera is in use while anybody looks through it, and its panning is said as a state with its toggle.</summary>
    [Fact]
    public void ACameraIsInUseWhileAnybodyLooks()
    {
        var ship = new Rigged();
        var camera = ship.Rig.Camera(1, "Camera");
        camera.Carrying("EnablePanning", "Enable panning", ValueKind.Flag).Offering("EnablePanning", "Panning On/Off");

        Assert.Equal(Literal.Of(false), Ran.Read(ship, camera, CameraBlock.InUse));
        camera.Watched = true;
        Assert.Equal(Literal.Of(true), Ran.Read(ship, camera, CameraBlock.InUse));

        var kind = ship.Entity(camera.Id).Kind;
        Assert.Equal("panning allowed", kind.FindAction("EnablePanning", Form.Value)!.Text);
        Assert.Equal("toggle panning allowed", kind.FindAction("EnablePanning", Form.Action)!.Text);
    }
}
