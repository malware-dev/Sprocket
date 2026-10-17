using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// Which of the picked blocks' screens, or other things of a type the host provides, a member acts on: a drop box of the
    /// names they share, and a line saying what the chosen one is doing now.
    /// </summary>
    /// <remarks>
    /// The drop box is made again only when the names change.
    /// </remarks>
    public class FacetChoice : Reveal
    {
        const float Indent = 28f;
        const float Frames = 12f;

        readonly VStack _body;
        readonly Label _says;
        readonly List<string> _names = new List<string>();

        ListField _box;
        float _progress;

        /// <summary>No names yet.</summary>
        public FacetChoice()
        {
            var line = new HStack();
            line.Children.Add(new Layers { Placement = FixedSize.Size(Indent, 1f) });
            line.Children.Add(new VStack { Spacing = 8f }.As(out _body));
            Content = line;
            _body.Children.Add(new Label { Style = Styles.ValueLabel }.As(out _says));
        }

        /// <summary>Raised with the name somebody chose in the drop box.</summary>
        public event Action<string> Chosen;

        /// <summary>The name chosen.</summary>
        public string Name => _box == null ? "" : _box.Text;

        /// <summary>The names to choose from, in order.</summary>
        public IList<string> Names => _names;

        /// <summary>The line under the drop box, saying what the chosen one is doing now.</summary>
        public string Line => _says.Text;

        /// <summary>Offers <paramref name="names" /> with <paramref name="chosen" /> chosen, the line under them saying <paramref name="line" />.</summary>
        public void Show(IList<string> names, string chosen, string line)
        {
            if (!Same(names))
            {
                _names.Clear();
                _names.AddRange(names);
                if (_box != null)
                    _body.Children.Remove(_box);

                _box = new ListField(names);
                _box.Edited += () => Choose(_box.Text);
                _body.Children.Insert(0, _box);
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

        /// <summary>Opens a step further, until it is all in view.</summary>
        /// <remarks>Called every tick the picker is shown; one comparison once open, as a reveal changes nothing it already holds.</remarks>
        public void Slide()
        {
            if (_progress >= 1f)
                return;

            _progress = Math.Min(1f, _progress + 1f / Frames);
            var left = 1f - _progress;
            Openness = 1f - left * left * left;
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
