using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>The height a step told a wheel suspension to move its wheel to.</summary>
    public sealed class SuspensionMove
    {
        /// <summary>Whether the step gave a height; a step that did not moves nothing.</summary>
        public bool Given;

        /// <summary>The height to move to, in meters.</summary>
        public float Height;

        /// <summary>The height a step moving the wheel was given.</summary>
        public static SuspensionMove Read(IList<Literal> arguments)
        {
            var move = new SuspensionMove();
            if (arguments == null || arguments.Count < 1)
                return move;

            move.Height = (float)arguments[0].Real;
            move.Given = true;
            return move;
        }
    }
}
