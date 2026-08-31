using System.Collections.Generic;
using Reactor.Ui;
using Reactor.World;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Host.SpaceEngineers.Presentation.Screen
{
    /// <summary>What a component view can ask of the screen it is drawn on.</summary>
    public interface IScreenHost
    {
        /// <summary>
        /// What the editor is editing, for the views that have words to put on it.
        /// </summary>
        /// <remarks>
        /// Core names blocks by id and never by name, because a name is read off this and what to say
        /// about one that is no longer here is this target's to phrase. So a view that renders a
        /// sentence needs the world the sentence is about.
        /// </remarks>
        IWorld World { get; }

        /// <summary>Hangs <paramref name="content" /> over the screen, under <paramref name="anchor" />.</summary>
        void ShowOverlay(Element content, Element anchor);

        /// <summary>Takes away whatever is hanging over the screen.</summary>
        void HideOverlay();

        /// <summary>Whether <paramref name="content" /> is the thing currently hanging over the screen.</summary>
        bool IsOverlay(Element content);

        /// <summary>Sends a number to core. Reordering is the one thing a view answers with a position.</summary>
        void SetNumber(int command, int value);

        /// <summary>Sends a switch's new state to core.</summary>
        void SetToggle(int command, bool on);
    }
}
