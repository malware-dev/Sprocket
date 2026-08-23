using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Renders frames to a file, with no window and nobody watching.
    /// <para>
    /// The other half of a frame being data. The draw list says what was asked for; this says what came
    /// out, which is the only way to tell a clip that was never issued from one that was issued and
    /// ignored. It is also what a golden-image test would be built on, when there is something stable
    /// enough to be worth freezing.
    /// </para>
    /// </summary>
    public static class Shot
    {
        /// <summary>What the pointer and the clock do, in the order somebody asked for it.</summary>
        /// <remarks>
        /// One list rather than a list of presses and a separate list of drags, because the order between
        /// them is the whole of what a script is. Pressing Run and then letting sixty ticks pass is a
        /// different picture from letting sixty ticks pass and then pressing Run, and only the first ever
        /// shows a system actually running.
        /// </remarks>
        public sealed class Step
        {
            public const int Press = 0;
            public const int Drag = 1;
            public const int Tick = 2;
            public const int Scroll = 3;
            public const int Type = 4;
            public const int Enter = 5;
            public const int Escape = 6;

            public int Kind;
            public Vector2 At;
            public Vector2 To;
            public int Count;
            public float Amount;
            public string Text;
        }

        public static void Save(string path, int width, int height)
        {
            Save(path, width, height, null);
        }

        /// <summary>
        /// Runs the script, drawing between every step, and writes what the last one left on screen.
        /// </summary>
        /// <remarks>
        /// Drawing between steps is not for the picture, it is what makes the next step answerable:
        /// nothing knows where it is until it has been arranged, so a press before the first draw has
        /// nothing to land on.
        /// </remarks>
        public static string Save(string path, int width, int height, IList<Step> script)
        {
            var surface = new PhosphorSurface();
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
                {
                    for (int i = 0; i < script.Count; i++)
                        Run(scene, bitmap, root, script[i]);
                }

                bitmap.Save(path);
                return scene.OnScreen;
            }
        }

        static void Run(EditorScene scene, RenderTargetBitmap bitmap, Control root, Step step)
        {
            if (step.Kind == Step.Press)
            {
                scene.Press(step.At, 1);
                bitmap.Render(root);
                return;
            }

            if (step.Kind == Step.Type)
            {
                scene.Type(step.Text);
                bitmap.Render(root);
                return;
            }

            if (step.Kind == Step.Enter)
            {
                scene.CommitAsk();
                bitmap.Render(root);
                return;
            }

            if (step.Kind == Step.Escape)
            {
                scene.CancelAsk();
                bitmap.Render(root);
                return;
            }

            if (step.Kind == Step.Scroll)
            {
                scene.ScrollBy(step.At, step.Amount);
                bitmap.Render(root);
                return;
            }

            if (step.Kind == Step.Tick)
            {
                // Drawn on every tick rather than only at the end, because the things worth seeing here
                // are the ones that decay: a light lit for a moment is invisible to a script that
                // advances the clock a hundred times and then looks once.
                for (int i = 0; i < step.Count; i++)
                {
                    scene.Tick();
                    bitmap.Render(root);
                }

                return;
            }

            // Carried in several small moves rather than one, because the sliding is worked out a frame
            // at a time and a single jump would land before anything had moved out of the way.
            scene.Grab(step.At);

            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;

                scene.Carry(new Vector2(
                    step.At.X + (step.To.X - step.At.X) * t,
                    step.At.Y + (step.To.Y - step.At.Y) * t));

                bitmap.Render(root);
            }

            scene.Drop();
            bitmap.Render(root);
        }
    }
}
