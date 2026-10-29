using System.IO;
using System.Windows.Media.Imaging;
using Lumel;
using Lumel.Controls;
using Lumel.Screens;
using Lumel.Sim;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Content;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A screen rendered to a picture with no window, from content registered by the folder a mod is in.</summary>
public class SimShotTests
{
    /// <summary>A workspace is written as a picture of the size asked for, with something drawn on it.</summary>
    [Fact]
    public void AWorkspaceIsWrittenAsAPicture()
    {
        var file = Path.Combine(Path.GetTempPath(), "SimShotTests-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            OnSta(() => SimShot.Save(file, 640, 360, SprocketContent.Instance, Notice(), SimShot.Plain(Color.Black)));

            var picture = new PngBitmapDecoder(new Uri(file), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
            Assert.Equal(640, picture.PixelWidth);
            Assert.Equal(360, picture.PixelHeight);

            var pixels = new byte[640 * 360 * 4];
            new FormatConvertedBitmap(picture, System.Windows.Media.PixelFormats.Bgra32, null, 0d).CopyPixels(pixels, 640 * 4, 0);
            Assert.Contains(pixels.Select((value, at) => at % 4 == 3 ? (byte)0 : value), value => value > 0);
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>The button the pointer rests on is lit in the picture, as the workspace lights it in the game.</summary>
    [Fact]
    public void WhatThePointerRestsOnIsLit()
    {
        var notice = Notice();
        var file = Path.Combine(Path.GetTempPath(), "SimShotTests-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            OnSta(() => SimShot.Save(file, 1920, 1080, SprocketContent.Instance, notice));
            var ok = Find<Button>(notice.Root)!;
            Assert.False(ok.IsPointerOver);

            notice.Pointer = ok.Bounds.Center;
            OnSta(() => SimShot.Save(file, 1920, 1080, SprocketContent.Instance, notice));

            Assert.True(ok.IsPointerOver);
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>A mod is registered by any folder holding its Data folder, and the fonts it declares come with it.</summary>
    [Fact]
    public void AModsFontsComeFromItsOwnDeclarations()
    {
        var content = new SimContent().Mod(Path.Combine(SprocketContent.Mods, "Maltek.Symbols"));

        var font = Assert.Single(content.Fonts);
        Assert.Equal("MaltekSymbols", font.Name);
        Assert.True(File.Exists(font.Table), font.Table);
    }

    /// <summary>A block named by its definition id is drawn with the icon its definition gives, where the game's content is here.</summary>
    [Fact]
    public void ABlockIsDrawnWithItsDefinitionsIcon()
    {
        if (GameContent.ContentFolder == null)
            return;

        var picture = new SimContent().PictureFor("MyObjectBuilder_Door/", out var block);

        Assert.True(block);
        Assert.NotNull(picture);
        Assert.True(File.Exists(picture), picture);
    }

    /// <summary>A mod's material whose texture the mod's folder lacks is drawn from the game's own content, as the game draws it.</summary>
    [Fact]
    public void AModsMaterialFallsBackToTheGamesTexture()
    {
        if (GameContent.ContentFolder == null)
            return;

        var mod = Path.Combine(Path.GetTempPath(), "SimShotTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(mod, "Data"));
        try
        {
            File.WriteAllText(Path.Combine(mod, "Data", "Borrowed_TransparentMaterials.sbc"),
                "<Definitions><TransparentMaterials><TransparentMaterial><Id><TypeId>TransparentMaterialDefinition</TypeId>" +
                "<SubtypeId>Borrowed_Blank</SubtypeId></Id><Texture>Textures\\GUI\\Blank.dds</Texture></TransparentMaterial>" +
                "</TransparentMaterials></Definitions>");

            var picture = new SimContent().Mod(mod).PictureFor("Borrowed_Blank", out _);

            Assert.NotNull(picture);
            Assert.True(File.Exists(picture), picture);
        }
        finally
        {
            Directory.Delete(mod, true);
        }
    }

    static T? Find<T>(Element element) where T : Element
    {
        if (element is T found)
            return found;

        for (var i = 0; i < element.VisualChildCount; i++)
        {
            var inside = Find<T>(element.GetVisualChild(i));
            if (inside != null)
                return inside;
        }

        return null;
    }

    /// <summary>Runs <paramref name="act" /> on a thread of its own in a single-threaded apartment, which drawing a picture needs.</summary>
    static void OnSta(Action act)
    {
        Exception? failed = null;
        var thread = new Thread(() =>
        {
            try
            {
                act();
            }
            catch (Exception error)
            {
                failed = error;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failed != null)
            throw new AggregateException(failed);
    }

    static Workspace Notice()
    {
        var arguments = new Arguments();
        arguments.Set("construct", "Hailstone");
        return TerminalNotice.Hosted("Only the owners of {construct} can open its automation.", arguments, "OK");
    }
}
