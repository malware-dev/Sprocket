using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lumel.Drawing;
using Sandbox.ModAPI;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using Color = VRageMath.Color;
using Size = System.Windows.Size;

namespace Sprocket.Sim.Simulation;

/// <summary>A screen's last frame of sprites as a picture of its face, drawn the way the game draws a text panel's sprites.</summary>
/// <remarks>
/// An app places its sprites in the screen's whole texture, where the face is the part centered in it, so everything is moved
/// by the margin between the two before it is drawn. A texture is placed by its center, or by its left or right edge where it
/// is aligned to one; text by the top of its line, at its left end, its right end or its middle. The face is filled with the
/// screen's background color first, as the game fills it behind an app.
/// </remarks>
public static class FaceShot
{
    const string Square = "SquareSimple";

    /// <summary>Has <paramref name="app" /> draw a frame on <paramref name="face" />, measuring in the game's fonts, and writes it to <paramref name="path" />, each of its pixels <paramref name="zoom" /> pixels across.</summary>
    public static void Save(string path, Face face, int zoom, Action app)
    {
        Vector2 size = face.Shape.SurfaceSize;
        var wide = (int)Math.Ceiling(size.X);
        var high = (int)Math.Ceiling(size.Y);

        var surface = new Surface { Target = new PixelSize(wide, high) };
        face.Measuring = surface.Screen;
        app();
        surface.Frame += _ => Draw(surface.Screen, face);

        var into = new Size(wide * zoom, high * zoom);
        var frame = new DrawingVisual();
        using (DrawingContext context = frame.RenderOpen())
            surface.Paint(context, into);

        var bitmap = new RenderTargetBitmap(wide * zoom, high * zoom, 96d, 96d, PixelFormats.Pbgra32);
        bitmap.Render(frame);

        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using (FileStream file = File.Create(path))
            png.Save(file);
    }

    static void Draw(IScreenSpace screen, Face face)
    {
        Vector2 texture = face.Shape.TextureSize;
        Vector2 margin = (texture - face.Shape.SurfaceSize) / 2f;
        const MyScreenSpaceLayer layer = MyScreenSpaceLayer.AboveHud;

        Vector2 shown = face.Shape.SurfaceSize;
        screen.DrawSprite(MyStringId.GetOrCompute(Square), new Rectangle(0, 0, (int)Math.Ceiling(shown.X), (int)Math.Ceiling(shown.Y)),
            null, face.Background, 0f, layer);

        var clipped = 0;
        var text = new StringBuilder();
        foreach (MySprite sprite in face.Sprites)
        {
            Vector2 at = (sprite.Position ?? texture / 2f) - margin;
            Vector2 span = sprite.Size ?? texture;
            Color color = sprite.Color ?? Color.White;

            switch (sprite.Type)
            {
                case SpriteType.TEXTURE:
                    if (sprite.Alignment == TextAlignment.LEFT)
                        at.X += span.X / 2f;
                    else if (sprite.Alignment == TextAlignment.RIGHT)
                        at.X -= span.X / 2f;

                    screen.DrawSprite(MyStringId.GetOrCompute(sprite.Data), new Rectangle((int)Math.Round(at.X - span.X / 2f),
                        (int)Math.Round(at.Y - span.Y / 2f), (int)Math.Round(span.X), (int)Math.Round(span.Y)), null, color,
                        sprite.RotationOrScale, layer);
                    break;

                case SpriteType.TEXT:
                    // A panel measures and draws text at its scale alone, where the game's screen also scales it by the
                    // screen's height against the HUD's reference height, so that is taken back out.
                    var font = sprite.FontId ?? "Debug";
                    var scale = sprite.RotationOrScale * ScreenSpaceGraphics.GameReferenceHeight / screen.SafeArea.Height;
                    text.Clear().Append(sprite.Data ?? "");
                    var width = screen.MeasureText(font, text, scale).X;
                    if (sprite.Alignment == TextAlignment.RIGHT)
                        at.X -= width;
                    else if (sprite.Alignment == TextAlignment.CENTER)
                        at.X -= width / 2f;

                    screen.DrawText(font, sprite.Data ?? "", at, scale, color, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                        float.PositiveInfinity, layer);
                    break;

                case SpriteType.CLIP_RECT:
                    for (; clipped > 0; clipped--)
                        screen.PopScissor(layer);

                    if (sprite.Position.HasValue && sprite.Size.HasValue)
                    {
                        screen.PushScissor(new Rectangle((int)at.X, (int)at.Y, (int)span.X, (int)span.Y), layer);
                        clipped++;
                    }

                    break;
            }
        }

        for (; clipped > 0; clipped--)
            screen.PopScissor(layer);
    }
}
