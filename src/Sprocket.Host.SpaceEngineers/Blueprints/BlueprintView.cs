using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Blueprints
{
    /// <summary>A blueprint's logic systems on the left, and the chosen one's parts on the right.</summary>
    public sealed class BlueprintView : ComponentView
    {
        const float MadeWidth = 380f;

        readonly BlueprintComponent _component;
        readonly Bar _root = new Bar();
        readonly Stack _made = Widgets.Column(Space.Row);
        readonly Stack _right = Widgets.Column(Space.Row);
        readonly Stack _parts = Widgets.Column(0f);
        readonly Label _madeTitle = Widgets.Label("", Palette.Faint, Lettering.Small);
        readonly Label _partsTitle = Widgets.Label("", Palette.Faint, Lettering.Small);
        readonly EntryRow _chosen;
        readonly List<EntryRow> _members = new List<EntryRow>();
        readonly List<PartRow> _partRows = new List<PartRow>();

        int _membersRevision = -1;
        int _partsRevision = -1;

        string _saidMade;
        string _saidParts;

        public BlueprintView(BlueprintComponent component)
        {
            _component = component;
            _chosen = new EntryRow(this);

            _madeTitle.Wraps = false;
            _madeTitle.Padding = new Thickness(Space.Line, Space.Item, 0f, 0f);
            _partsTitle.Wraps = false;
            _partsTitle.Padding = new Thickness(Space.Line, Space.Region, 0f, 0f);

            var made = new Sized(MadeWidth);
            made.Content = _made;

            _right.Children.Add(_chosen.Row);
            _right.Children.Add(Widgets.Line());
            _right.Children.Add(_partsTitle);
            _right.Children.Add(_parts);

            _root.Left = made;
            _root.Fill = _right;
            _root.Spacing = Space.Region * 2f;
            _root.Tops = true;
        }

        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _root; }
        }

        protected override void Refresh()
        {
            // Capitals, as every section of a screen is named: this is the screen naming a part of itself.
            if (!ReferenceEquals(_saidMade, _component.MadeTitle) || !ReferenceEquals(_saidParts, _component.PartsTitle))
            {
                _saidMade = _component.MadeTitle;
                _saidParts = _component.PartsTitle;
                _madeTitle.Text = Widgets.Caps(_saidMade);
                _partsTitle.Text = Widgets.Caps(_saidParts);
            }

            if (_membersRevision != _component.Members.Revision)
            {
                _membersRevision = _component.Members.Revision;
                Members();
            }

            if (_partsRevision != _component.Parts.Revision)
            {
                _partsRevision = _component.Parts.Revision;
                Parts();
            }

            for (var i = 0; i < _members.Count && i < _component.Members.Count; i++)
                _members[i].Update(_component.Members[i]);

            _chosen.Row.Collapsed = _component.Chosen == null;
            if (_component.Chosen != null)
                _chosen.Update(_component.Chosen);

            for (var i = 0; i < _partRows.Count && i < _component.Parts.Count; i++)
                _partRows[i].Update(Host.World, _component.Parts[i], _component.RenamePrompt);
        }

        void Members()
        {
            _members.Clear();
            _made.Children.Clear();
            _made.Children.Add(_madeTitle);

            for (var i = 0; i < _component.Members.Count; i++)
            {
                var row = new EntryRow(this);
                _members.Add(row);
                _made.Children.Add(row.Row);
            }
        }

        void Parts()
        {
            _partRows.Clear();
            _parts.Children.Clear();

            for (var i = 0; i < _component.Parts.Count; i++)
            {
                var row = new PartRow();
                _partRows.Add(row);
                _parts.Children.Add(row.Root);
            }
        }
    }
}
