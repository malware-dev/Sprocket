using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.ModSupport
{
    /// <summary>One option an action or function takes, such as a height, which a player fills in on the step; made by <see cref="Option" />.</summary>
    /// <typeparam name="T">What the mod's callback is given for it.</typeparam>
    public sealed class SprocketOption<T> : ISprocketOption
    {
        readonly Dictionary<string, object> _said = new Dictionary<string, object>();
        readonly Func<object, T> _read;

        /// <summary>An option of the type word <paramref name="type" />, called <paramref name="name" />, its value given to the mod as <paramref name="read" /> makes it.</summary>
        public SprocketOption(string type, Func<string> name, Func<object, T> read)
        {
            _said[SprocketWire.Type] = type;
            _said[SprocketWire.Named] = name;
            _read = read;
        }

        /// <summary>Gives the option its <paramref name="unit" /> as players read it, such as "m".</summary>
        public SprocketOption<T> Unit(string unit)
        {
            if (_said.ContainsKey(SprocketWire.Percent))
                throw new InvalidOperationException("An option is either a percentage or has a unit, not both.");

            _said[SprocketWire.Unit] = unit;
            return this;
        }

        /// <summary>Has players give the option as 0 to 100 %, while the mod is given a share from 0 to 1.</summary>
        public SprocketOption<T> Percent()
        {
            if (_said.ContainsKey(SprocketWire.Unit))
                throw new InvalidOperationException("An option is either a percentage or has a unit, not both.");

            _said[SprocketWire.Percent] = true;
            return this;
        }

        /// <summary>The least and most the option may be, the same on every block.</summary>
        public SprocketOption<T> Range(double least, double most)
        {
            return Range(block => least, block => most);
        }

        /// <summary>The least and most the option may be on each block.</summary>
        /// <remarks>Asked as the editor shows the field and as a step is checked, never on a tick; the mod still clamps what it is given.</remarks>
        public SprocketOption<T> Range(Func<IMyTerminalBlock, double> least, Func<IMyTerminalBlock, double> most)
        {
            _said[SprocketWire.Least] = least;
            _said[SprocketWire.Most] = most;
            return this;
        }

        /// <inheritdoc />
        public Dictionary<string, object> Wire()
        {
            return _said;
        }

        /// <summary>The value Sprocket gives for the option, as the mod's callback takes it.</summary>
        public T Read(object given)
        {
            return _read(given);
        }

        /// <summary>Says the option is a word of <paramref name="type" />.</summary>
        public SprocketOption<T> Choosing(SprocketType type)
        {
            _said[SprocketWire.Of] = type.FullName;
            return this;
        }
    }
}
