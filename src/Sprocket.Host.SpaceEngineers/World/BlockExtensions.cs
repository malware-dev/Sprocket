using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Sprocket knows about particular kinds of block, one of each for a world.
    /// </summary>
    /// <remarks>
    /// One set per world rather than one for the game, because an extension watching a block over time
    /// remembers what it saw - a vent moving gas, a rotor at an angle - and that memory is about the
    /// blocks of one construct in one world. Held by the world, it goes when the world goes.
    /// </remarks>
    public sealed class BlockExtensions
    {
        readonly IBlockExtension[] _all =
        {
            new DoorExtension(),
            new AirVentExtension(),
            new ButtonPanelExtension(),
            new RotorExtension(),
            new PistonExtension(),
            new BatteryExtension(),
            new TankExtension(),
            new SoundExtension()
        };

        readonly ISignals[] _triggers =
        {
            new ButtonPanelSignals(),
            new TimerSignals(),
            new SensorSignals(),
            new EventControllerSignals(),
            new ShipControllerSignals()
        };

        /// <summary>What signals <paramref name="block" />'s toolbar slots send, or null where each sends one of its own.</summary>
        public ISignals SignalsFor(IMyTerminalBlock block)
        {
            for (var i = 0; i < _triggers.Length; i++)
            {
                if (_triggers[i].Claims(block))
                    return _triggers[i];
            }

            return null;
        }

        /// <summary>The extension that knows <paramref name="block" />, or null where none does.</summary>
        public IBlockExtension For(IMyTerminalBlock block)
        {
            for (var i = 0; i < _all.Length; i++)
            {
                if (_all[i].Claims(block))
                    return _all[i];
            }

            return null;
        }
    }
}
