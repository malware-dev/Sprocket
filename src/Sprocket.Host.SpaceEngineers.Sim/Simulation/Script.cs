using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System.Collections.Generic;
using System;
using System.Globalization;
using VRageMath;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// Things to do to the screen, in order, and the playing of them.
    /// </summary>
    /// <remarks>
    /// Both a picture and a frame dump are the same script driven the same way and read differently at
    /// the end, so the driving lives here rather than in either of them. Two copies would answer the
    /// same script differently the first time one of them learned a step.
    /// </remarks>
    public sealed class Script
    {
        private readonly List<Step> _steps = new List<Step>();

        /// <summary>The steps, in the order they are played.</summary>
        public IList<Step> Steps => _steps;

        /// <summary>How many steps there are.</summary>
        public int Count => _steps.Count;

        /// <summary>
        /// Reads a command line as a script.
        /// </summary>
        /// <remarks>
        /// A bare pair of numbers is a press, because that is nearly every line of nearly every script
        /// and making the common case wordless is worth the small asymmetry.
        /// </remarks>
        public static Script Parse(string[] args, int from)
        {
            var script = new Script();

            for (int i = from; i < args.Length; i++)
            {
                string word = args[i].TrimStart('-');

                if (word == "tick" && i + 1 < args.Length)
                {
                    script.Add(Step.Tick).Count = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    i += 1;
                    continue;
                }

                if (word == "tab")
                {
                    script.Add(Step.Onward);
                    continue;
                }

                if (word == "left" || word == "right" || word == "up" || word == "down")
                {
                    script.Add(Step.Arrow).At = new Vector2(
                        word == "right" ? 1 : word == "left" ? -1 : 0,
                        word == "down" ? 1 : word == "up" ? -1 : 0);
                    continue;
                }

                if (word == "alt")
                {
                    script.Add(Step.Alt);
                    continue;
                }

                // A player new to this release, who is offered its callouts, which a script otherwise never meets.
                if (word == "callouts")
                {
                    script.Add(Step.Callouts);
                    continue;
                }

                if (word == "enter" || word == "escape" || word == "clear")
                {
                    script.Add(word == "enter" ? Step.Enter : word == "escape" ? Step.Escape : Step.Clear);
                    continue;
                }

                // The construct handed to a player by number, so a shot can show what changing hands does.
                if (word == "own" && i + 1 < args.Length)
                {
                    script.Add(Step.Own).Count = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
                    i += 1;
                    continue;
                }

                if (word == "load" && i + 1 < args.Length)
                {
                    script.Add(Step.Load).Text = args[i + 1];
                    i += 1;
                    continue;
                }

                // A press on what the screen says rather than where it says it, so a script outlives the layout. Words with
                // spaces in them are quoted, and survive a command line split at every space: "+ step" arrives as two
                // pieces and is joined again here.
                if (word == "press" && i + 1 < args.Length)
                {
                    script.Add(Step.Named).Text = Quoted(args, i + 1, out var last);
                    i = last;
                    continue;
                }

                if (word == "type" && i + 1 < args.Length)
                {
                    script.Add(Step.Type).Text = args[i + 1];
                    i += 1;
                    continue;
                }

                if (word == "double" && i + 2 < args.Length)
                {
                    script.Add(Step.Double).At = Point(args, i + 1);
                    i += 2;
                    continue;
                }

                if (word == "move" && i + 2 < args.Length)
                {
                    script.Add(Step.Move).At = Point(args, i + 1);
                    i += 2;
                    continue;
                }

                if (word == "scroll" && i + 3 < args.Length)
                {
                    Step scroll = script.Add(Step.Scroll);
                    scroll.At = Point(args, i + 1);
                    scroll.Amount = float.Parse(args[i + 3], CultureInfo.InvariantCulture);
                    i += 3;
                    continue;
                }

                if ((word == "drag" || word == "carry") && i + 4 < args.Length)
                {
                    Step drag = script.Add(word == "drag" ? Step.Drag : Step.Carry);
                    drag.At = Point(args, i + 1);
                    drag.To = Point(args, i + 3);
                    i += 4;
                    continue;
                }

                // A press with something held down. Named for what it means rather than for the key,
                // the same way the rest of the screen names it: a script is not a keyboard.
                if ((word == "also" || word == "through") && i + 2 < args.Length)
                {
                    Step held = script.Add(Step.Press);
                    held.At = Point(args, i + 1);
                    held.Held = word == "also" ? HeldModifier.Also : HeldModifier.Through;
                    i += 2;
                    continue;
                }

                if (i + 1 >= args.Length)
                    break;

                script.Add(Step.Press).At = Point(args, i);
                i += 1;
            }

            return script;
        }

        /// <summary>
        /// Runs every step in order, drawing after each one.
        /// </summary>
        /// <remarks>
        /// Drawing between steps is not for the picture, it is what makes the next step answerable:
        /// nothing knows where it is until it has been arranged, so a press before the first draw has
        /// nothing to land on.
        /// </remarks>
        public void Play(EditorScene scene, Action drawn)
        {
            foreach (var step in _steps)
                Run(scene, step, drawn);
        }

        private Step Add(int kind)
        {
            var step = new Step();
            step.Kind = kind;

            _steps.Add(step);
            return step;
        }

        /// <summary>The words starting at <paramref name="at" />: one piece, or every piece up to the one closing its quote, which <paramref name="last" /> is given.</summary>
        private static string Quoted(string[] args, int at, out int last)
        {
            last = at;
            if (!args[at].StartsWith("\"", StringComparison.Ordinal))
                return args[at];

            var words = args[at];
            while (!(words.Length > 1 && words.EndsWith("\"", StringComparison.Ordinal)) && last + 1 < args.Length)
                words += " " + args[++last];

            return words.Trim('"');
        }

        private static Vector2 Point(string[] args, int at)
        {
            return new Vector2(
                float.Parse(args[at], CultureInfo.InvariantCulture),
                float.Parse(args[at + 1], CultureInfo.InvariantCulture));
        }

        private static void Run(EditorScene scene, Step step, Action drawn)
        {
            if (step.Kind == Step.Press)
            {
                // Moved there first, because a press in the game arrives under a pointer that was already
                // resting on the thing. A script that pressed without moving would leave the highlight on
                // whatever it was last over, which is a state no player can produce.
                scene.MoveTo(step.At);

                // Held for the press and let go afterwards, which is what a key does. Left set, one
                // shifted press would make every press after it a run.
                scene.Held = step.Held;
                scene.Press(step.At, 1);
                scene.Held = HeldModifier.Alone;

                drawn();
                return;
            }

            // Two presses in one gesture, which the game reports as a count rather than as two
            // presses - and which the editor treats as choosing a thing and moving on in one move.
            // Not the same as pressing twice, so a script cannot say it any other way.
            if (step.Kind == Step.Named)
            {
                var at = scene.Find(step.Text);
                scene.MoveTo(at);
                scene.Press(at, 1);
                drawn();
                return;
            }

            if (step.Kind == Step.Onward)
            {
                scene.Tab();
                drawn();
                return;
            }

            if (step.Kind == Step.Arrow)
            {
                scene.Arrow((int)step.At.X, (int)step.At.Y);
                drawn();
                return;
            }

            if (step.Kind == Step.Double)
            {
                scene.MoveTo(step.At);
                scene.Press(step.At, 2);
                drawn();
                return;
            }

            if (step.Kind == Step.Move)
            {
                scene.MoveTo(step.At);
                drawn();
                return;
            }

            if (step.Kind == Step.Callouts)
            {
                scene.MeetCallouts();
                drawn();
                return;
            }

            if (step.Kind == Step.Own)
            {
                scene.Own(step.Count);
                drawn();
                return;
            }

            if (step.Kind == Step.Load)
            {
                // Said out loud, because a program that will not read is the thing worth looking at and
                // a picture of a construct that silently loaded nothing looks like a working empty ship.
                string trouble = scene.Load(System.IO.File.ReadAllText(step.Text));
                if (!string.IsNullOrEmpty(trouble))
                    Console.Error.Write(trouble);

                drawn();
                return;
            }

            if (step.Kind == Step.Type)
            {
                scene.Type(step.Text);
                drawn();
                return;
            }

            // A question with answers owns both keys, the same way the game's own screen gives them
            // to it: Enter takes the answer the page named and Escape dismisses, which is not the
            // same as answering.
            if (step.Kind == Step.Enter)
            {
                scene.Enter();
                drawn();
                return;
            }

            if (step.Kind == Step.Alt)
            {
                scene.View.Codex.FnIsPinKeyHeld = () => true;
                drawn();
                return;
            }

            if (step.Kind == Step.Escape)
            {
                scene.Escape();
                drawn();
                return;
            }

            if (step.Kind == Step.Clear)
            {
                scene.Clear();
                drawn();
                return;
            }

            if (step.Kind == Step.Scroll)
            {
                scene.ScrollBy(step.At, step.Amount);
                drawn();
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
                    drawn();
                }

                return;
            }

            // Carried in several small moves rather than one, each a tick apart, because the sliding is worked
            // out a frame at a time and a jump with no frame between would land before anything had moved out of
            // the way.
            scene.Move(step.At);
            scene.Down(step.At);
            scene.Tick();

            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;

                scene.Move(new Vector2(
                    step.At.X + (step.To.X - step.At.X) * t,
                    step.At.Y + (step.To.Y - step.At.Y) * t));

                scene.Tick();
                drawn();
            }

            if (step.Kind == Step.Carry)
                return;

            scene.Up();
            scene.Tick();
            drawn();
        }
    }
}
