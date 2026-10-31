using Lumel.Screens;
using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Sim.Machine;

namespace Sprocket.Sim.Simulation;

/// <summary>A construct's status drawn on a harness screen through a text panel, as the panel's app draws it in the game.</summary>
public static class StatusFace
{
    /// <summary>
    /// Lays <paramref name="picture" /> out for <paramref name="face" /> showing <paramref name="status" /> in
    /// <paramref name="texts" />, at the page <paramref name="turn" /> places in with the page <paramref name="from" /> places
    /// in beside it to slide from, and draws it into the face's sprites.
    /// </summary>
    /// <remarks>The panel lets go of the picture once it has drawn, so the picture can be drawn on another face.</remarks>
    public static void Draw(StatusPicture picture, Face face, ConstructStatus status, Texts texts, int turn, int from)
    {
        var panel = Panel(picture, face);
        Lay(picture, panel, face, status, texts, turn, from);
        face.Sprites.Clear();
        panel.Draw(face.Sprites);
        panel.Content = null;
    }

    /// <summary>A text panel on <paramref name="face" /> showing <paramref name="picture" />, timed at tick 0.</summary>
    public static TextPanel Panel(StatusPicture picture, Face face)
    {
        return new TextPanel(face.Surface, null, () => 0) { Content = picture.Root };
    }

    /// <summary>Lays <paramref name="picture" /> out on <paramref name="panel" />, as <see cref="Draw" /> does, without drawing it.</summary>
    public static void Lay(StatusPicture picture, TextPanel panel, Face face, ConstructStatus status, Texts texts, int turn, int from)
    {
        panel.Graphics.Begin();
        picture.Lay(panel.Graphics, status, texts, face.Foreground, face.Background, face.Padding, face.Shape.Metres, turn, from);
    }
}
