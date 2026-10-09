using Sandbox.ModAPI;
using VRage.Game.Components;

namespace RichTextPanels
{
    /// <summary>The mod's place in a world: shares the panel settings for as long as the world is loaded, and offers them in the terminal.</summary>
    /// <remarks>
    ///     The game creates session components itself, by the descriptor, so this is the one static-free way in. Every
    ///     machine shares the settings; only a player's game has a terminal, so a dedicated server offers nothing.
    /// </remarks>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class RichTextSession : MySessionComponentBase
    {
        Sharing _sharing;
        PanelControls _controls;

        /// <inheritdoc />
        public override void BeforeStart()
        {
            _sharing = new Sharing();
            _sharing.Attach();

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            _controls = new PanelControls(_sharing);
            _controls.Attach();
        }

        /// <inheritdoc />
        protected override void UnloadData()
        {
            if (_controls != null)
                _controls.Detach();

            if (_sharing != null)
                _sharing.Detach();

            _controls = null;
            _sharing = null;
        }
    }
}
