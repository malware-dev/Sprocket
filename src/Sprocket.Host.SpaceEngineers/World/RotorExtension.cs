using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Layout;

// The direction lives in the Ingame namespace, which cannot be imported whole: IMyMotorStator is
// declared in both, and the mod-side one is the one with the event on it.
using MyRotationDirection = Sandbox.ModAPI.Ingame.MyRotationDirection;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A rotor: turned to an angle, and the members of its terminal said in Sprocket's words.
    /// </summary>
    /// <remarks>
    /// Turning to an angle is the game's own: it puts a limit at the angle and runs the rotor at it,
    /// so the rotor's limits are the game's to overwrite, and are. The rotor has arrived when it has
    /// reached the limit it is running towards - the test the game itself makes to say a limit was
    /// reached. Its angle, its limits and its speed are all the block's own, which the game saves, so
    /// a turn under way survives a reload and is still known to be under way.
    /// <para>
    /// Hinges share the rotor's type and are not claimed here. The game tells the two apart by the
    /// definition, and a hinge has hard limits of its own.
    /// </para>
    /// </remarks>
    sealed class RotorExtension : IBlockExtension
    {
        /// <summary>Turning to an angle, which is Sprocket's own and waits until it gets there.</summary>
        public const string RotateTo = "rotate-to";

        /// <summary>How near a limit a rotor counts as at it, in radians. Transcribed from the game's own test.</summary>
        const float Hair = 0.0001f;

        const string Arrived = "arrived";
        const string There = "is-there";

        /// <summary>What this world's rotors were last seen doing. See <see cref="RotorTurn" />.</summary>
        readonly RotorTurn _turn = new RotorTurn();

        /// <summary>The game's own names for the three directions, which are the values the game is told.</summary>
        /// <remarks>Transcribed from the game's <c>MyRotationDirection</c>: shortest way, then clockwise, then counter-clockwise.</remarks>
        static readonly string[] Directions = { "0", "1", "2" };

        static readonly string[] Ways = { "directly", "clockwise", "counter-clockwise" };

        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyMotorStator && !Hinge(block);
        }

        /// <summary>Whether the block's definition says it is a hinge rather than a rotor.</summary>
        /// <remarks>
        /// Asked of the definition because nothing else says: a rotor and a hinge are the same type
        /// with the same terminal. With no definitions loaded - the test harness - a rotor is a rotor.
        /// </remarks>
        static bool Hinge(IMyTerminalBlock block)
        {
            var definition = Definition(block);
            return definition != null && definition.RotorType == MyRotorType.Hinge;
        }

        /// <summary>What the game says this block is, or null where nothing has been loaded to say.</summary>
        static MyMotorStatorDefinition Definition(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            return definitions == null
                ? null
                : definitions.GetCubeBlockDefinition(block.BlockDefinition) as MyMotorStatorDefinition;
        }

        /// <summary>
        /// How fast the block will turn, in revolutions a minute.
        /// </summary>
        /// <remarks>
        /// The block's own, which differs between a rotor and a small one. A revolution a minute is
        /// two pi radians a minute, which is where the sixty comes from.
        /// </remarks>
        static float Fastest(IMyTerminalBlock block)
        {
            var rotor = block as IMyMotorStator;
            return rotor == null ? 0f : rotor.MaxRotorAngularVelocity * 60f / (2f * (float)Math.PI);
        }

        /// <summary>A bound as a field writes one: whole where it is whole.</summary>
        static string Said(float number)
        {
            return number == (float)Math.Floor(number)
                ? ((int)number).ToString(CultureInfo.InvariantCulture)
                : number.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>Nothing to arrange: arriving is read off the rotor.</summary>
        public bool Raises(IMyTerminalBlock block, string eventId, string key)
        {
            return true;
        }

        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithProperty(Arrived, "at its angle")
                .WithState(There, Arrived, true, "is at its angle")
                .WithAction(RotateTo, "rotate", There, Way(), Angle(block), Speed(block))
                .Lasting().Replacing("RotateToAngle").In(Category.Movement)
                .Promoting("Reverse", false, "reverse", "reverse")
                .Promoting("Attach", false, "attach", "attach")
                .Promoting("Detach", false, "detach", "detach")
                .Promoting("RotorLock_On", false, "lock", "lock")
                .Promoting("RotorLock_Off", false, "unlock", "unlock")
                .Promoting("Velocity", true, "speed", "speed")
                .Promoting("LowerLimit", true, "lower-limit", "lower limit")
                .Promoting("UpperLimit", true, "upper-limit", "upper limit")
                .Promoting("Torque", true, "torque", "torque")
                .Promoting("BrakingTorque", true, "braking-torque", "braking torque");
        }

        static Field Way()
        {
            var way = Field.Among("direction", Ways, Directions);
            way.Needed = true;
            way.Default = Ways[0];
            return way;
        }

        /// <summary>How far round the block will go, which is the block's own to say.</summary>
        static Field Angle(IMyTerminalBlock block)
        {
            float least = -360f;
            float most = 360f;

            var definition = Definition(block);
            if (definition != null)
            {
                least = definition.MinAngleDeg ?? least;
                most = definition.MaxAngleDeg ?? most;
            }

            var angle = Field.Between("angle", Said(least), Said(most));
            angle.Needed = true;
            angle.Edits = Editor.Angle;
            angle.Lead = "to";
            angle.Unit = "°";
            return angle;
        }

        static Field Speed(IMyTerminalBlock block)
        {
            // A turn at no speed sets out for an angle it can never reach, so there is no reading of an
            // empty speed that means anything.
            var speed = Field.Between("speed", "0", Said(Fastest(block)));
            speed.Needed = true;
            speed.Above = true;
            speed.Default = "1";
            speed.Lead = "at";
            speed.Unit = " rpm";
            return speed;
        }

        public bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value)
        {
            value = default(EntityValue);

            var rotor = block as IMyMotorStator;
            if (rotor == null || propertyId != Arrived)
                return false;

            value = EntityValue.Of(AtLimit(rotor));
            return true;
        }

        /// <summary>Whether the rotor has reached the limit it is running towards.</summary>
        /// <remarks>
        /// The game's own test for a limit reached, tolerance and all, on the rotor's own angle and
        /// limits: a rotor held against a limit rests within a hair of it rather than on it, and the
        /// game counts it there within a ten-thousandth of a radian. A rotor that is not running is
        /// running towards nothing, and has arrived nowhere.
        /// </remarks>
        static bool AtLimit(IMyMotorStator rotor)
        {
            var speed = rotor.TargetVelocityRPM;
            if (speed > 0)
                return rotor.Angle > rotor.UpperLimitRad - Hair;

            if (speed < 0)
                return rotor.Angle < rotor.LowerLimitRad + Hair;

            return false;
        }

        public bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var rotor = block as IMyMotorStator;
            if (rotor == null || actionId != RotateTo)
                return false;

            MyRotationDirection way;
            float angle;
            float speed;
            if (!Given(values, out way, out angle, out speed))
                return true;

            if (Already(rotor, angle))
                Hold(rotor, way, angle, speed);
            else
                rotor.RotateToAngle(way, angle, speed);

            _turn.Began(rotor);
            return true;
        }

        /// <summary>Whether the rotor already stands at the angle, by the game's own test.</summary>
        /// <remarks>
        /// The game does nothing at all when told to turn to where a rotor already is: no limit, no
        /// speed. A rotor not already pushing that way would then never read as arrived, and a sequence
        /// that turns something to where it last left it is run every time an airlock cycles.
        /// </remarks>
        static bool Already(IMyMotorStator rotor, float degrees)
        {
            return Math.Abs(degrees * (float)Math.PI / 180f - rotor.Angle) <= float.Epsilon;
        }

        /// <summary>
        /// Leaves the rotor as a finished turn to where it stands would: the limit at the angle, the
        /// other one taken away, and the speed pushing against it.
        /// </summary>
        static void Hold(IMyMotorStator rotor, MyRotationDirection way, float degrees, float speed)
        {
            var limit = degrees * (float)Math.PI / 180f;
            if (way == MyRotationDirection.CCW)
            {
                rotor.UpperLimitRad = float.MaxValue;
                rotor.LowerLimitRad = limit;
                rotor.TargetVelocityRPM = -speed;
            }
            else
            {
                rotor.LowerLimitRad = float.MinValue;
                rotor.UpperLimitRad = limit;
                rotor.TargetVelocityRPM = speed;
            }
        }

        /// <summary>The direction, angle and speed a step turning the rotor was given, in that order.</summary>
        static bool Given(IList<Literal> values, out MyRotationDirection way, out float angle, out float speed)
        {
            way = MyRotationDirection.AUTO;
            angle = 0;
            speed = 0;

            if (values == null || values.Count < 3)
                return false;

            way = (MyRotationDirection)(long)values[0].Number;
            angle = (float)values[1].Number;
            speed = (float)Math.Abs(values[2].Number);
            return true;
        }

        /// <summary>
        /// Stops the rotor where it stands.
        /// </summary>
        /// <remarks>
        /// A rotor is the reason this exists. What it was told to do is to run at a speed until it
        /// meets a limit, so a run that ends while it is turning would leave it turning - and the
        /// sequence that was watching for a reason to stop it is exactly the one that has just
        /// stopped. Its limits are left where the turn put them; the speed is what makes it move.
        /// </remarks>
        public void Stop(IMyTerminalBlock block, string actionId)
        {
            var rotor = block as IMyMotorStator;
            if (rotor != null && actionId == RotateTo)
                rotor.TargetVelocityRPM = 0f;
        }

        /// <summary>
        /// Why the rotor is not getting to its angle, or nothing while it still might.
        /// </summary>
        /// <remarks>
        /// In the order somebody would look: whether it can turn at all, then whether it was told to,
        /// then whether something is holding it. A rotor whose speed has been taken away is the one
        /// answer worth giving up on - it was set by the turn, so nought means something else has
        /// been at it and nothing is heading for the angle any more.
        /// </remarks>
        public Trouble TroubleWith(IMyTerminalBlock block, string stateId)
        {
            var rotor = block as IMyMotorStator;
            if (rotor == null || stateId != There || AtLimit(rotor))
                return Trouble.None;

            if (!rotor.IsWorking)
                return Trouble.Waiting("cannot turn - it is switched off or has no power");

            if (!rotor.IsAttached)
                return Trouble.Waiting("cannot turn - it has no head");

            if (rotor.RotorLock)
                return Trouble.Waiting("cannot turn - it is locked");

            if (rotor.Torque <= 0f)
                return Trouble.Waiting("cannot turn - its torque is nought");

            if (rotor.TargetVelocityRPM == 0f)
                return Trouble.Fatal("stopped turning - its speed was set to nought");

            // Waited through rather than given up on. Whatever is holding the rotor may be moved, and
            // the rotor leaning on it meanwhile is no worse than the rotor stopped: measured in game,
            // taking its speed away does not settle what the pushing does to a construct.
            return _turn.Turning(rotor)
                ? Trouble.None
                : Trouble.Waiting("cannot turn - something is holding it");
        }

        public string StatusOf(IMyTerminalBlock block)
        {
            return null;
        }

        public bool IsBusy(IMyTerminalBlock block)
        {
            var rotor = block as IMyMotorStator;
            return rotor != null && rotor.TargetVelocityRPM != 0 && !AtLimit(rotor);
        }

        /// <summary>
        /// The ticks the turn should take: the way round it goes, at the speed it was given.
        /// </summary>
        /// <remarks>
        /// A revolution a minute is six degrees a second, and the game runs sixty ticks to the second,
        /// so a rotor turns a tenth of a degree a tick for every revolution a minute it is given.
        /// </remarks>
        public int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            var rotor = block as IMyMotorStator;

            MyRotationDirection way;
            float angle;
            float speed;
            if (rotor == null || actionId != RotateTo || !Given(values, out way, out angle, out speed) || speed <= 0)
                return GridEntity.Unestimated;

            var from = Degrees(rotor.Angle);
            var to = Wrapped(angle);
            var clockwise = Wrapped(to - from);
            var counter = Wrapped(from - to);

            float degrees;
            if (way == MyRotationDirection.CW)
                degrees = clockwise;
            else if (way == MyRotationDirection.CCW)
                degrees = counter;
            else
                degrees = Math.Min(clockwise, counter);

            return (int)Math.Ceiling(degrees / (speed / 10f));
        }

        static float Degrees(float radians)
        {
            return Wrapped(radians * 180f / (float)Math.PI);
        }

        /// <summary>An angle brought into one turn, nought to three hundred and sixty.</summary>
        static float Wrapped(float degrees)
        {
            var wrapped = degrees % 360f;
            return wrapped < 0 ? wrapped + 360f : wrapped;
        }
    }
}
