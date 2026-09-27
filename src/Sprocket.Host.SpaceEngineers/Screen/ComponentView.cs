using System.ComponentModel;
using Sprocket.Host.SpaceEngineers.Blueprints;
using Sprocket.Host.SpaceEngineers.Developing;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>One component's elements, kept between ticks so values can be replaced rather than rebuilt.</summary>
    public abstract class ComponentView
    {
        /// <summary>What to put in the layout.</summary>
        public abstract Element Root { get; }

        EditorComponent _watched;

        bool _told = true;

        /// <summary>
        /// Starts listening to the component this view is showing.
        /// </summary>
        /// <remarks>
        /// Paired with <see cref="Release" /> and called from the one place a view is made, so the two
        /// cannot come apart. A component outlives the views that show it - a page makes it once and
        /// thereafter changes its values - so a subscription left behind keeps a dead view alive for the
        /// rest of the session, writing to elements nobody can see.
        /// </remarks>
        internal void Watch(EditorComponent component)
        {
            if (_watched != null)
                Release();

            _watched = component;
            _watched.PropertyChanged += Changed;
        }

        /// <summary>Stops listening. Called from the one place a view is dropped.</summary>
        internal void Release()
        {
            if (_watched == null)
                return;

            _watched.PropertyChanged -= Changed;
            _watched = null;
        }

        /// <summary>Whether this view is still holding a subscription, for a test that counts them.</summary>
        internal bool Watching
        {
            get { return _watched != null; }
        }

        void Changed(object sender, PropertyChangedEventArgs e)
        {
            // A flag and nothing else. The event arrives while core is still writing its screen, so
            // anything read here would be half of one - ObservableList says the same about its own.
            _told = true;
        }

        /// <summary>
        /// Brings this view into line with what it is showing, and moves whatever moves on its own.
        /// </summary>
        /// <remarks>
        /// Not virtual, because the gate is the point: a view writes its values when something has
        /// changed and not otherwise, and the base class holding that is what stops the next view being
        /// written to read everything on every frame.
        /// </remarks>
        internal void Tick(int tick)
        {
            if (_told || Stale)
            {
                _told = false;
                Wires++;
                Refresh();
            }

            Moved(tick);
        }

        /// <summary>
        /// Whether this view has a reason to write its values that nothing announced.
        /// </summary>
        /// <remarks>
        /// True until what it shows announces its changes, because a view whose component has not been
        /// carried over would otherwise stop updating. A view whose component keeps a revision of its
        /// own can answer with that instead, which is a single comparison rather than a screenful of
        /// reading. <see cref="Wires" /> counts what is left to do, and zero on an idle frame is done.
        /// </remarks>
        protected virtual bool Stale
        {
            get { return true; }
        }

        /// <summary>Writes what this view shows onto its elements.</summary>
        protected abstract void Refresh();

        /// <summary>
        /// Moves whatever moves on its own, every tick.
        /// </summary>
        /// <param name="tick">
        /// The editor's own count. Passed rather than fetched, because an indicator that blinks at a rate
        /// needs to know what time it is and core is the only thing that knows what a tick means here.
        /// </param>
        protected virtual void Moved(int tick)
        {
        }

        /// <summary>
        /// How many times this view has written what it shows onto its elements.
        /// </summary>
        /// <remarks>
        /// Counted, because "the values are pushed rather than polled" is a claim about a path that runs
        /// sixty times a second, and the only honest form of that claim is a number that stands still
        /// while nothing is happening. The text measurements in the layout are counted for the same
        /// reason and by the same method.
        /// </remarks>
        internal int Wires { get; private set; }

        /// <summary>
        /// Offers this view the chance to take hold of something at <paramref name="point" />.
        /// </summary>
        /// <remarks>
        /// Asked before an ordinary press, so a grab does not also count as a click on whatever was under
        /// it. A view that takes hold is then given every move until the button comes back up.
        /// </remarks>
        public virtual bool TryGrab(Vector2 point)
        {
            return false;
        }

        /// <summary>Where the thing being carried has got to.</summary>
        public virtual void Carry(Vector2 point)
        {
        }

        /// <summary>Lets go, and puts the result wherever it belongs.</summary>
        public virtual void Drop()
        {
        }

        /// <summary>
        /// Offers a turn of the wheel at <paramref name="point" />, and answers whether it was used.
        /// </summary>
        /// <remarks>
        /// Only a view that scrolls itself has anything to do here, and it is handed the point because
        /// it may well have several regions that scroll - a picker has one per column. Everything else
        /// sits inside the screen's own scroller and is moved by it.
        /// </remarks>
        public virtual bool ScrollBy(Vector2 point, float amount)
        {
            return false;
        }

        /// <summary>
        /// Whether this manages its own scrolling and must not be wrapped in a scroller.
        /// </summary>
        /// <remarks>
        /// True for anything that scrolls sideways or scrolls several regions independently. Wrapping one
        /// of those in a vertical scroller gives two scrollers fighting over the same gesture.
        /// </remarks>
        public virtual bool ScrollsItself
        {
            get { return false; }
        }

        /// <summary>
        /// The fields this view shows as part of the screen, or null where it shows none.
        /// </summary>
        /// <remarks>
        /// Laid out by the view and answered through the screen, which is what takes the keyboard,
        /// the presses and the drags and hands them on - see <see cref="Ask.Column" />.
        /// </remarks>
        public virtual Ask Given
        {
            get { return null; }
        }

        /// <summary>Whether this view gives values in a column of its own, shown or not yet.</summary>
        public virtual bool Gives
        {
            get { return false; }
        }

        /// <summary>The way to finish the screen, where this view carries it rather than the footer, or null.</summary>
        /// <remarks>Answered whether or not it is on the screen yet, so the footer never shows a second one.</remarks>
        public virtual Label Done
        {
            get { return null; }
        }

        /// <summary>Says that what is in <see cref="Given" /> may have changed, for a view that tells core.</summary>
        public virtual void Entered()
        {
        }

        /// <summary>
        /// The screen this view is on, for the few things a view cannot do from inside the layout.
        /// </summary>
        /// <remarks>
        /// Opening something over the top is the case. A view draws where it was arranged, so anything
        /// that has to hang over its neighbours has to be handed to whoever draws last.
        /// </remarks>
        public IScreenHost Host { get; internal set; }

        public static ComponentView For(EditorComponent component)
        {
            var script = component as ScriptComponent;
            if (script != null)
                return new ScriptView(script);

            var blueprint = component as BlueprintComponent;
            if (blueprint != null)
                return new BlueprintView(blueprint);

            // Everything else, drawn loudly. A component this target has not learned to draw must not be
            // silently absent: a screen missing its picker looks exactly like a screen with nothing to
            // pick, and the second is a thing that legitimately happens.
            return new UnsupportedView(component);
        }
    }
}
