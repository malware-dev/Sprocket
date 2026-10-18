using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;

namespace Sprocket.MultiRename
{
    /// <summary>The mod in one world: lets the game's own Name box give every selected block the name typed.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MultiRenameSession : MySessionComponentBase
    {
        // The id of the game's rename box. Some blocks have none, such as the compact antenna.
        const string RenameBox = "Name";

        /// <inheritdoc />
        public override void BeforeStart()
        {
            // A dedicated server has no terminal. MyAPIGateway.TerminalControls does not exist yet in LoadData.
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
                return;

            MyAPIGateway.TerminalControls.CustomControlGetter += Arrange;
        }

        /// <inheritdoc />
        protected override void UnloadData()
        {
            if (MyAPIGateway.TerminalControls != null)
                MyAPIGateway.TerminalControls.CustomControlGetter -= Arrange;
        }

        /// <summary>Lets the rename box among <paramref name="controls" /> apply to every block selected.</summary>
        /// <remarks>Called as the terminal's <c>CustomControlGetter</c>, each time a block's controls are about to show.</remarks>
        static void Arrange(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            foreach (var control in controls)
            {
                if (control.Id != RenameBox)
                    continue;

                control.SupportsMultipleBlocks = true;
                return;
            }
        }
    }
}
