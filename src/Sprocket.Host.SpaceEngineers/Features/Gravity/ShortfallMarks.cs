using System;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sprocket.Host.SpaceEngineers.Features.Gravity
{
    /// <summary>The boxes a field that fell short of its fit is shown with, for half a minute: what it has to cover, and how far it reaches.</summary>
    /// <remarks>
    /// Each box follows its grid, so a ship that moves keeps them where they belong. Drawn in the game's own gizmo lines, red
    /// for what must be covered and white for the field, as the game's block builder draws its own boxes.
    /// </remarks>
    public sealed class ShortfallMarks
    {
        /// <summary>How long a fit's boxes stay, in seconds: long enough to close the terminal and look.</summary>
        const double Shown = 30.0;

        /// <summary>How thick the lines are drawn, in metres.</summary>
        const float Line = 0.1f;

        static readonly MyStringId Needed = MyStringId.GetOrCompute("GizmoDrawLineRed");
        static readonly MyStringId Reached = MyStringId.GetOrCompute("GizmoDrawLineWhite");

        readonly List<Mark> _marks = new List<Mark>();

        /// <summary>
        /// Shows, until <paramref name="now" /> and half a minute, the cells <paramref name="needed" /> of
        /// <paramref name="generator" />'s grid in that grid's space, and its field <paramref name="reached" /> in its own, in place
        /// of whatever it is showing for that generator.
        /// </summary>
        public void Show(IMyGravityGenerator generator, BoundingBoxD needed, BoundingBoxD reached, TimeSpan now)
        {
            for (var i = _marks.Count - 1; i >= 0; i--)
            {
                if (_marks[i].Generator == generator)
                    _marks.RemoveAt(i);
            }

            _marks.Add(new Mark { Generator = generator, Needed = needed, Reached = reached, Until = now + TimeSpan.FromSeconds(Shown) });
        }

        /// <summary>Draws every box still to be shown at <paramref name="now" />, and lets go of the rest.</summary>
        /// <remarks>Every frame, because a box in the world is drawn for one frame at a time; with nothing to show it costs a count.</remarks>
        public void Draw(TimeSpan now)
        {
            if (_marks.Count == 0)
                return;

            var white = Color.White;
            for (var i = _marks.Count - 1; i >= 0; i--)
            {
                var mark = _marks[i];
                var generator = mark.Generator;
                if (now > mark.Until || generator.Closed || generator.CubeGrid == null)
                {
                    _marks.RemoveAt(i);
                    continue;
                }

                var grid = generator.CubeGrid.WorldMatrix;
                var field = generator.WorldMatrix;
                MySimpleObjectDraw.DrawTransparentBox(ref grid, ref mark.Needed, ref white, MySimpleObjectRasterizer.Wireframe, 1, Line,
                    null, Needed, false, -1, MyBillboard.BlendTypeEnum.LDR);
                MySimpleObjectDraw.DrawTransparentBox(ref field, ref mark.Reached, ref white, MySimpleObjectRasterizer.Wireframe, 1, Line,
                    null, Reached, false, -1, MyBillboard.BlendTypeEnum.LDR);
            }
        }

        sealed class Mark
        {
            public IMyGravityGenerator Generator;
            public BoundingBoxD Needed;
            public BoundingBoxD Reached;
            public TimeSpan Until;
        }
    }
}
