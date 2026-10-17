using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Where a step told the piston to move to, and how fast.</summary>
    public sealed class PistonMove
    {
        /// <summary>Whether the step gave both; a step that did not moves nothing.</summary>
        public bool Given;

        /// <summary>The position to move to, in meters.</summary>
        public float Position;

        /// <summary>How fast to move, in meters a second, never negative.</summary>
        public float Speed;

        /// <summary>The position and speed a step moving the piston was given, in that order.</summary>
        public static PistonMove Read(IList<Literal> arguments)
        {
            var move = new PistonMove();
            if (arguments == null || arguments.Count < 2)
                return move;

            move.Position = (float)arguments[0].Real;
            move.Speed = (float)Math.Abs(arguments[1].Real);
            move.Given = true;
            return move;
        }
    }
}
