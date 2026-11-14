using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// Which of the picked blocks' screens, or other things of a type the host provides, the members listed under it act on:
    /// a drop box of the names they share, and beside it what the chosen one is doing now.
    /// </summary>
    /// <remarks>
    /// The drop box is made again only when the names change.
    /// </remarks>
    public partial class FacetChoice : HStack
    {
        readonly List<string> _names = new List<string>();

        ListField _box;

        /// <summary>No names yet.</summary>
        public FacetChoice()
        {
            CreateView();
        }

        /// <summary>Raised with the name somebody chose in the drop box.</summary>
        public event Action<string> Chosen;

        /// <summary>The name chosen.</summary>
        public string Name => _box == null ? "" : _box.Text;

        /// <summary>The names to choose from, in order.</summary>
        public IList<string> Names => _names;

        /// <summary>What the chosen one is doing now, said beside the drop box.</summary>
        public string Line => _says.Text;

        /// <summary>Offers <paramref name="names" /> with <paramref name="chosen" /> chosen, saying <paramref name="line" /> beside them.</summary>
        public void Show(IList<string> names, string chosen, string line)
        {
            if (!Same(names))
            {
                _names.Clear();
                _names.AddRange(names);
                if (_box != null)
                    Children.Remove(_box);

                _box = new ListField(names);
                _box.Edited += () => Choose(_box.Text);
                Children.Insert(0, _box);
            }

            _box.Text = chosen;
            _says.Text = line;
        }

        /// <summary>Chooses <paramref name="name" />, as choosing it in the drop box does.</summary>
        public void Choose(string name)
        {
            var chosen = Chosen;
            if (chosen != null)
                chosen(name);
        }

        bool Same(IList<string> names)
        {
            if (names.Count != _names.Count)
                return false;

            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] != _names[i])
                    return false;
            }

            return true;
        }
    }
}
