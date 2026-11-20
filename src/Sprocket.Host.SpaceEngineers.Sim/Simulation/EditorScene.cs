using Sprocket.Host.SpaceEngineers;
using Sprocket.Host.SpaceEngineers.Features.Callouts;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Versioning;
using System;
using System.Collections.Generic;
using Ship = Sprocket.Sim.Machine.Simulation;
using VRageMath;
using Lumel;
using Lumel.Drawing;
using Lumel.Input;
using Sprocket.Sim.Machine;
using Lumel.Text;
using VRage.Input;
using Sprocket.Sim.Content;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// Sprocket itself, running on a made-up ship, drawn the way the mod will draw it.
    /// </summary>
    /// <remarks>
    /// Three things meet here and none of them knows about the other two: core decides what is on screen,
    /// <see cref="EditorWindow" /> decides how this target draws it, and the simulated ship supplies a world
    /// and a clock. The same three meet in the mod, with the game supplying the third.
    /// </remarks>
    public sealed class EditorScene
    {
        readonly Ship _ship;
        readonly SprocketWorkspace _workspace = new SprocketWorkspace();
        EditorWindow _view;

        // The player's choices, as a player already offered this release's callouts has them, so a screen shows none.
        PlayerSettings _player = new PlayerSettings(true, ModVersion.Release);

        Vector2 _at;

        /// <summary>The editor on a ship of its own.</summary>
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
            store.SetValue(BlockStorage.ProgramKey, program);
            return new EditorScene(Ship.On(store));
        }

        /// <summary>A screen on a construct of <paramref name="rig" />'s blocks that remembers nothing, for a test about particular blocks.</summary>
        public static EditorScene On(Rig rig)
        {
            return new EditorScene(Ship.On(rig));
        }

        EditorScene(Ship ship)
        {
            _ship = ship;
            SaveChoices();
            _view = new EditorWindow(_ship.Editor, _workspace);
            _workspace.Open(_view);
        }

        /// <summary>
        /// The editor itself, for anything that needs to drive it rather than press at it.
        /// </summary>
        /// <remarks>
        /// A test that is about what the editor does, rather than about where this target puts it,
        /// says so by coming through here instead of pressing coordinates.
        /// </remarks>
        public SprocketEditor Editor => _ship.Editor;

        /// <summary>The editor's window.</summary>
        public EditorWindow View => _view;

        /// <summary>The workspace the editor is shown in, windows included.</summary>
        public SprocketWorkspace Workspace => _workspace;

        /// <summary>
        /// Says everything in <paramref name="culture" />, such as <c>no</c>, read from the mod's own texts and rewording the editor as
        /// the session does when the game's language changes; one frame passes while the screen is built again in it.
        /// </summary>
        public void Speak(string culture)
        {
            _ship.Texts.Load(new ResxTexts(ResxTexts.Beside, culture));
            _ship.Editor.Reword();
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
            var trouble = ProgramReader.Load(program, _ship.Editor.Program, _ship.World.Values);

            // The editor built its screen from an empty program, so it has to be told: in the game
            // the program is read before the editor exists and this is the moment that stands in.
            _ship.Editor.Invalidate();
            return trouble;
        }

        /// <summary>Hands the whole construct to <paramref name="player" />, lets the automation hear it, and opens the editor again.</summary>
        public void Own(long player)
        {
            _ship.Machine.Own(player);
            _ship.Tick();
            _ship.Tick();
            _ship.Editor.Opened();
            _ship.Editor.Invalidate();
        }

        /// <summary>where the program is being stored, for the line at the bottom of the window.</summary>
        public string Where => _ship.Where;

        /// <summary>
        /// The things on the made-up ship that can be poked at from outside.
        /// </summary>
        /// <remarks>
        /// For the simulator's own scaffolding and nothing else. Tripping a sensor or draining a tank is
        /// something the world does, and in the game a ship actually flies up to the sensor - so none of
        /// this is Sprocket, and none of it goes anywhere the mod can compile.
        /// </remarks>
        public IEnumerable<Machine.Panel> Panels()
        {
            return _ship.Panels();
        }

        /// <summary>Writes the program out. The one moment a timer cannot cover is closing the window.</summary>
        public void Save()
        {
            _ship.Save();
        }

        /// <summary>The ship under the screen, for a test about what it does rather than what it shows.</summary>
        public Ship Ship => _ship;

        /// <summary>The world saved and loaded again, and the screen opened anew on the editor that comes back.</summary>
        public void Reload()
        {
            _ship.Reload();
            Reopen();
        }

        /// <summary>The screen closed and opened again, as a player looking again does.</summary>
        public void Reopen()
        {
            _view.Close();
            SaveChoices();
            _view = new EditorWindow(_ship.Editor, _workspace);
            _workspace.Open(_view);
            _ship.Editor.Opened();
        }

        /// <summary>What the player has chosen, which the editor keeps here where the game keeps it in a file.</summary>
        public PlayerSettings Player => _player;

        /// <summary>Opens the editor again for a player who has not been offered any callout, as one is after an update.</summary>
        public void MeetCallouts()
        {
            _player = new PlayerSettings(true);
            Reopen();
        }

        /// <summary>Has the editor keep the player's choices here.</summary>
        void SaveChoices()
        {
            _ship.Editor.Callouts.Saving(() => _player, saved => _player = saved);
        }

        /// <summary>The frame the ship is on, which the screen is timed by.</summary>
        public int CurrentTick => _ship.Editor.CurrentTick;

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
                foreach (HeadingPart part in screen.Heading)
                {
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
                var box = _workspace.Input.Focused as TextBox;
                for (Element? at = box; at != null; at = at.Parent)
                {
                    if (at is FieldSet)
                        return box;
                }

                return null;
            }
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
            _workspace.Key(MyKeys.Enter, false, false);
        }

        /// <summary>Escape, as the game's screen hands it over: closing what is open over the page, or else going back.</summary>
        /// <remarks>The editor's own answer, as the game's screen asks it; on the first page, where the game would close the editor, nothing happens.</remarks>
        public void Escape()
        {
            _view.Escape();
        }

        /// <summary>A key going down, as the game's screen hands one over, answering whether anything took it.</summary>
        public bool Key(MyKeys key, bool shift, bool ctrl)
        {
            return _workspace.Key(key, shift, ctrl);
        }

        /// <summary>Tab, which moves the keyboard to the next thing that takes it.</summary>
        public void Tab()
        {
            _workspace.Key(MyKeys.Tab, false, false);
        }

        /// <summary>An arrow key: one across for right, minus one for left, and down the same way.</summary>
        public void Arrow(int across, int down)
        {
            _workspace.Key(across < 0 ? MyKeys.Left : across > 0 ? MyKeys.Right : down < 0 ? MyKeys.Up : MyKeys.Down, false, false);
        }

        /// <summary>Types <paramref name="text" /> into whatever has the keyboard.</summary>
        public void Type(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            foreach (var character in text)
                _workspace.Text(character);
        }

        /// <summary>Empties whatever has the keyboard, as selecting everything and deleting it would.</summary>
        /// <remarks>
        /// Here because a script had no other way to reach an empty box, and an empty box is where most of what is
        /// interesting about a text field happens - a hint appearing, a name being refused, a search giving a list back.
        /// </remarks>
        public void Clear()
        {
            if (_workspace.Input.Focused == null)
                return;

            _workspace.Key(MyKeys.A, false, true);
            _workspace.Key(MyKeys.Back, false, false);
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
            _workspace.Pointer = point;
        }

        /// <summary>Moves whatever is under the pointer. Positive scrolls down.</summary>
        public void ScrollBy(Vector2 point, float amount)
        {
            _workspace.Wheel(point, new Vector2(0f, amount));
        }

        /// <summary>Turns the wheel sideways over <paramref name="point" />, as Shift and the wheel do in the game. Positive scrolls right.</summary>
        public void ScrollSideways(Vector2 point, float amount)
        {
            _workspace.Wheel(point, new Vector2(amount, 0f));
        }

        /// <summary>How many times the screen has written what the editor says onto its controls.</summary>
        public int Wires => _view.Wires;

        /// <summary>The tip open on whatever the pointer rests on, or null where none is.</summary>
        public Tip? ShownTip => _workspace.ShownTip;

        /// <summary>The codex tip open on whatever the pointer rests on, or null where none is or the tip open is a plain one.</summary>
        public CodexTip? CodexTip => _view.ShownCodexTip;

        /// <summary>Whether the keep key is held, as the game's screen answers the view when it asks.</summary>
        public bool IsKeepKeyHeld
        {
            set
            {
                var held = value;
                _view.Codex.FnIsPinKeyHeld = () => held;
            }
        }

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
            return _workspace.HitTest(point);
        }

        /// <summary>Where a press reaches the first thing on the screen saying <paramref name="text" />, in UI units.</summary>
        /// <exception cref="InvalidOperationException">Nothing a press can reach says it.</exception>
        public Vector2 Find(string text)
        {
            Vector2 at;
            if (!_view.Find(text, out at))
                throw new InvalidOperationException("Nothing on the screen says '" + text + "'.");

            return at;
        }

        /// <summary>What is held down with the pointer, which the game's own screen reads off the keyboard as a press lands.</summary>
        public HeldModifier Held { get; set; }

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
            _workspace.PointerDown(point, PointerButton.Left, presses, Held == HeldModifier.Through, Held == HeldModifier.Also);
        }

        /// <summary>The pointer moving to <paramref name="point" /> with the button down.</summary>
        public void Move(Vector2 point)
        {
            _at = point;
            _workspace.PointerMove(point);
        }

        /// <summary>The button coming up where the pointer last was.</summary>
        /// <remarks>
        /// A press that closes the editor, such as on its close cross, opens it again at once: the editor is the whole of what
        /// this program draws.
        /// </remarks>
        public void Up()
        {
            _workspace.PointerUp(_at, PointerButton.Left);
            if (!_view.IsOpen)
                _workspace.Open(_view);
        }

        /// <summary>Brings the editor up to date and draws it into <paramref name="g" />.</summary>
        public void Draw(ScreenSpaceGraphics g)
        {
            _workspace.Update();

            // Where it goes is the editor's own business, and it is the same answer in the mod.
            _workspace.DrawScreen(g, g.BoundsOf(ScreenArea.Window), g.BoundsOf(ScreenArea.Terminal));
        }
    }
}
