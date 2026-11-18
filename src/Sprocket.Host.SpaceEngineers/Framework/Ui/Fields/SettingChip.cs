using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>A setting standing where a value's field would be: its mark, its name, and what it is where an installation chooses nothing.</summary>
    public partial class SettingChip : HStack
    {
        /// <summary>A chip naming no setting yet.</summary>
        public SettingChip()
        {
            CreateView();
        }

        /// <summary>The setting's name.</summary>
        public string Setting
        {
            get { return _name.Text; }
            set { _name.Text = value ?? ""; }
        }

        /// <summary>What the setting is where an installation chooses nothing, as a person reads it; empty where it has no default.</summary>
        public string Default
        {
            get { return _default.Text; }
            set
            {
                _default.Text = value ?? "";
                _default.IsVisible = _default.Text.Length > 0;
            }
        }
    }
}
