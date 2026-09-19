using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>
    /// The ways to start something new on this screen.
    /// </summary>
    /// <remarks>
    /// Every screen that holds a list of things somebody made has one of these above it: the
    /// ship offers a new system or a loaded one, a system offers the ways a sequence can
    /// begin. The shape is deliberately the same in both places, because it is the same
    /// question - what would you like to add here - and a screen whose top half is learnt once
    /// is learnt everywhere.
    ///
    /// An option that needs an answer before anything can happen carries the question it wants
    /// asked. How it is asked is entirely the target's business: a panel under the button, a
    /// modal, a line at the top of the screen, a prompt on a terminal. Core says what is needed
    /// and what to send back, and nothing about where it appears or how it opens.
    ///
    /// So there is no half-open state here. Core is not told that somebody is currently being
    /// asked, only that an answer arrived - which means an abandoned question leaves nothing
    /// behind anywhere, because nothing was ever started.
    /// </remarks>
    public sealed class StartComponent : EditorComponent
    {
        readonly List<StartOption> _options = new List<StartOption>();

        public List<StartOption> Options => _options;

        public StartOption Add(string label, int command)
        {
            var option = new StartOption(label, command);
            _options.Add(option);
            return option;
        }

        /// <summary>Adds one that has to be answered before anything happens.</summary>
        public StartOption Add(string label, string question, int command)
        {
            var option = Add(label, command);
            option.Question = question;
            return option;
        }
    }
}
