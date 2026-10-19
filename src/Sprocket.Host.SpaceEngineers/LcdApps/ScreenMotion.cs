using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Runtime;

namespace Sprocket.Host.SpaceEngineers.LcdApps
{
    /// <summary>The screens that move, drawn on every other tick by a <see cref="SurfaceLoop" /> that is on the rota only while one does.</summary>
    public sealed class ScreenMotion
    {
        readonly Rota _rota;
        readonly SurfaceLoop _loop = new SurfaceLoop();
        readonly Shift _shift;
        bool _looping;

        /// <summary>Motion drawn on <paramref name="rota" />.</summary>
        public ScreenMotion(Rota rota)
        {
            _rota = rota;
            _shift = new Shift(rota.Join(), Move);
        }

        /// <summary>Draws <paramref name="surface" /> every other tick while something on it moves.</summary>
        public void Animate(ILiveSurface surface)
        {
            _loop.Add(surface);
            if (_looping)
                return;

            _looping = true;
            _rota.Every(_shift, Rota.Each);
        }

        /// <summary>Stops drawing <paramref name="surface" /> however much moves on it.</summary>
        public void Still(ILiveSurface surface)
        {
            _loop.Remove(surface);
        }

        /// <summary>Draws every screen that moves, and leaves the rota once none does.</summary>
        void Move(int tick)
        {
            _loop.Run(tick);
            if (_loop.Busy)
                return;

            _looping = false;
            _rota.Leave(_shift);
        }
    }
}
