using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "Braking torque on a working rotor", run in the real game on a rotor and a battery built by the test: how far
    /// the rotor's head turns in two seconds of a steady push, with no torque of its own, braking torque none or the most, and the
    /// rotor switched on or off.
    /// </summary>
    /// <remarks>The world has no gravity to swing a load, so the test pushes the head about the rotor's axis itself.</remarks>
    public sealed class RotorBrakingGameTest
    {
        const float Most = 1e9f;

        readonly GameTestReport _report;
        readonly Dictionary<string, double> _turned = new Dictionary<string, double>();
        float _mass;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public RotorBrakingGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var grid = new BuiltGrid(new Vector3D(0, 0, 440), BuiltGrid.Battery(Vector3I.Zero),
                BuiltGrid.Block<MyObjectBuilder_MotorStator>("LargeStator", new Vector3I(1, 0, 0)));
            var rotor = grid.Find<IMyMotorStator>();
            if (rotor == null)
            {
                _report.Fail("rotor", "the rotor was not built");
                yield break;
            }

            yield return 3 * PluginUpdates.PerSecond;
            // The large head of a large rotor, by the game's own action.
            rotor.GetActionWithName("AddRotorTopPart").Apply(rotor);
            yield return 3 * PluginUpdates.PerSecond;
            if (rotor.TopGrid == null)
            {
                _report.Fail("rotor head", "AddRotorTopPart gave the rotor no head");
                yield break;
            }

            rotor.TargetVelocityRPM = 0;
            rotor.Torque = 0;
            foreach (var on in new[] { false, true })
            {
                foreach (var braking in new[] { 0f, Most })
                {
                    var spin = Spin(rotor, on, braking);
                    while (spin.MoveNext())
                        yield return spin.Current;
                }
            }

            _report.Check("braking torque on a switched off rotor", _turned["off, braking 0"] > 2 * _turned["off, braking most"],
                $"turned {_turned["off, braking 0"]:0.###} rad with no braking and {_turned["off, braking most"]:0.###} rad at the most");
            var free = _turned["on, braking 0"];
            _report.Check("braking torque on a switched on rotor", free > 0 && System.Math.Abs(_turned["on, braking most"] - free) < free / 10,
                $"with no torque of its own and a {_mass:0} kg head, it turned {_turned["on, braking 0"]:0.###} rad with no braking and {_turned["on, braking most"]:0.###} rad at the most");
        }

        /// <summary>Sets the head spinning with the rotor <paramref name="on" /> and <paramref name="braking" />, and keeps how far it turns.</summary>
        IEnumerator<int> Spin(IMyMotorStator rotor, bool on, float braking)
        {
            rotor.Enabled = on;
            rotor.BrakingTorque = braking;
            yield return PluginUpdates.PerSecond;

            // A steady push about the rotor's axis on every update, an angular impulse sized to the head's mass.
            var head = rotor.TopGrid.Physics;
            _mass = head.Mass;
            var push = (Vector3)(rotor.WorldMatrix.Up * head.Mass * 0.5);
            var before = rotor.Angle;
            var turned = 0.0;
            for (var i = 0; i < 2 * PluginUpdates.PerSecond; i++)
            {
                head.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, null, null, push);
                yield return 0;
                var now = rotor.Angle;
                var step = now - before;
                if (step > MathHelper.Pi)
                    step -= MathHelper.TwoPi;
                if (step < -MathHelper.Pi)
                    step += MathHelper.TwoPi;
                turned += System.Math.Abs(step);
                before = now;
            }

            _turned[$"{(on ? "on" : "off")}, braking {(braking > 0 ? "most" : "0")}"] = turned;

            // Stopped before the next case.
            head.AngularVelocity = Vector3.Zero;
        }
    }
}
