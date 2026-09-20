using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Storage;
using Ship = Sprocket.Sim.Machine.Simulation;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Sim.Machine;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// Sprocket itself, running on a made-up ship, drawn the way the mod will draw it.
    /// </summary>
    /// <remarks>
    /// Three things meet here and none of them knows about the other two: core decides what is on screen,
    /// <see cref="EditorView" /> decides how this target draws it, and the simulated ship supplies a world
    /// and a clock. The same three meet in the mod, with the game supplying the third.
    /// </remarks>
    public sealed class EditorScene
    {
        readonly Ship _ship;
        readonly EditorView _view;

        public EditorScene() : this(new Ship())
        {
        }

        /// <summary>
        /// A screen on a construct that remembers nothing.
        /// </summary>
        /// <remarks>
        /// What anything running without a person in front of it wants. The windowed simulator keeps its
        /// program between runs on purpose, because it is a playground - but a frame captured for
        /// comparison, or a test, must not depend on what somebody last built by hand, and must not
        /// write into that playground either.
        /// </remarks>
        public static EditorScene Nowhere()
        {
            return new EditorScene(Ship.Fresh());
        }

        EditorScene(Ship ship)
        {
            _ship = ship;
            _view = new EditorView(_ship.Editor);
        }

        /// <summary>
        /// The editor itself, for anything that needs to drive it rather than press at it.
        /// </summary>
        /// <remarks>
        /// A test that is about what the editor does, rather than about where this target puts it,
        /// says so by coming through here instead of pressing coordinates.
        /// </remarks>
        public SprocketEditor Editor => _ship.Editor;

        /// <summary>
        /// Reads a program into the construct, the way loading a save does.
        /// </summary>
        /// <remarks>
        /// So a shot can show a state that pressing cannot reach. A script a person wrote by hand is a
        /// supported way in - the language is readable and repairable on purpose - and some of what it
        /// can say has no button in the editor yet.
        /// <para>
        /// Whatever would not read is handed back rather than thrown, because a program that will not
        /// load is itself worth looking at.
        /// </para>
        /// </remarks>
        public string Load(string program)
        {
            var trouble = ProgramText.Load(program, _ship.Editor.Program, _ship.World.Values);

            // The editor built its screen from an empty program, so it has to be told: in the game
            // the program is read before the editor exists and this is the moment that stands in.
            _ship.Editor.Invalidate();
            return trouble;
        }

        /// <summary>Where the program is being kept, for the line at the bottom of the window.</summary>
        public string Where => _ship.Where;

        /// <summary>
        /// The things on the made-up ship that can be poked at from outside.
        /// </summary>
        /// <remarks>
        /// For the simulator's own scaffolding and nothing else. Tripping a sensor or draining a tank is
        /// something the world does, and in the game a ship actually flies up to the sensor - so none of
        /// this is Sprocket, and none of it goes anywhere the mod can compile.
        /// </remarks>
        internal IEnumerable<Panel> Panels()
        {
            return _ship.Panels();
        }

        /// <summary>Writes the program out. The one moment a timer cannot cover is closing the window.</summary>
        public void Save()
        {
            _ship.Save();
        }

        /// <summary>Advances the ship one frame. The clock is the window's, as it is the game's in the mod.</summary>
        public void Tick()
        {
            _ship.Tick();
        }

        /// <summary>
        /// What screen the editor is on, and what it is called.
        /// </summary>
        /// <remarks>
        /// For the headless harness to say where a script ended up. A press that misses lands nowhere
        /// and looks exactly like a press that worked, which has already cost an hour once.
        /// </remarks>
        public string OnScreen
        {
            get
            {
                EditorScreen screen = _ship.Editor.Screen;

                // Joined here rather than asked of core, which publishes a heading in parts and no
                // string of the whole thing on purpose - a target handed one would draw one. This is a
                // line of log output, so joining it is a report rather than a rendering.
                string heading = "";
                for (int i = 0; i < screen.Heading.Count; i++)
                {
                    HeadingPart part = screen.Heading[i];

                    if (heading.Length > 0)
                        heading += part.Lead.Length > 0 ? " " : " > ";

                    if (part.Lead.Length > 0)
                        heading += part.Lead + " ";

                    heading += part.Text;
                }

                return heading + (string.IsNullOrEmpty(screen.Subtitle)
                    ? "" : "  |  " + screen.Subtitle);
            }
        }

        /// <summary>Whether a question is open and wants the keyboard.</summary>
        public bool IsAsking => _view.IsAsking;

        /// <summary>The box that question is being answered in.</summary>
        public TextBox AskBox => _view.AskBox;

        public bool CaretVisible
        {
            set => _view.CaretVisible = value;
        }

        public void CommitAsk()
        {
            _view.CommitAsk();
        }

        public void CancelAsk()
        {
            _view.CancelAsk();
        }

        /// <summary>Moves the caret to the next field of a form, which Tab does in the game.</summary>
        public void NextField()
        {
            _view.NextField();
        }

        /// <summary>The box the keyboard is in, when something on the screen has taken it.</summary>
        public TextBox Focus => _view.Focus;

        /// <summary>Sends what was just typed wherever it belongs.</summary>
        public void Typed()
        {
            _view.Typed();
        }

        /// <summary>
        /// The box keystrokes belong in, or null when nothing on screen is being typed into.
        /// </summary>
        /// <remarks>
        /// Asked here rather than worked out by each caller. A question owns the keyboard while it is
        /// open; otherwise it goes to whatever was focused, which is how a search box narrows a list as
        /// somebody types. The window and the headless harness both need that answer and must not be
        /// allowed to arrive at different ones.
        /// </remarks>
        public TextBox Typing => _view.IsAsking ? _view.AskBox : _view.Focus;

        /// <summary>
        /// Empties whatever has the keyboard, the way holding backspace would.
        /// </summary>
        /// <remarks>
        /// Here because a script had no way to reach an empty box, and an empty box is where most of what
        /// is interesting about a text field happens - a hint appearing, a name being refused, a search
        /// giving a list back. Typing could only ever add.
        /// </remarks>
        public void Clear()
        {
            TextBox box = Typing;
            if (box == null)
                return;

            box.SelectAll();
            box.Backspace();
            Typed();
        }

        /// <summary>Types <paramref name="text" /> into whatever has the keyboard.</summary>
        public void Type(string text)
        {
            TextBox box = Typing;
            if (box == null || string.IsNullOrEmpty(text))
                return;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] >= ' ')
                    box.InsertChar(text[i]);
            }

            // Every keystroke, not just the last. A search that only narrowed once somebody stopped
            // typing would feel broken, and this is the path that proves it.
            Typed();
        }

        /// <summary>
        /// Says where the pointer is, so the screen can light whatever it is resting on.
        /// </summary>
        /// <remarks>
        /// Separate from pressing because moving is not pressing, and because a script that only ever
        /// presses should not accidentally be testing the highlight as well.
        /// </remarks>
        public void MoveTo(Vector2 point)
        {
            _view.Pointer = point;
        }

        /// <summary>Moves whatever is under the pointer. Positive scrolls down.</summary>
        public void ScrollBy(Vector2 point, float amount)
        {
            _view.ScrollBy(point, amount);
        }

        /// <summary>How many times the screen has written what its controls mean onto them.</summary>
        public int Wires => _view.Wires;

        /// <summary>How many views are listening to a component right now.</summary>
        public int Watching => _view.Watching;

        /// <summary>How many questions the mod has asked the game, in total.</summary>
        public int Asked => Fakes.Pretend.Asked;

        /// <summary>Puts the screen away, as a host closing it does.</summary>
        public void PutAway()
        {
            _view.Release();
        }

        /// <summary>Shows it again, as a host reopening the same one does.</summary>
        public void ShowAgain()
        {
            _view.Attach();
        }

        /// <summary>
        /// What a press or a hover at <paramref name="point" /> would reach, without touching it.
        /// </summary>
        /// <remarks>
        /// For asking the screen about itself rather than driving it: what a test can then do is sweep
        /// every point and hold the rules that are true of everything a person can reach.
        /// </remarks>
        public Element Under(Vector2 point)
        {
            return _view.Under(point);
        }

        /// <summary>
        /// What is being held down, which the game's own screen reads off the keyboard every frame.
        /// </summary>
        public Held Held
        {
            get { return _view.Held; }
            set { _view.Held = value; }
        }

        /// <summary>Presses whatever is at <paramref name="point" />, in UI units.</summary>
        public bool Press(Vector2 point, int clicks)
        {
            bool hit = _view.Press(point, clicks);

            // Taken and dropped on the floor, on purpose. There is no window here to close - the editor is
            // the whole of what this program draws - and leaving the request standing would mean the next
            // host to ask got a yes for a press somebody made minutes ago.
            _view.TakeClose();
            return hit;
        }

        /// <summary>Offers the press as a grab first, so a drag is not also a click.</summary>
        public bool Grab(Vector2 point)
        {
            return _view.Grab(point);
        }

        public void Carry(Vector2 point)
        {
            _view.Carry(point);
        }

        public void Drop()
        {
            _view.Drop();
        }

        public bool IsDragging => _view.IsDragging;

        public void Draw(ScreenSpaceGraphics g)
        {
            _view.Update();

            // Where it goes is the editor's own business, and it is the same answer in the mod.
            _view.Draw(g);
        }
    }
}
