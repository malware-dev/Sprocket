using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Xunit;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Tests;

/// <summary>A block with several screens: each member names the screen it means.</summary>
public class ScreensTests
{
    /// <summary>A block with two screens, front and back, as a billboard has.</summary>
    sealed class Billboard : Fitting
    {
        public Billboard(long id, string name)
            : base(id, name, typeof(MyObjectBuilder_LCDPanelsBlock), "LargeBlockBillboard", "Billboard")
        {
            Fit(Pretend.To<IMyFunctionalBlock>()
                .Also<IMyTextSurfaceProvider>()
                .Gives("UseGenericLcd", () => true)
                .Gives("SurfaceCount", () => 2)
                .Does("GetSurface", arguments => Faces[(int)arguments[0]].Surface));
        }

        /// <summary>Its two screens.</summary>
        public Face[] Faces { get; } = [new Face("Front"), new Face("Back")];
    }

    /// <summary>One screen, holding what it is set to.</summary>
    sealed class Face
    {
        public Face(string name)
        {
            Surface = Pretend.To<IMyTextSurface>()
                .Gives("DisplayName", () => name)
                .Does("GetFonts", _ => null!)
                .Does("WriteText", arguments =>
                {
                    Text = (string)arguments[0];
                    return true;
                })
                .Gives("ContentType", () => Content)
                .Takes("ContentType", value => Content = (ContentType)value)
                .Gives("FontSize", () => FontSize)
                .Takes("FontSize", value => FontSize = (float)value)
                .Gives("FontColor", () => FontColor)
                .Takes("FontColor", value => FontColor = (Color)value)
                .Gives("Alignment", () => Alignment)
                .Takes("Alignment", value => Alignment = (TextAlignment)value)
                .Done();
        }

        public IMyTextSurface Surface { get; }

        public string Text { get; private set; } = "";

        public ContentType Content { get; set; }

        public float FontSize { get; set; } = 1f;

        public Color FontColor { get; set; } = Color.White;

        public TextAlignment Alignment { get; set; }
    }

    static (Rigged Ship, Billboard Board) Built()
    {
        var ship = new Rigged();
        return (ship, ship.Rig.Weld(new Billboard(1, "Billboard")));
    }

    /// <summary>Its show text, set up screen and each screen's settings are vetted, and the game's own settings are not.</summary>
    [Fact]
    public void ItsScreenMembersAreVettedAndTheGamesSettingsAreNot()
    {
        var (ship, board) = Built();
        board.Carrying("FontSize", "Font size", ValueKind.Real, "0.1", "10");
        var kind = ship.Entity(board.Id).Kind;

        Assert.True(kind.FindAction(Screen.ShowText, Form.Native)!.Vetted);
        Assert.True(kind.FindAction(Screens.SetUp, Form.Native)!.Vetted);
        foreach (var id in new[] { "content", "text-color", "font-size", "alignment", "keep-aspect-ratio" })
            Assert.True(kind.FindAction(id, Form.Value)!.Vetted, id + " is not vetted");

        Assert.False(kind.FindAction("FontSize", Form.Value)!.Vetted);
    }

    /// <summary>Text is shown on the screen a step names, and that screen is set to show text.</summary>
    [Fact]
    public void TextIsShownOnTheScreenNamed()
    {
        var (ship, board) = Built();
        board.Faces[1].Content = ContentType.SCRIPT;

        Ran.Step(ship, board, Screen.ShowText, Literal.Of("Back"), Literal.Of("Closed"));

        Assert.Equal("", board.Faces[0].Text);
        Assert.Equal("Closed", board.Faces[1].Text);
        Assert.Equal(ContentType.TEXT_AND_IMAGE, board.Faces[1].Content);
    }

    /// <summary>Setting up a screen sets what was given on the screen named and leaves everything left empty.</summary>
    [Fact]
    public void SettingUpAScreenSetsOnlyWhatWasGiven()
    {
        var (ship, board) = Built();
        var empty = Literal.Of("");

        Ran.Step(ship, board, Screens.SetUp, Literal.Of("Back"), empty, Literal.Colour(255, 0, 0, 255), empty, empty,
            Literal.Of(2.5), Literal.Of("center"), empty, empty, empty, empty, empty);

        Assert.Equal(new Color(255, 0, 0, 255), board.Faces[1].FontColor);
        Assert.Equal(2.5f, board.Faces[1].FontSize);
        Assert.Equal(TextAlignment.CENTER, board.Faces[1].Alignment);
        Assert.Equal(1f, board.Faces[0].FontSize);
        Assert.Equal(ContentType.NONE, board.Faces[1].Content);
    }

    /// <summary>A screen's setting is read off the screen a test names.</summary>
    [Fact]
    public void ASettingIsReadOffTheScreenNamed()
    {
        var (ship, board) = Built();
        board.Faces[1].FontSize = 3f;
        board.Faces[1].Alignment = TextAlignment.RIGHT;

        Assert.Equal(3.0, Ran.Read(ship, board, "font-size", Literal.Of("Back")).Real, 3);
        Assert.Equal(1.0, Ran.Read(ship, board, "font-size", Literal.Of("Front")).Real, 3);
        Assert.Equal(2L, Ran.Read(ship, board, "alignment", Literal.Of("Back")).Whole);
    }
}
