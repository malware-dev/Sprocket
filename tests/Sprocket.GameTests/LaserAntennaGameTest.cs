using System.Collections.Generic;
using System.Globalization;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRageMath;
using MyLaserAntennaStatus = Sandbox.ModAPI.Ingame.MyLaserAntennaStatus;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "An idle laser antenna and out of range", run in the real game on a laser antenna and a battery built by the
    /// test: the link the game reports while the antenna is left idle, and while idle once its grid is turned upside down from the
    /// last direction it was given. Sprocket's starts connecting is the link leaving idle, so the trigger stays quiet only
    /// while the game reports idle throughout.
    /// </summary>
    public sealed class LaserAntennaGameTest
    {
        static readonly Vector3D At = new Vector3D(0, 0, 500);

        readonly GameTestReport _report;
        readonly HashSet<MyLaserAntennaStatus> _seen = new HashSet<MyLaserAntennaStatus>();
        bool _outside;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public LaserAntennaGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var grid = new BuiltGrid(At, BuiltGrid.Battery(Vector3I.Zero),
                BuiltGrid.Block<MyObjectBuilder_LaserAntenna>("LargeBlockLaserAntenna", new Vector3I(2, 0, 0)));
            var antenna = grid.Find<IMyLaserAntenna>();
            if (antenna == null)
            {
                _report.Fail("laser antenna", "the laser antenna was not built");
                yield break;
            }

            yield return 3 * PluginUpdates.PerSecond;

            var left = Watch(antenna, 10);
            while (left.MoveNext())
                yield return left.Current;

            _report.Check("an idle laser antenna left alone", Only(MyLaserAntennaStatus.Idle),
                $"over 10 s the game reports {Said()}, outside its limits: {_outside}");

            // Given a direction straight up, 2 km away, where no antenna answers, then told to go idle.
            var up = antenna.WorldMatrix.Up;
            var target = antenna.GetPosition() + up * 2000;
            antenna.SetTargetCoords(string.Format(CultureInfo.InvariantCulture, "GPS:Target:{0}:{1}:{2}:", target.X, target.Y, target.Z));
            antenna.Connect();
            yield return 5 * PluginUpdates.PerSecond;
            var aimed = antenna.Status;
            antenna.GetActionWithName("Idle").Apply(antenna);
            yield return PluginUpdates.PerSecond;
            var idled = antenna.Status;
            _report.Note($"aimed straight up the game reports {aimed}, and told to go idle, {idled}");

            // Turned upside down, the last direction is straight down through its base. A stock laser antenna turns -180 to 180 degrees
            // in elevation and azimuth (CubeBlocks_Communications.sbc), so no direction is outside its limits.
            var cube = antenna.CubeGrid;
            var turned = cube.WorldMatrix;
            turned = MatrixD.CreateRotationX(System.Math.PI) * MatrixD.CreateTranslation(turned.Translation);
            cube.WorldMatrix = turned;
            var flipped = Watch(antenna, 10);
            while (flipped.MoveNext())
                yield return flipped.Current;

            var facing = Vector3D.Dot(antenna.WorldMatrix.Up, up);
            _report.Check("an idle laser antenna turned upside down", facing < -0.99 && idled == MyLaserAntennaStatus.Idle && Only(MyLaserAntennaStatus.Idle),
                $"turned until its up is {facing:0.##} of where it was, over 10 s the game reports {Said()}, outside its limits: {_outside}");

            // The same direction asked for upside down, which it turns to.
            antenna.Connect();
            var asked = Watch(antenna, 3);
            while (asked.MoveNext())
                yield return asked.Current;

            _report.Note($"told to connect upside down, over 3 s the game reports {Said()}, outside its limits: {_outside}");
            antenna.GetActionWithName("Idle").Apply(antenna);
        }

        /// <summary>Notes the link <paramref name="antenna" /> reports every sixth of a second for <paramref name="seconds" /> seconds, from nothing.</summary>
        IEnumerator<int> Watch(IMyLaserAntenna antenna, int seconds)
        {
            _seen.Clear();
            _outside = false;
            for (var sixth = 0; sixth < seconds * 6; sixth++)
            {
                _seen.Add(antenna.Status);
                _outside |= antenna.IsOutsideLimits;
                yield return PluginUpdates.PerSecond / 6;
            }
        }

        bool Only(MyLaserAntennaStatus status) => _seen.Count == 1 && _seen.Contains(status);

        string Said() => string.Join(", ", _seen);
    }
}
