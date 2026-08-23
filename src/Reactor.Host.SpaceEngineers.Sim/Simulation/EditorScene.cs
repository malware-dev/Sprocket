using Phosphor;
using Ship = Reactor.Host.System.Simulation;
using Reactor.Screens;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Reactor itself, running on a made-up ship, drawn the way the mod will draw it.
    /// <para>
    /// Three things meet here and none of them knows about the other two: core decides what is on screen,
    /// <see cref="EditorView" /> decides how this target draws it, and the simulated ship supplies a world
    /// and a clock. The same three meet in the mod, with the game supplying the third.
    /// </para>
    /// </summary>
    public sealed class EditorScene
    {
        readonly Ship _ship = new Ship();
        readonly EditorView _view;

        public EditorScene()
        {
            _view = new EditorView(_ship.Editor);
        }

        /// <summary>Where the program is being kept, for the line at the bottom of the window.</summary>
        public string Where
        {
            get { return _ship.Where; }
        }

        /// <summary>
        /// The things on the made-up ship that can be poked at from outside.
        /// </summary>
        /// <remarks>
        /// For the simulator's own scaffolding and nothing else. Tripping a sensor or draining a tank is
        /// something the world does, and in the game a ship actually flies up to the sensor - so none of
        /// this is Reactor, and none of it goes anywhere the mod can compile.
        /// </remarks>
        internal System.Collections.Generic.IEnumerable<Reactor.Host.System.SimEntity> Pokeable()
        {
            return _ship.Pokeable();
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

        /// <summary>Whether a question is open and wants the keyboard.</summary>
        public bool IsAsking
        {
            get { return _view.IsAsking; }
        }

        /// <summary>The box that question is being answered in.</summary>
        public Phosphor.TextBox AskBox
        {
            get { return _view.AskBox; }
        }

        public bool CaretVisible
        {
            set { _view.CaretVisible = value; }
        }

        public void CommitAsk()
        {
            _view.CommitAsk();
        }

        public void CancelAsk()
        {
            _view.CancelAsk();
        }

        /// <summary>The box the keyboard is in, when something on the screen has taken it.</summary>
        public Phosphor.TextBox Focus
        {
            get { return _view.Focus; }
        }

        /// <summary>Sends what was just typed wherever it belongs.</summary>
        public void Typed()
        {
            _view.Typed();
        }

        /// <summary>Moves the scrolling region. Positive scrolls down.</summary>
        public void ScrollBy(float amount)
        {
            _view.ScrollBy(amount);
        }

        /// <summary>Presses whatever is at <paramref name="point" />, in UI units.</summary>
        public bool Press(Vector2 point, int clicks)
        {
            return _view.Press(point, clicks);
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

        public bool IsDragging
        {
            get { return _view.IsDragging; }
        }

        public void Draw(ScreenSpaceGraphics g)
        {
            Vector2 size = g.Size;

            _view.Update();

            // A column down the middle, roughly where a terminal panel sits. Not a considered layout yet:
            // the point of this frame is to find out what the real screens need, and guessing at chrome
            // before seeing them would be deciding it twice.
            const float width = 620f;
            _view.Draw(g, new RectangleF((size.X - width) * 0.5f, 48f, width, size.Y - 96f));
        }
    }
}
