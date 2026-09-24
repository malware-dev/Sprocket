using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Controls;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    public sealed class HeadingView : ComponentView
    {
        readonly HeadingComponent _component;
        readonly Label _label = Widgets.Label("", Palette.Faint, Lettering.Small);

        /// <summary>What it was last given, so the capitals are made when the word changes.</summary>
        string _said;

        bool _follows;

        public HeadingView(HeadingComponent component)
        {
            _component = component;
            _label.Wraps = false;

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
            _label.Padding = new Thickness(Space.Line, _follows ? Space.Region * 2f : Space.Item,
                0f, 0f);
        }

        /// <summary>Told rather than asked: the one thing it shows announces itself.</summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _label; }
        }

        protected override void Refresh()
        {
            if (_follows != _component.Follows)
                Spaced();

            if (ReferenceEquals(_said, _component.Text))
                return;

            // Capitals, like a picker's own title: this is the screen naming a part of itself rather
            // than repeating a word somebody typed, and it has to read as a label rather than as one
            // more line of the list under it.
            _said = _component.Text;
            _label.Text = Widgets.Caps(_component.Text);
        }
    }
}
