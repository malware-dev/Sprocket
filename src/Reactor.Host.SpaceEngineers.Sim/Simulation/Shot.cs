using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Renders frames to a file, with no window and nobody watching.
    /// </summary>
    /// <remarks>
    /// The other half of a frame being data. The draw list says what was asked for; this says what came
    /// out, which is the only way to tell a clip that was never issued from one that was issued and
    /// ignored.
    /// </remarks>
    public static class Shot
    {
        public static void Save(string path, int width, int height)
        {
            Save(path, width, height, null);
        }

        /// <summary>Runs the script and writes what the last step left on screen.</summary>
        public static string Save(string path, int width, int height, Script script)
        {
            // Drawn at exactly the size asked for, so a shot is the screen at that resolution rather
            // than a picture of it scaled to fit something else.
            var surface = new Surface();
            surface.Target = new PixelSize(width, height);
            var scene = new EditorScene();
            surface.Frame += scene.Draw;

            // Something behind it, or the bitmap is transparent - which reads as white and makes every
            // light-on-dark choice look wrong. See Backdrop for what it is and is not claiming.
            var root = new Panel();
            root.Children.Add(new Backdrop());
            root.Children.Add(surface);

            var size = new Size(width, height);
            root.Measure(size);
            root.Arrange(new Rect(size));

            using (var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96)))
            {
                bitmap.Render(root);

                if (script != null)
                    script.Play(scene, () => bitmap.Render(root));

                bitmap.Save(path);
                return scene.OnScreen;
            }
        }
    }
}
