using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Simulation
{
    /// <summary>One thing the game was asked to draw, kept as data rather than done immediately.</summary>
    /// <remarks>
    /// Recorded rather than drawn for two reasons. The near one is mechanical: a canvas only hands out
    /// something to draw with inside its own render pass, while Phosphor lays out and draws in one go and
    /// should not have to care. The far one is the useful one - a frame that exists as a list can be
    /// compared to another frame, which is how this stops being something to look at and starts being
    /// something to assert on.
    /// </remarks>
    public struct DrawCommand
    {
        public DrawKind Kind;
        public MyScreenSpaceLayer Layer;

        public MyStringId Material;
        public Rectangle Destination;
        public Rectangle? Source;
        public float Rotation;

        public string Font;
        public string Text;
        public Vector2 Position;
        public float Scale;
        public MyGuiDrawAlignEnum Align;
        public float MaxWidth;

        public Color Color;
        public Rectangle Clip;
    }
}
