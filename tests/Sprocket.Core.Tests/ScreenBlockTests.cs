using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Runtime;
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
        var app = ship.Entity(panel.Id).Kind.FindAction(TextPanel.App, Nature.Value)!.Being!;
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

        Assert.True(kind.FindAction("Title", Nature.Value)!.Hidden);
        Assert.Equal("text color", kind.FindAction("FontColor", Nature.Value)!.Text);
        Assert.Equal("app color", kind.FindAction("ScriptForegroundColor", Nature.Value)!.Text);
        Assert.Equal("preserve aspect ratio", kind.FindAction("PreserveAspectRatio", Nature.Value)!.Text);
    }

    /// <summary>
    /// A text panel vouches for what it shows and every setting it offers, and not for its title, the game's steps and
    /// pairs that only say a setting again, or the toggle that flips whether it keeps the aspect ratio.
    /// </summary>
    [Fact]
    public void ATextPanelVouchesForItsSettingsAndNotTheirSteps()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        string[] colors = ["FontColor", "BackgroundColor", "ScriptForegroundColor", "ScriptBackgroundColor"];
        string[] layout = ["Content", "Font", "FontSize", "alignment", "TextPaddingSlider", "Rotate", "ChangeIntervalSlider"];
        foreach (var id in colors)
            panel.Carrying(id, id, ValueKind.Color);

        foreach (var id in layout)
            panel.Carrying(id, id, ValueKind.Real);

        panel.Carrying("Title", "Title", ValueKind.Text)
            .Carrying("PreserveAspectRatio", "Preserve aspect ratio", ValueKind.Flag)
            .Offering("PreserveAspectRatio", "Preserve aspect ratio On/Off")
            .Offering("PreserveAspectRatio_On", "Preserve aspect ratio On")
            .Offering("PreserveAspectRatio_Off", "Preserve aspect ratio Off")
            .Offering("IncreaseFontSize", "Increase Font Size")
            .Offering("DecreaseTextPaddingSlider", "Decrease Text padding")
            .Offering("IncreaseChangeIntervalSlider", "Increase Image change interval");
        var kind = ship.Entity(panel.Id).Kind;

        Assert.True(kind.FindAction(TextPanel.ShowImage, Nature.Native)!.Vetted);
        Assert.True(kind.FindAction(TextPanel.Shown, Nature.Value)!.Vetted);
        Assert.True(kind.FindAction(TextPanel.App, Nature.Value)!.Vetted);
        foreach (var id in colors.Concat(layout).Append("PreserveAspectRatio"))
            Assert.True(kind.FindAction(id, Nature.Value)!.Vetted, id + " is not vetted");

        Assert.False(kind.FindAction("PreserveAspectRatio", Nature.Action)!.Vetted);

        Assert.False(kind.FindAction("Title", Nature.Value)!.Vetted);
        foreach (var id in new[]
                 {
                     "PreserveAspectRatio_On", "PreserveAspectRatio_Off", "IncreaseFontSize", "DecreaseTextPaddingSlider",
                     "IncreaseChangeIntervalSlider"
                 })
            Assert.False(kind.FindAction(id, Nature.Action)!.Vetted, id + " is vetted");
    }

    /// <summary>A step chooses the app a panel runs, and leaves what the panel shows as it was.</summary>
    [Fact]
    public void AStepChoosesThePanelsApp()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        panel.Content = ContentType.TEXT_AND_IMAGE;

        Assert.Equal(SequenceProblem.None, Ran.Set(ship, panel, TextPanel.App, Literal.Name("TSS_Weather")));

        Assert.Equal("TSS_Weather", panel.App);
        Assert.Equal(ContentType.TEXT_AND_IMAGE, panel.Content);
    }

    /// <summary>An app the panel does not have is a fault, and leaves the app it runs as it was.</summary>
    [Fact]
    public void AnAppThePanelLacksIsAFault()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        panel.App = "TSS_ClockAnalog";

        Assert.Equal(SequenceProblem.Mistyped, Ran.Set(ship, panel, TextPanel.App, Literal.Name("TSS_Horizon")));

        Assert.Equal("TSS_ClockAnalog", panel.App);
    }

    /// <summary>An emotion controller vouches for showing a face, and not for the game's own action for each face.</summary>
    [Fact]
    public void AnEmotionControllerVouchesForShowingAFaceAlone()
    {
        var ship = new Rigged();
        var faces = ship.Rig.EmotionController(1, "Face");
        var kind = ship.Entity(faces.Id).Kind;

        Assert.True(kind.FindAction(EmotionController.ShowFace, Nature.Native)!.Vetted);
        Assert.False(kind.FindAction(@"Textures\Models\Emotes\Happy.dds", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction(@"Textures\Models\Emotes\Sad.dds", Nature.Action)!.Vetted);
    }

    /// <summary>
    /// A camera vouches for whether it is in use, its panning with its toggle and its view rotation, and not for the panning
    /// pair or looking through it.
    /// </summary>
    [Fact]
    public void ACameraVouchesForItsViewSettings()
    {
        var ship = new Rigged();
        var camera = ship.Rig.Camera(1, "Camera");
        camera.Carrying("EnablePanning", "Enable panning", ValueKind.Flag)
            .Carrying("ViewRotation", "View rotation", ValueKind.Real)
            .Offering("EnablePanning", "Panning On/Off")
            .Offering("EnablePanning_On", "Panning On")
            .Offering("EnablePanning_Off", "Panning Off")
            .Offering("View", "View");
        var kind = ship.Entity(camera.Id).Kind;

        Assert.True(kind.FindAction(CameraBlock.InUse, Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("EnablePanning", Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("EnablePanning", Nature.Action)!.Vetted);
        Assert.True(kind.FindAction("ViewRotation", Nature.Value)!.Vetted);

        Assert.False(kind.FindAction("EnablePanning_On", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("EnablePanning_Off", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("View", Nature.Action)!.Vetted);
    }

    /// <summary>Showing a face offers every face the block has by its own name, and runs the game's action for the one chosen.</summary>
    [Fact]
    public void ShowingAFaceRunsThatFacesAction()
    {
        var ship = new Rigged();
        var faces = ship.Rig.EmotionController(1, "Face");
        var kind = ship.Entity(faces.Id).Kind;

        var fields = new List<Field>();
        kind.FindAction(EmotionController.ShowFace, Nature.Native)!.Fields(fields);
        var face = Assert.Single(fields);
        Assert.Equal(new[] { "Happy", "Sad" }, face.Words);
        Assert.True(kind.FindAction(@"Textures\Models\Emotes\Sad.dds", Nature.Action)!.Hidden);
        Assert.False(kind.FindAction("Blink", Nature.Action)!.Hidden);

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
        Assert.Equal("panning", kind.FindAction("EnablePanning", Nature.Value)!.Text);
        Assert.Equal("toggle panning", kind.FindAction("EnablePanning", Nature.Action)!.Text);
    }
}
