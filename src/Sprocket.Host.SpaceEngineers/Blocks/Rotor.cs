using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;

// The direction lives in the Ingame namespace, which cannot be imported whole: IMyMotorStator is
// declared in both, and the mod-side one is the one with the event on it.
using MyRotationDirection = Sandbox.ModAPI.Ingame.MyRotationDirection;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A rotor or a hinge, turned to an angle.</summary>
    /// <remarks>A turn under way survives a reload: its angle, limits and speed are the block's own, which the game saves.</remarks>
    public sealed class Rotor : Capability
    {
        /// <summary>Turning to an angle, which is Sprocket's own and waits until it gets there.</summary>
        public const string RotateTo = "rotate-to";

        /// <summary>Adding a head, which is the game's own, in one of the sizes the block offers.</summary>
        public const string AddHead = "add-head";

        const float Hair = 0.0001f;

        const string Turning = "A rotor raises its angle on every tick it turns, too often to listen to.";

        const string Unheard = "A rotor raises nothing as it is locked or unlocked.";

        static readonly string[] Directions = { "0", "1", "2" };

        static readonly string[] Ways = { "directly", "clockwise", "counter-clockwise" };

        static readonly string[] Sizes = { "normal", "medium", "small" };

        static readonly Reading Arrived = Reading.Of<IMyMotorStator>(AtLimit);

        static readonly Reading Held = Reading.Of<IMyMotorStator>(rotor => rotor.RotorLock);

        static readonly Reading Joined = Reading.Of<IMyMotorStator>(rotor => rotor.IsAttached);

        // The game's angle is in radians; the terminal says degrees, a rotor's nought to three hundred and sixty and a
        // hinge's a quarter turn either way.
        static readonly OwnValue RotorAngle = OwnValue.Number<IMyMotorStator>(rotor => Degrees(rotor.Angle));

        static readonly OwnValue HingeAngle = OwnValue.Number<IMyMotorStator>(rotor => rotor.Angle * 180.0 / Math.PI);

        static readonly OwnValue Locked = OwnValue.Flag(Held);

        static readonly OwnValue Attached = OwnValue.Flag(Joined);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyMotorStator;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            // A hinge's lock is its own, and a hinge goes the short way whatever it is told.
            var hinge = Hinge(block);
            var locks = hinge ? "HingeLock" : "RotorLock";

            var turns = OwnAction.Of<IMyMotorStator, Turn>(given => Turn.Read(given, hinge), Turned, Arrived, null, Why,
                Stop, Expected);

            if (hinge)
                kind.WithAction(RotateTo, "rotate", turns, AskedAngle(block), AskedSpeed(block));
            else
                kind.WithAction(RotateTo, "rotate", turns, AskedWay(), AskedAngle(block), AskedSpeed(block));

            kind.Lasting().Listed("rotate to").In(Category.Movement)
                .Hiding("RotateToAngle", "ResetVelocity")
                .Renaming("Reverse", false, "reverse")
                .Renaming("Attach", false, "attach")
                .Renaming("Detach", false, "detach")
                .Renaming($"{locks}_On", false, "lock")
                .Renaming($"{locks}_Off", false, "unlock")
                .Renaming(locks, false, "toggle lock")
                .Renaming("Velocity", true, "velocity")
                .Measuring("Velocity", " rpm")
                .Renaming("LowerLimit", true, "lower limit")
                .Measuring("LowerLimit", "°")
                .Renaming("UpperLimit", true, "upper limit")
                .Measuring("UpperLimit", "°")
                .Renaming("Torque", true, "torque")
                .Measuring("Torque", " N*m")
                .Renaming("BrakingTorque", true, "braking torque")
                .Measuring("BrakingTorque", " N*m")
                .Renaming("Displacement", true, "displacement")
                .Measuring("Displacement", " m")
                .Renaming("SafetyDetach", true, "safety detach distance")
                .Measuring("SafetyDetach", " m")
                .Renaming("ShareInertiaTensor", true, "share inertia tensor")
                .Renaming("ShareInertiaTensor", false, "toggle share inertia tensor");

            var heads = Heads(block, hinge);
            var adds = OwnAction.Of<IMyMotorStator, string>(given => Head(given, heads), Added);
            if (heads[1] != null || heads[2] != null)
                kind.WithAction(AddHead, "add head", adds, AskedSize(heads)).In(Category.Repair);
            else
                kind.WithAction(AddHead, "add head", adds).In(Category.Repair);

            kind.WithValue("angle", "angle", hinge ? HingeAngle : RotorAngle).Measured("°").Looked(Turning).In(Category.Movement)
                .WithValue("locked", "locked", Locked).Looked(Unheard).In(Category.Movement)
                .WithValue("attached", "attached", Attached).Status().Announced()
                .WithChange("attaches", "attaches", "attached", Literal.Of(true)).Announced()
                .Reading("is attached", "is not attached")
                .WithChange("detaches", "detaches", "attached", Literal.Of(false)).Announced()
                .Reading("is detached", "is attached");
        }

        /// <summary>Told as the head is attached or detached.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var rotor = block as IMyMotorStator;
            return rotor == null ? null : new Listening(rotor, told);
        }

        /// <inheritdoc />
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var rotor = block as IMyMotorStator;
            return rotor != null && rotor.TargetVelocityRPM != 0 && !AtLimit(rotor);
        }

        /// <summary>
        /// The game's actions that add a head to this kind of block, by size: normal, medium and small, each null where the
        /// block does not offer it.
        /// </summary>
        static string[] Heads(IMyTerminalBlock block, bool hinge)
        {
            var stem = hinge ? "Hinge" : "Rotor";
            var heads = new string[Sizes.Length];
            heads[0] = $"Add{stem}TopPart";
            if (block.CubeGrid == null || block.CubeGrid.GridSizeEnum != MyCubeSize.Large)
                return heads;

            var definition = Definition(block);
            if (definition == null || definition.MediumTopPart != null)
                heads[1] = $"AddMedium{stem}TopPart";

            heads[2] = $"AddSmall{stem}TopPart";
            return heads;
        }

        /// <summary>The field that asks which size of head to add, among the sizes <paramref name="heads" /> offers.</summary>
        /// <remarks>A size is written as its place among normal, medium and small, whichever of them a block offers.</remarks>
        static Field AskedSize(string[] heads)
        {
            var words = new List<string>();
            var means = new List<string>();
            for (var i = 0; i < heads.Length; i++)
            {
                if (heads[i] == null)
                    continue;

                words.Add(Sizes[i]);
                means.Add(i.ToString(CultureInfo.InvariantCulture));
            }

            var size = Field.Among("size", words, means);
            size.Entry = FieldInput.Whole;
            size.Needed = true;
            size.Default = Sizes[0];
            return size;
        }

        /// <summary>The game's action for the size of head a step asked for, among <paramref name="heads" />; null where it asked for one the block does not offer.</summary>
        static string Head(IList<Literal> arguments, string[] heads)
        {
            var at = arguments == null || arguments.Count == 0 ? 0 : arguments[0].Whole;
            return at >= 0 && at < heads.Length ? heads[at] : null;
        }

        /// <summary>Adds the head through the game's own action, which does nothing while one is attached.</summary>
        static void Added(IMyMotorStator rotor, string head, ReaderMemory seen)
        {
            if (head != null)
                Toolbars.Perform(rotor, head, null);
        }

        /// <summary>Whether the block's definition says it is a hinge rather than a rotor.</summary>
        static bool Hinge(IMyTerminalBlock block)
        {
            var definition = Definition(block);
            if (definition != null)
                return definition.RotorType == MyRotorType.Hinge;

            var subtype = block.BlockDefinition.SubtypeName;
            return subtype != null && subtype.IndexOf("Hinge", StringComparison.Ordinal) >= 0;
        }

        /// <summary>What the game says this block is, or null where nothing has been loaded to say.</summary>
        static MyMotorStatorDefinition Definition(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            return definitions?.GetCubeBlockDefinition(block.BlockDefinition) as MyMotorStatorDefinition;
        }

        /// <summary>How fast the block will turn at most, in revolutions a minute.</summary>
        static float Fastest(IMyTerminalBlock block)
        {
            var rotor = block as IMyMotorStator;
            // A revolution a minute is two pi radians a minute, which is where the sixty comes from.
            return rotor == null ? 0f : rotor.MaxRotorAngularVelocity * 60f / (2f * (float)Math.PI);
        }

        /// <summary>A bound as a field writes one: whole where it is whole.</summary>
        static string Said(float number)
        {
            return number == (float)Math.Floor(number)
                ? ((int)number).ToString(CultureInfo.InvariantCulture)
                : number.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static Field AskedWay()
        {
            // The game's own values for MyRotationDirection: shortest way, then clockwise, then counter-clockwise.
            var way = Field.Among("direction", Ways, Directions);
            way.Entry = FieldInput.Whole;
            way.Needed = true;
            way.Default = Ways[0];
            return way;
        }

        /// <summary>How far round the block will go, which is the block's own to say.</summary>
        static Field AskedAngle(IMyTerminalBlock block)
        // Every hinge the game has is built to a quarter turn either way, which holds where no definition says otherwise.
        {
            var reach = Hinge(block) ? 90f : 360f;
            float least = -reach;
            float most = reach;

            var definition = Definition(block);
            if (definition != null)
            {
                least = definition.MinAngleDeg ?? least;
                most = definition.MaxAngleDeg ?? most;
            }

            var angle = Field.Between("angle", Said(least), Said(most));
            angle.Needed = true;
            angle.Entry = FieldInput.Decimal;
            angle.Edits = FieldEditor.Angle;
            angle.Lead = "to";
            angle.Unit = "°";
            return angle;
        }

        static Field AskedSpeed(IMyTerminalBlock block)
        {
            // A turn at no speed sets out for an angle it can never reach, so there is no reading of an
            // empty speed that means anything.
            var speed = Field.Between("speed", "0", Said(Fastest(block)));
            speed.Needed = true;
            speed.Above = true;
            speed.Entry = FieldInput.Decimal;
            speed.Default = "1";
            speed.Lead = "at";
            speed.Unit = " rpm";
            return speed;
        }

        /// <summary>Whether the rotor has reached the limit it is running towards.</summary>
        /// <remarks>The game's own test for a limit reached; a rotor that is not running has arrived nowhere.</remarks>
        static bool AtLimit(IMyMotorStator rotor)
        {
            var speed = rotor.TargetVelocityRPM;
            if (speed > 0)
                // Hair is how near a limit counts as at it, in radians, transcribed from the game's own test.
                return rotor.Angle > rotor.UpperLimitRad - Hair;

            if (speed < 0)
                return rotor.Angle < rotor.LowerLimitRad + Hair;

            return false;
        }

        /// <summary>Sets the rotor turning to the angle, and starts watching whether it is held.</summary>
        static void Turned(IMyMotorStator rotor, Turn turn, ReaderMemory seen)
        {
            if (!turn.Given)
                return;

            if (Already(rotor, turn.Angle))
                Hold(rotor, turn.Way, turn.Angle, turn.Speed);
            else
                rotor.RotateToAngle(turn.Way, turn.Angle, turn.Speed);

            RotorMotion.Began(rotor, seen);
        }

        /// <summary>Whether the rotor already stands at the angle, by the game's own test.</summary>
        /// <remarks>The game does nothing when told to turn a rotor to where it already is.</remarks>
        static bool Already(IMyMotorStator rotor, float degrees)
        {
            return Math.Abs(degrees * (float)Math.PI / 180f - rotor.Angle) <= float.Epsilon;
        }

        /// <summary>Leaves the rotor as a finished turn to where it stands would: the limit at the angle, the other taken away, and the speed pushing against it.</summary>
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

        /// <summary>Stops the rotor where it stands.</summary>
        static void Stop(IMyMotorStator rotor)
        {
            rotor.TargetVelocityRPM = 0f;
        }

        /// <summary>Why the rotor is not getting to its angle, or nothing while it still might.</summary>
        static BlockTrouble Why(IMyMotorStator rotor, Turn turn, ReaderMemory seen)
        {
            if (AtLimit(rotor))
                return BlockTrouble.None;

            if (!rotor.IsWorking)
                return BlockTrouble.Waiting(Cause.RotorNoPower);

            if (!rotor.IsAttached)
                return BlockTrouble.Waiting(Cause.RotorNoHead);

            if (rotor.RotorLock)
                return BlockTrouble.Waiting(Cause.RotorLocked);

            if (rotor.Torque <= 0f)
                return BlockTrouble.Waiting(Cause.RotorNoTorque);

            if (rotor.TargetVelocityRPM == 0f)
                return BlockTrouble.Fatal(Cause.RotorStopped);

            // Waited through rather than given up on: measured in game, taking its speed away does not settle what the
            // pushing does to a construct.
            return RotorMotion.Turning(rotor, seen)
                ? BlockTrouble.None
                : BlockTrouble.Waiting(Cause.RotorHeld);
        }

        /// <summary>The ticks the turn should take: the way round it goes, at the speed it was given.</summary>
        /// <remarks>A revolution a minute is six degrees a second, and the game runs sixty ticks to the second.</remarks>
        static int Expected(IMyMotorStator rotor, Turn turn)
        {
            if (!turn.Given || turn.Speed <= 0)
                return GridEntity.Unestimated;

            var from = Degrees(rotor.Angle);
            var to = Wrapped(turn.Angle);
            var clockwise = Wrapped(to - from);
            var counter = Wrapped(from - to);

            float degrees;
            if (turn.Way == MyRotationDirection.CW)
                degrees = clockwise;
            else if (turn.Way == MyRotationDirection.CCW)
                degrees = counter;
            else
                degrees = Math.Min(clockwise, counter);

            return (int)Math.Ceiling(degrees / (turn.Speed / 10f));
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

        /// <summary>A rotor being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyMotorStator _rotor;
            readonly Action _told;

            public Listening(IMyMotorStator rotor, Action told)
            {
                _rotor = rotor;
                _told = told;
                _rotor.OnAttachedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _rotor.OnAttachedChanged -= Changed;
            }

            void Changed(IMyMechanicalConnectionBlock block)
            {
                _told();
            }
        }

        /// <summary>Where a step told the rotor to turn to, which way, and how fast.</summary>
        sealed class Turn
        {
            /// <summary>Whether the step gave all of it; a step that did not turns nothing.</summary>
            public bool Given;

            public MyRotationDirection Way;

            public float Angle;

            public float Speed;

            /// <summary>The direction, angle and speed a step turning a rotor was given, or the angle and speed for a hinge, which goes the short way.</summary>
            public static Turn Read(IList<Literal> arguments, bool hinge)
            {
                var turn = new Turn { Way = MyRotationDirection.AUTO };

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
}
