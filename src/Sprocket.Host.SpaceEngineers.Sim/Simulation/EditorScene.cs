using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Ship = Sprocket.Sim.Machine.Simulation;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Sim.Machine;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Input;
using Sprocket.Sim.Content;

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

        Vector2 _at;

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

        /// <summary>A scene on a block that holds <paramref name="program" />, the way a world saved earlier leaves one.</summary>
        public static EditorScene Holding(string program)
        {
            var store = Store.Nowhere();
            store.SetValue(SprocketUnit.ProgramKey, program);
            return new EditorScene(Ship.On(store));
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

        /// <summary>Says everything in <paramref name="culture" />, such as <c>no</c>, read from the mod's own texts; one frame passes while the screen is built again in it.</summary>
        public void Speak(string culture)
        {
            _ship.Texts.Book.Load(new ResxTexts(ResxTexts.Beside, culture));
            _ship.Editor.Invalidate();
            Tick();
        }

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
        internal IEnumerable<Machine.Panel> Panels()
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

        /// <summary>Whether a box of a form of values has the keyboard.</summary>
        public bool IsFilling => Filling != null;

        /// <summary>The box of a form of values that has the keyboard, or null where none has it.</summary>
        public TextBox? Filling
        {
            get
            {
                var box = _view.Input.Focused as TextBox;
                for (Element? at = box; at != null; at = at.Parent)
                {
                    if (at is FieldSet)
                        return box;
                }

                return null;
            }
        }

        /// <summary>Whether the caret is drawn this frame.</summary>
        public bool CaretVisible
        {
            set => _view.CaretVisible = value;
        }

        /// <summary>Whether a question with answers is waiting, which owns Enter and Escape.</summary>
        public bool IsQuestioning => _view.IsQuestioning;

        /// <summary>Takes the answer the page said Enter takes, as the game's screen does.</summary>
        public bool TakeDefault()
        {
            return _view.TakeDefault();
        }

        /// <summary>Dismisses the standing question, telling the page what that meant.</summary>
        public void Dismiss()
        {
            _view.Dismiss();
        }

        /// <summary>Enter, as the game's screen hands it over: the answer a question names, or whatever holds the keyboard.</summary>
        public void Enter()
        {
            _view.Key(MyKeys.Enter, false, false);
        }

        /// <summary>Escape, short of going back: a question is dismissed, and otherwise whatever holds the keyboard lets it go.</summary>
        public void Escape()
        {
            if (_view.IsQuestioning)
            {
                _view.Dismiss();
                return;
            }

            _view.Input.Key(MyKeys.Escape, false, false);
        }

        /// <summary>A key going down, as the game's screen hands one over, answering whether anything took it.</summary>
        public bool Key(MyKeys key, bool shift, bool ctrl)
        {
            return _view.Key(key, shift, ctrl);
        }

        /// <summary>Tab, which moves the keyboard to the next thing that takes it.</summary>
        public void Tab()
        {
            _view.Key(MyKeys.Tab, false, false);
        }

        /// <summary>An arrow key: one across for right, minus one for left, and down the same way.</summary>
        public void Arrow(int across, int down)
        {
            _view.Key(across < 0 ? MyKeys.Left : across > 0 ? MyKeys.Right : down < 0 ? MyKeys.Up : MyKeys.Down, false, false);
        }

        /// <summary>Types <paramref name="text" /> into whatever has the keyboard.</summary>
        public void Type(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            for (var i = 0; i < text.Length; i++)
                _view.Text(text[i]);
        }

        /// <summary>Empties whatever has the keyboard, as selecting everything and deleting it would.</summary>
        /// <remarks>
        /// Here because a script had no other way to reach an empty box, and an empty box is where most of what is
        /// interesting about a text field happens - a hint appearing, a name being refused, a search giving a list back.
        /// </remarks>
        public void Clear()
        {
            if (_view.Input.Focused == null)
                return;

            _view.Key(MyKeys.A, false, true);
            _view.Key(MyKeys.Back, false, false);
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

        /// <summary>How many times the screen has written what the editor says onto its controls.</summary>
        public int Wires => _view.Wires;

        /// <summary>How many questions the mod has asked the game, in total.</summary>
        public int Asked => Fakes.Pretend.Asked;

        /// <summary>
        /// What a press or a hover at <paramref name="point" /> would reach, without touching it.
        /// </summary>
        /// <remarks>
        /// For asking the screen about itself rather than driving it: what a test can then do is sweep
        /// every point and hold the rules that are true of everything a person can reach.
        /// </remarks>
        public Element? Under(Vector2 point)
        {
            return _view.Under(point);
        }

        /// <summary>What is held down with the pointer, which the game's own screen reads off the keyboard as a press lands.</summary>
        public Held Held { get; set; }

        /// <summary>Presses at <paramref name="point" />, in UI units, the <paramref name="presses" />th time in quick succession.</summary>
        public void Press(Vector2 point, int presses)
        {
            Down(point, presses);
            Up();
        }

        /// <summary>The button going down at <paramref name="point" />, the <paramref name="presses" />th time in quick succession.</summary>
        public void Down(Vector2 point, int presses = 1)
        {
            _at = point;
            _view.PointerDown(point, presses, Held == Held.Through, Held == Held.Also);
        }

        /// <summary>The pointer moving to <paramref name="point" /> with the button down.</summary>
        public void Move(Vector2 point)
        {
            _at = point;
            _view.PointerMove(point);
        }

        /// <summary>The button coming up where the pointer last was.</summary>
        /// <remarks>
        /// A request to leave is taken and dropped on the floor, on purpose. There is no window here to close - the editor
        /// is the whole of what this program draws - and leaving the request standing would mean the next host to ask got a
        /// yes for a press somebody made minutes ago.
        /// </remarks>
        public void Up()
        {
            _view.PointerUp(_at);
            _view.TakeClose();
        }

        public void Draw(ScreenSpaceGraphics g)
        {
            _view.Update();

            // Where it goes is the editor's own business, and it is the same answer in the mod.
            _view.Draw(g);
        }
    }
}
