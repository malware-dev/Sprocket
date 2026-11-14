using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>The attributes written above a statement that Sprocket does not use, each kept as it was written so a save writes it back.</summary>
    public sealed class KeptAttributes
    {
        readonly List<string> _names = new List<string>();
        readonly List<string> _written = new List<string>();

        /// <summary>How many attributes are kept.</summary>
        public int Count => _names.Count;

        /// <summary>The name of the attribute at <paramref name="index" />, in the order they were written.</summary>
        public string Name(int index)
        {
            return _names[index];
        }

        /// <summary>The value of the attribute at <paramref name="index" />, exactly as it was written.</summary>
        public string Written(int index)
        {
            return _written[index];
        }

        /// <summary>Keeps the attribute <paramref name="name" />, its value written as <paramref name="written" />, after those kept already.</summary>
        public void Add(string name, string written)
        {
            _names.Add(name);
            _written.Add(written);
        }
    }
}
