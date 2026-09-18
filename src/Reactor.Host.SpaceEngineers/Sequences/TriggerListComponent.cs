using System.Collections.Generic;
using System.ComponentModel;
using Reactor.Host.SpaceEngineers.Screen;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What starts the things a system does, each written out as its own sentence.
    /// </summary>
    /// <remarks>
    /// Its own component rather than rows in the list below it, because a trigger is not a thing you
    /// open - it is a sentence you change in place, and a row that could only be pressed as a whole
    /// had to ask which half was meant.
    /// </remarks>
    public sealed class TriggerListComponent : EditorComponent
    {
        public TriggerListComponent()
        {
            Triggers = new ObservableList<SentenceTrigger>();
            Triggers.Changed += Listed;
        }

        /// <summary>The ones being repeated for, so each can be let go of again.</summary>
        readonly List<SentenceTrigger> _heard = new List<SentenceTrigger>();

        /// <summary>
        /// Repeats what a trigger says as something this list said.
        /// </summary>
        /// <remarks>
        /// A page writes each row's live state every tick from what is running, and each of those
        /// announces only when it actually moves. Gathering them here means a target listens to the
        /// list rather than to every row of it, and lets go of one thing rather than of a list it
        /// has to keep in step.
        /// </remarks>
        void Listed()
        {
            for (var i = 0; i < _heard.Count; i++)
                _heard[i].PropertyChanged -= Said;

            _heard.Clear();

            foreach (SentenceTrigger trigger in Triggers)
            {
                trigger.PropertyChanged += Said;
                _heard.Add(trigger);
            }

            Raise(nameof(Triggers));
        }

        void Said(object sender, PropertyChangedEventArgs e)
        {
            Raise(nameof(Triggers));
        }

        public ObservableList<SentenceTrigger> Triggers { get; private set; }
    }
}
