using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.GUI.TextPanel;
using Xunit;
using CameraBlock = Sprocket.Host.SpaceEngineers.Blocks.Camera;
using EmotionController = Sprocket.Host.SpaceEngineers.Blocks.EmotionController;
using TextPanel = Sprocket.Host.SpaceEngineers.Blocks.TextPanel;
using HostTypes = Sprocket.Host.SpaceEngineers.Blocks.HostTypes;
using ScreenType = Sprocket.Host.SpaceEngineers.Blocks.ScreenType;

namespace Sprocket.Tests;

/// <summary>What a text panel shows and an emotion controller's face, and a camera's viewer.</summary>
public class ScreenBlockTests
{
    /// <summary>A value of a few names a person reads by other words shows those words, and the program keeps the names.</summary>
    [Fact]
    public void AValueReadByOtherWordsKeepsItsNames()
    {
        var kind = new EntityKind("Test/Screen", "Screen");
        kind.WithValue("app", "app", OwnValue.OneOf<IMyTerminalBlock>(new[] { "TSS_ClockAnalog", "TSS_Weather" }, _ => 0)
            .Saying(new[] { "Clock Analog", "Weather" }));
        var fields = new List<Field>();
        kind.FindAction("app", ReachKind.Value)!.Fields(fields);

        var field = Assert.Single(fields);
        Assert.Equal(new[] { "Clock Analog", "Weather" }, field.Words);
        Assert.Equal(new[] { "TSS_ClockAnalog", "TSS_Weather" }, field.Means);
    }

    /// <summary>An app the game names none for is read by its id.</summary>
    [Fact]
    public void AnAppTheGameNamesNoneForIsItsId()
    {
        Assert.Equal("TSS_Unknown", ScreenType.AppName("TSS_Unknown"));
    }

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
        var app = ship.Entity(panel.Id).Kind.FindAction(TextPanel.App, ReachKind.Value)!.OwnValue!;
        Assert.Equal(new[] { "TSS_ClockAnalog", "TSS_Weather" }, app.Names);
        Assert.True(app.Is(Literal.Name("TSS_Weather")).Holds(panel.Block, null!));
    }

    /// <summary>
    /// A panel's settings are its one screen's, read and set as any block's screen is, and its title is left to the terminal.
    /// </summary>
    [Fact]
    public void APanelsSettingsAreItsScreens()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Screen");
        panel.Carrying("Title", "Title", ValueKind.Text)
            .Carrying("FontColor", "Font Color", ValueKind.Color)
            .Carrying("ScriptForegroundColor", "Font Color", ValueKind.Color);
        var kind = ship.Entity(panel.Id).Kind;
        var screen = HostTypes.Returned("screen");

        Assert.True(kind.FindAction("Title", ReachKind.Value)!.Hidden);
        Assert.True(kind.FindAction("FontColor", ReachKind.Value)!.Hidden);
        Assert.True(kind.FindAction("ScriptForegroundColor", ReachKind.Value)!.Hidden);
        Assert.Same(screen, kind.FindAction("text-color", ReachKind.Value)!.Type);
        Assert.Same(screen, kind.FindAction("app-color", ReachKind.Value)!.Type);
        Assert.Same(screen, kind.FindAction(ScreenType.ShowText, ReachKind.Native)!.Type);
        Assert.Single(kind.Facets(screen!));
    }

    /// <summary>
    /// A text panel vouches for what it shows, its rotation and its screen's members, and not for its title, the game's own
    /// screen settings its screen reads in their place, or the game's steps and pairs that only say a setting again.
    /// </summary>
    [Fact]
    public void ATextPanelVouchesForItsScreenAndNotTheGamesSettings()
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

        Assert.True(kind.FindAction(TextPanel.ShowImage, ReachKind.Native)!.Vetted);
        Assert.True(kind.FindAction(TextPanel.Shown, ReachKind.Value)!.Vetted);
        Assert.True(kind.FindAction(TextPanel.App, ReachKind.Value)!.Vetted);
        Assert.True(kind.FindAction("Rotate", ReachKind.Value)!.Vetted);
        Assert.True(kind.FindAction(ScreenType.ShowText, ReachKind.Native)!.Vetted);
        foreach (var id in new[] { "text-color", "background-color", "app-color", "content", "font", "font-size", "alignment", "padding" })
            Assert.True(kind.FindAction(id, ReachKind.Value)!.Vetted, id + " is not vetted");

        foreach (var id in colors.Concat(layout).Append("PreserveAspectRatio").Where(id => id != "Rotate"))
            Assert.False(EntityAction.Value(kind.Actions, "", id)?.Vetted ?? false, id + " is vetted");

        Assert.False(kind.FindAction("PreserveAspectRatio", ReachKind.Action)!.Vetted);

        Assert.False(kind.FindAction("Title", ReachKind.Value)!.Vetted);
        foreach (var id in new[]
                 {
                     "PreserveAspectRatio_On", "PreserveAspectRatio_Off", "IncreaseFontSize", "DecreaseTextPaddingSlider",
                     "IncreaseChangeIntervalSlider"
                 })
            Assert.False(kind.FindAction(id, ReachKind.Action)!.Vetted, id + " is vetted");
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

        Assert.True(kind.FindAction(EmotionController.ShowFace, ReachKind.Native)!.Vetted);
        Assert.False(kind.FindAction(@"Textures\Models\Emotes\Happy.dds", ReachKind.Action)!.Vetted);
        Assert.False(kind.FindAction(@"Textures\Models\Emotes\Sad.dds", ReachKind.Action)!.Vetted);
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

        Assert.True(kind.FindAction(CameraBlock.InUse, ReachKind.Value)!.Vetted);
        Assert.True(kind.FindAction("EnablePanning", ReachKind.Value)!.Vetted);
        Assert.True(kind.FindAction("EnablePanning", ReachKind.Action)!.Vetted);
        Assert.True(kind.FindAction("ViewRotation", ReachKind.Value)!.Vetted);

        Assert.False(kind.FindAction("EnablePanning_On", ReachKind.Action)!.Vetted);
        Assert.False(kind.FindAction("EnablePanning_Off", ReachKind.Action)!.Vetted);
        Assert.False(kind.FindAction("View", ReachKind.Action)!.Vetted);
    }

    /// <summary>Showing a face offers every face the block has by its own name, and runs the game's action for the one chosen.</summary>
    [Fact]
    public void ShowingAFaceRunsThatFacesAction()
    {
        var ship = new Rigged();
        var faces = ship.Rig.EmotionController(1, "Face");
        var kind = ship.Entity(faces.Id).Kind;

        var fields = new List<Field>();
        kind.FindAction(EmotionController.ShowFace, ReachKind.Native)!.Fields(fields);
        var face = Assert.Single(fields);
        Assert.Equal(new[] { "Happy", "Sad" }, face.Words);
        Assert.True(kind.FindAction(@"Textures\Models\Emotes\Sad.dds", ReachKind.Action)!.Hidden);
        Assert.False(kind.FindAction("Blink", ReachKind.Action)!.Hidden);

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
        Assert.Equal("panning", kind.FindAction("EnablePanning", ReachKind.Value)!.Text);
        Assert.Equal("toggle panning", kind.FindAction("EnablePanning", ReachKind.Action)!.Text);
    }
}
