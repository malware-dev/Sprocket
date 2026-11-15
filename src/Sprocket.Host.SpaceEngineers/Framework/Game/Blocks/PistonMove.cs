using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Where a step told the piston to move to, and how fast: each piston at that speed, or all of them at it in total.</summary>
    public sealed class PistonMove
    {
        /// <summary>What a step says for each piston going at the speed given, which it may leave out.</summary>
        public const string Each = "each";

        /// <summary>What a step says for the pistons going at the speed given together, the speed shared out among them.</summary>
        public const string Total = "total";

        const float Hair = 0.0001f;

        readonly Dictionary<long, float> _shares = new Dictionary<long, float>();

        /// <summary>Whether the step gave both; a step that did not moves nothing.</summary>
        public bool Given;

        /// <summary>The position to move to, in meters.</summary>
        public float Position;

        /// <summary>How fast to move, in meters a second, never negative.</summary>
        public float Speed;

        /// <summary>Whether the speed is shared out among the step's pistons rather than given to each.</summary>
        public bool Shared;

        /// <summary>The position and speed a step moving the piston was given, in that order, then whether the speed is in total.</summary>
        public static PistonMove Read(IList<Literal> arguments)
        {
            var move = new PistonMove();
            if (arguments == null || arguments.Count < 2)
                return move;

            move.Position = (float)arguments[0].Real;
            move.Speed = (float)Math.Abs(arguments[1].Real);
            move.Shared = arguments.Count > 2 && arguments[2].Kind == ValueKind.Name && arguments[2].Text == Total;
            move.Given = true;
            return move;
        }

        /// <summary>The field that asks whether the speed is each piston's or all of theirs together, which a step may leave out.</summary>
        public static Field Asked()
        {
            var field = Field.Among("speed for", new[] { Each, "in total" }, new[] { Each, Total });
            field.Entry = FieldInput.Name;
            field.Default = Each;
            field.Quiet = Each;
            field.MayBeLeftOut = true;
            return field;
        }

        /// <summary>
        /// Shares the speed out among <paramref name="pistons" />, every piston the step moves, where it is in total: each gets
        /// its part of the distance all of them have left, so together they go at the speed given and arrive at once.
        /// </summary>
        /// <remarks>Worked out once as the step starts. A piston with no way left keeps the whole speed, which only holds it there.</remarks>
        public void Share(IList<IMyPistonBase> pistons)
        {
            _shares.Clear();
            if (!Shared)
                return;

            var left = 0f;
            foreach (var piston in pistons)
                left += Left(piston);

            if (left <= Hair)
                return;

            foreach (var piston in pistons)
            {
                var way = Left(piston);
                if (way > Hair)
                    _shares[piston.EntityId] = Speed * way / left;
            }
        }

        /// <summary>How fast <paramref name="piston" /> goes: its share where the speed is in total, the speed given otherwise.</summary>
        public float SpeedOf(IMyPistonBase piston)
        {
            float share;
            return _shares.TryGetValue(piston.EntityId, out share) ? share : Speed;
        }

        /// <summary>How far <paramref name="piston" /> has left to the position, within the reach it has.</summary>
        float Left(IMyPistonBase piston)
        {
            var to = Math.Max(piston.LowestPosition, Math.Min(piston.HighestPosition, Position));
            return Math.Abs(to - piston.CurrentPosition);
        }
    }
}
