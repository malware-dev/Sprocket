using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sprocket.Sim.Simulation
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
            // A construct that remembers nothing, the same as the suite uses. A shot that ran on the
            // playground both depended on what somebody last built by hand and wrote its own script
            // into it, so taking a picture quietly edited the thing being photographed.
            var scene = EditorScene.Nowhere();
            surface.Frame += scene.Draw;

            // Something behind it, or the bitmap is transparent - which reads as white and makes every
            // light-on-dark choice look wrong. See Backdrop for what it is and is not claiming.
            var behind = new Backdrop();
            var size = new Size(width, height);

            var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            Take(bitmap, behind, surface, size);

            if (script != null)
                script.Play(scene, delegate { Take(bitmap, behind, surface, size); });

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream file = File.Create(path))
                png.Save(file);

            return scene.OnScreen;
        }

        /// <summary>
        /// Draws the screen as it is now into the bitmap.
        /// </summary>
        /// <remarks>
        /// Painted into a visual of its own rather than by invalidating a control and rendering it. This
        /// framework repaints a control when its dispatcher next runs, and a shot has no dispatcher to
        /// run - so that route quietly writes out the frame before the one that was asked for.
        /// </remarks>
        static void Take(RenderTargetBitmap bitmap, Backdrop behind, Surface surface, Size size)
        {
            var frame = new DrawingVisual();
            using (DrawingContext context = frame.RenderOpen())
            {
                behind.Paint(context, size);
                surface.Paint(context, size);
            }

            // Cleared as well, because the bitmap composites onto what is already in it.
            bitmap.Clear();
            bitmap.Render(frame);
        }
    }
}
