using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    public sealed class HeadingView : ComponentView
    {
        readonly HeadingComponent _component;
        readonly Label _label = Widgets.Label("", Palette.Faint, Lettering.Small);
        readonly Label _name = Widgets.Label("", Palette.Ink, Lettering.Small);
        readonly Label _after = Widgets.Label("", Palette.Faint, Lettering.Small);
        readonly Stack _line = Widgets.RowOf(0f);

        /// <summary>What it was last given, so the words are made when they change.</summary>
        string _said;
        string _saidName;
        string _saidAfter;

        bool _follows;

        public HeadingView(HeadingComponent component)
        {
            _component = component;
            _label.Wraps = false;
            _name.Wraps = false;
            _after.Wraps = false;

            _line.Children.Add(_label);
            _line.Children.Add(_name);
            _line.Children.Add(_after);

            Spaced();
        }

        /// <summary>
        /// All of the room above and none below, where there is anything above to be told apart
        /// from.
        /// </summary>
        /// <remarks>
        /// That is the whole job of a section name - to read as the start of what follows rather
        /// than the end of what came before - and a heading with the same air on both sides belongs
        /// to neither. The first section on a screen takes the ordinary line of air instead: the
        /// header's own rule is already the thing it is being told apart from.
        /// </remarks>
        void Spaced()
        {
            _follows = _component.Follows;
            _line.Padding = new Thickness(Space.Line, _follows ? Space.Region * 2f : Space.Item,
                0f, 0f);
        }

        /// <summary>Told rather than asked: the one thing it shows announces itself.</summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _line; }
        }

        protected override void Refresh()
        {
            if (_follows != _component.Follows)
                Spaced();

            if (ReferenceEquals(_said, _component.Text) && ReferenceEquals(_saidName, _component.Name)
                && ReferenceEquals(_saidAfter, _component.After))
                return;

            _said = _component.Text;
            _saidName = _component.Name;
            _saidAfter = _component.After;

            // Capitals, like a picker's own title: this is the screen naming a part of itself rather
            // than repeating a word somebody typed, and it has to read as a label rather than as one
            // more line of the list under it. A heading that holds a name is written as a sentence
            // instead, all of it: the name cannot be shouted, and half a line in capitals reads as two.
            var named = _component.Name.Length > 0;
            _label.Text = named ? _component.Text + " " : Widgets.Caps(_component.Text);
            _name.Text = _component.Name;
            _name.Collapsed = !named;
            _after.Text = _component.After.Length > 0 ? " " + _component.After : "";
            _after.Collapsed = _component.After.Length == 0;
        }
    }
}
