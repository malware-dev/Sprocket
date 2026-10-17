using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using MyRotationDirection = Sandbox.ModAPI.Ingame.MyRotationDirection;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Where a step told the rotor to turn to, which way, and how fast.</summary>
    public sealed class RotorTurn
    {
        /// <summary>Whether the step gave all of it; a step that did not turns nothing.</summary>
        public bool Given;

        /// <summary>Which way to turn; a hinge always goes the short way.</summary>
        public MyRotationDirection Way;

        /// <summary>The angle to turn to.</summary>
        public float Angle;

        /// <summary>How fast to turn, never negative.</summary>
        public float Speed;

        /// <summary>The direction, angle and speed a step turning a rotor was given, or the angle and speed for a hinge, which goes the short way.</summary>
        public static RotorTurn Read(IList<Literal> arguments, bool hinge)
        {
            var turn = new RotorTurn { Way = MyRotationDirection.AUTO };

            var at = hinge ? 0 : 1;
            if (arguments == null || arguments.Count < at + 2)
                return turn;

            if (!hinge)
                turn.Way = (MyRotationDirection)arguments[0].Whole;

            turn.Angle = (float)arguments[at].Real;
            turn.Speed = (float)Math.Abs(arguments[at + 1].Real);
            turn.Given = true;
            return turn;
        }
    }
}
