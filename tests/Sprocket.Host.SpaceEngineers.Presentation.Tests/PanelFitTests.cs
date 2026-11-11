using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Measuring;
using Sprocket.Sim.Simulation;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The automation panel in each of its scenes, on every screen of the reference set and every page it turns between, measured
/// with the game's own letter widths: no words run past the side of what holds them, and no two runs are drawn over each other.
/// </summary>
public class PanelFitTests
{
    /// <summary>Every scene the panel is shown in.</summary>
    public static IEnumerable<object[]> Scenes => StatusScenes.Names.Select(name => new object[] { name });

    /// <summary>Each page of the scene fits on every screen.</summary>
    [Theory]
    [MemberData(nameof(Scenes))]
    public void EveryScreenFits(string scene)
    {
        var found = new List<string>();
        OnSta(() => Look(scene, found));
        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
    }

    static void Look(string scene, List<string> found)
    {
        var status = StatusScenes.Make(scene, out var texts);
        foreach (ScreenShape shape in ScreenShape.Reference)
        {
            var picture = new StatusPicture();
            for (var turn = 0; turn < Math.Max(1, picture.Pages); turn++)
            {
                var face = new Face(shape.Name) { Shape = shape };
                var shown = turn;
                var device = FaceShot.Drawn(face, () => StatusFace.Draw(picture, face, status, texts, shown, shown));
                var size = shape.SurfaceSize;
                var said = TextFit.Cut(device.Commands, device.Fonts!, size.X, size.Y)
                    .Concat(TextFit.Overlapping(device.Commands, device.Fonts!, size.X, size.Y, Symbols.Font));
                found.AddRange(said.Select(one => $"{shape.Name}, page {turn + 1}: {one}"));
            }
        }
    }

    /// <summary>Runs <paramref name="act" /> on a thread of its own in a single-threaded apartment, which the drawing surface needs.</summary>
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
}
