using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Picking;
using Reactor.Host.SpaceEngineers.Sequences;
using VRageMath;
using Reactor.Host.SpaceEngineers.Layout;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>One component's elements, kept between ticks so values can be replaced rather than rebuilt.</summary>
    public abstract class ComponentView
    {
        /// <summary>What to put in the layout.</summary>
        public abstract Element Root { get; }

        /// <summary>
        /// Copies this tick's values onto the elements.
        /// </summary>
        /// <param name="tick">
        /// The editor's own count. Passed rather than fetched, because an indicator that blinks at a rate
        /// needs to know what time it is and core is the only thing that knows what a tick means here.
        /// </param>
        public abstract void Update(int tick);

        /// <summary>Adds whatever in here can be pressed. Called after the layout, never before.</summary>
        public virtual void Collect(List<Hit> hits)
        {
        }

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
        /// The screen this view is on, for the few things a view cannot do from inside the layout.
        /// </summary>
        /// <remarks>
        /// Opening something over the top is the case. A view draws where it was arranged, so anything
        /// that has to hang over its neighbours has to be handed to whoever draws last.
        /// </remarks>
        public IScreenHost Host { get; internal set; }

        public static ComponentView For(EditorComponent component)
        {
            var heading = component as HeadingComponent;
            if (heading != null)
                return new HeadingView(heading);

            var notice = component as NoticeComponent;
            if (notice != null)
                return new NoticeView(notice);

            var start = component as StartComponent;
            if (start != null)
                return new StartView(start);

            var choices = component as ChoiceListComponent;
            if (choices != null)
                return new ChoiceListView(choices);

            var status = component as StatusListComponent;
            if (status != null)
                return new StatusListView(status);

            var pick = component as PickComponent;
            if (pick != null)
                return new PickView(pick);

            var sequence = component as SequenceComponent;
            if (sequence != null)
                return new SequenceView(sequence);

            // Everything else, drawn loudly. A component this target has not learned to draw must not be
            // silently absent: a screen missing its picker looks exactly like a screen with nothing to
            // pick, and the second is a thing that legitimately happens.
            return new UnsupportedView(component);
        }
    }
}
