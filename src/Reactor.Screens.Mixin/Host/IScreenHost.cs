using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Host
{
    /// <summary>What a component view can ask of the screen it is drawn on.</summary>
    public interface IScreenHost
    {
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
