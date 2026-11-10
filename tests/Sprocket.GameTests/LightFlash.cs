using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Sandbox.ModAPI;

namespace Sprocket.GameTests
{
    /// <summary>A light watched for coming on and going off again, as a program flashes it, measured in game time and in real time.</summary>
    public sealed class LightFlash
    {
        static readonly TimeSpan Patience = TimeSpan.FromSeconds(8);

        readonly IMyFunctionalBlock _light;
        TimeSpan _cameOn = TimeSpan.MinValue;
        TimeSpan _onFor = TimeSpan.MinValue;
        double _realOnFor = -1;
        int _framesOn = -1;
        int _gameFramesOn = -1;
        float _simulationRatio = -1;

        /// <summary>A watch on <paramref name="light" />, which is off as it starts.</summary>
        public LightFlash(IMyFunctionalBlock light)
        {
            _light = light;
        }

        /// <summary>Waits a frame at a time for the light to come on and then go off, each for at most eight seconds of game time.</summary>
        public IEnumerable<int> Watch()
        {
            var start = MySession.Static.ElapsedGameTime;
            while (!_light.Enabled)
            {
                if (MySession.Static.ElapsedGameTime - start > Patience)
                    yield break;

                yield return 1;
            }

            var on = MySession.Static.ElapsedGameTime;
            _cameOn = on - start;
            var real = Stopwatch.StartNew();
            var gameFrames = MySession.Static.GameplayFrameCounter;
            var frames = 0;
            while (_light.Enabled)
            {
                if (MySession.Static.ElapsedGameTime - on > Patience)
                    yield break;

                frames++;
                yield return 1;
            }

            _onFor = MySession.Static.ElapsedGameTime - on;
            _realOnFor = real.Elapsed.TotalSeconds;
            _framesOn = frames;
            _gameFramesOn = MySession.Static.GameplayFrameCounter - gameFrames;
            _simulationRatio = MyPhysics.SimulationRatio;
        }

        /// <summary>Reports the flash under <paramref name="check" />: it passes where the light came on and stayed on about 3 seconds of game time.</summary>
        public void Report(GameTestReport report, string check)
        {
            if (_cameOn == TimeSpan.MinValue)
            {
                report.Fail(check, "the light never came on");
                return;
            }

            if (_onFor == TimeSpan.MinValue)
            {
                report.Fail(check, $"the light came on after {_cameOn.TotalSeconds:0.00} s and never went off");
                return;
            }

            var seconds = _onFor.TotalSeconds;
            report.Check(check, seconds > 2.5 && seconds < 3.5,
                $"on after {_cameOn.TotalSeconds:0.00} s, for {seconds:0.00} s of game time, {_realOnFor:0.00} s of real time, {_framesOn} plugin updates, {_gameFramesOn} gameplay frames, simulation ratio {_simulationRatio:0.00}");
        }
    }
}
