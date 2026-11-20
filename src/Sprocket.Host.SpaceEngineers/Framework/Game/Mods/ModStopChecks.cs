using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>
    /// Looks, once, a second after Sprocket stops a mod's task, whether the mod has said the task stopped, and logs it as
    /// breaking the mod support guidelines where it has not.
    /// </summary>
    /// <remarks>One shift on the world's rota for every stopped task, due one second after its stop; nothing runs while none is due.</remarks>
    public sealed class ModStopChecks
    {
        /// <summary>How long a mod's task may take to say it has stopped, in ticks: one second.</summary>
        public const int SlowStop = Duration.PerSecond;

        readonly Rota _rota;
        readonly Shift _shift;
        readonly List<Check> _due = new List<Check>();

        /// <summary>The checks, made on <paramref name="rota" />.</summary>
        public ModStopChecks(Rota rota)
        {
            _rota = rota;
            _shift = new Shift(rota.Join(), Due);
        }

        /// <summary>How many checks are still due, which a test counts.</summary>
        public int Waiting => _due.Count;

        /// <summary>Looks a second from now whether <paramref name="stopped" /> says the task <paramref name="what" /> names has stopped.</summary>
        public void Watch(Func<bool> stopped, string what)
        {
            _due.Add(new Check { Due = _rota.Now + SlowStop, Stopped = stopped, What = what });
            if (_due.Count == 1)
                _rota.At(_shift, _due[0].Due);
        }

        /// <summary>Logs each task due by <paramref name="tick" /> that has not said it stopped, and waits for the next.</summary>
        void Due(int tick)
        {
            while (_due.Count > 0 && _due[0].Due <= tick)
            {
                var check = _due[0];
                _due.RemoveAt(0);
                if (!check.Stopped())
                    GameLog.Say($"Guideline breach: {check.What} took more than {SlowStop / Duration.PerSecond} s to stop. A mod's task must stop as fast as it can.");
            }

            if (_due.Count > 0)
                _rota.At(_shift, _due[0].Due);
        }

        /// <summary>One stopped task to look at.</summary>
        sealed class Check
        {
            public int Due;
            public Func<bool> Stopped;
            public string What;
        }
    }
}
