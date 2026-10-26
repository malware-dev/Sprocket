using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lumel.Screens;

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
        /// <summary>Writes the editor as it opens, at <paramref name="width" /> by <paramref name="height" />.</summary>
        public static void Save(string path, int width, int height)
        {
            Save(path, width, height, null);
        }

        /// <summary>Runs the script and writes what the last step left on screen; the block starts holding <paramref name="holding" />, or nothing, the screen speaks <paramref name="culture" />, or English, offers the developer tools where <paramref name="developer" />, is an admin's where <paramref name="admin" />.</summary>
        public static string Save(string path, int width, int height, Script script, string holding = null,
            string culture = null, bool developer = false, bool admin = false)
        {
            // A construct that remembers nothing, the same as the suite uses. A shot that ran on the
            // playground both depended on what somebody last built by hand and wrote its own script
            // into it, so taking a picture quietly edited the thing being photographed.
            var scene = holding == null ? EditorScene.Nowhere() : EditorScene.Holding(holding);
            return Save(path, width, height, script, scene, culture, developer, admin);
        }

        /// <summary>Runs the script on <paramref name="scene" />, a construct somebody fitted out for the picture, and writes what the last step left on screen.</summary>
        public static string Save(string path, int width, int height, Script script, EditorScene scene,
            string culture = null, bool developer = false, bool admin = false)
        {
            // Drawn at exactly the size asked for, so a shot is the screen at that resolution rather
            // than a picture of it scaled to fit something else.
            var surface = new Surface();
            surface.Target = new PixelSize(width, height);
            if (culture != null)
                scene.Speak(culture);

            // One frame passes while the screen is built again with the tools on it, as for a language.
            if (developer)
            {
                scene.Editor.DeveloperMode = true;
                scene.Tick();
            }

            // Asked as the editor opens, as the game asks, so it opens again for an admin.
            if (admin)
            {
                scene.Ship.Admin = true;
                scene.Reopen();
                scene.Tick();
            }

            surface.Frame += scene.Draw;

            // Something behind it, or the bitmap is transparent - which reads as white and makes every
            // light-on-dark choice look wrong. See Backdrop for what it is and is not claiming.
            var behind = new Backdrop();
            var size = new Size(width, height);

            var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            Take(bitmap, behind, surface, size);

            if (script != null)
                script.Play(scene, () => Take(bitmap, behind, surface, size));

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream file = File.Create(path))
                png.Save(file);

            return scene.OnScreen;
        }

        /// <summary>Writes <paramref name="workspace" />, a screen of its own such as a notice, across a <paramref name="width" /> by <paramref name="height" /> frame.</summary>
        public static void Save(string path, int width, int height, Workspace workspace)
        {
            var surface = new Surface();
            surface.Target = new PixelSize(width, height);
            surface.Frame += graphics => workspace.Draw(graphics, new VRageMath.RectangleF(0f, 0f, graphics.Size.X, graphics.Size.Y));

            var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            Take(bitmap, new Backdrop(), surface, new Size(width, height));

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream file = File.Create(path))
                png.Save(file);
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
