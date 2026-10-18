using System;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sprocket.GravityFit
{
    /// <summary>The boxes a field that fell short of its fit is shown with, for half a minute: what it has to cover, and how far it reaches.</summary>
    public sealed class ShortfallMarks
    {
        const double Shown = 30.0;

        const float Line = 0.1f;

        static readonly MyStringId NeededLine = MyStringId.GetOrCompute("GizmoDrawLineRed");
        static readonly MyStringId ReachedLine = MyStringId.GetOrCompute("GizmoDrawLineWhite");

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
        /// <remarks>Called every frame; with nothing to show it costs a count.</remarks>
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
                    null, NeededLine, false, -1, MyBillboard.BlendTypeEnum.LDR);
                MySimpleObjectDraw.DrawTransparentBox(ref field, ref mark.Reached, ref white, MySimpleObjectRasterizer.Wireframe, 1, Line,
                    null, ReachedLine, false, -1, MyBillboard.BlendTypeEnum.LDR);
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
