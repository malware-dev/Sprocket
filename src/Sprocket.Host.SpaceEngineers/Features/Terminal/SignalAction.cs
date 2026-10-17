using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

// IMyTerminalBlock is declared in both the ModAPI and the Ingame namespace, so the Ingame one cannot be imported whole.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Features.Terminal
{
    /// <summary>
    /// The action Sprocket puts into somebody else's block so that block can start a sequence.
    /// </summary>
    /// <remarks>
    /// Registered on every machine: a slot stores this action's id, and a machine without the action draws the slot as
    /// unassigned.
    /// </remarks>
    public static class SignalAction
    {
        /// <summary>The action's id.</summary>
        /// <remarks>Persisted in every slot that holds the action, in saved games.</remarks>
        public const string ActionId = "Sprocket_Trigger";

        /// <summary>The toolbar types the action is hidden from: every list a player can assign from.</summary>
        /// <remarks>
        /// Hidden through <c>InvalidToolbarTypes</c> and never through <c>Enabled</c>: <c>GetActions</c> checks <c>IsEnabled</c>
        /// first, so a false <c>Enabled</c> stops the slot firing. The property starts null and is to be assigned, since
        /// <c>Add</c> on it is a null reference at registration, as THDigi's terminal control example documents.
        /// </remarks>
        static List<MyToolbarType> EveryList()
        {
            // Button panels included: in a local game a slot Sprocket wrote still shows and fires; untested on a dedicated server.
            return new List<MyToolbarType>
            {
                MyToolbarType.Character, MyToolbarType.Ship,
                MyToolbarType.Seat, MyToolbarType.BuildCockpit, MyToolbarType.Spectator, MyToolbarType.ButtonPanel
            };
        }

        // The game keeps custom terminal actions for the life of the process, across world loads, so the action is
        // registered once per process; an instance would register it again with every load.
        static bool _registered;

        /// <summary>What a slot points at: this block, and this trigger of it.</summary>
        public static MyToolbarItemInfo Slot(long sprocketBlockId)
        {
            return MyToolbarItemInfo.BlockAction(sprocketBlockId, ActionId);
        }

        /// <summary>Whether the toolbar item <paramref name="info" /> is this action.</summary>
        public static bool IsOurs(MyToolbarItemInfo info)
        {
            return info.ActionId == ActionId;
        }

        /// <summary>The argument that says which sequence of which system, ready to write into a slot.</summary>
        /// <remarks>
        /// One opaque string: every slot in every saved world freezes the parameter list's arity and types, so the key can only
        /// grow inside the string.
        /// </remarks>
        public static List<TerminalActionParameter> Argument(string key)
        {
            var arguments = new List<TerminalActionParameter>();
            arguments.Add(TerminalActionParameter.Get(key));
            return arguments;
        }

        /// <summary>Makes the action once, in <paramref name="texts" />, which the game keeps for the session.</summary>
        public static void Register(Texts texts)
        {
            if (_registered)
                return;

            _registered = true;

            IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyProgrammableBlock>(ActionId);
            action.Name = new StringBuilder(texts.TerminalAction);

            // The fallback text for a slot Sprocket has not labelled.
            var slot = texts.TerminalOpen;
            action.Writer = delegate(IMyTerminalBlock _, StringBuilder text) { text.Append(slot); };

            // The value fixes the parameter's type, and is what the action receives when run with none; an empty key resolves to
            // nothing.
            action.ParameterDefinitions.Add(TerminalActionParameter.Get(string.Empty));
            action.ActionWithParameters = Run;

            action.InvalidToolbarTypes = EveryList();

            MyAPIGateway.TerminalControls.AddAction<IMyProgrammableBlock>(action);
        }

        /// <summary>Fires the key a slot carries on the Sprocket unit of <paramref name="block" />.</summary>
        /// <remarks>Runs on the server: the press is a server event and <c>Apply</c> never syncs.</remarks>
        static void Run(IMyTerminalBlock block, ListReader<TerminalActionParameter> parameters)
        {
            if (block == null || parameters.Count == 0)
                return;

            // The value's type is whatever was written, not what was declared, so a slot edited by hand may hold another type.
            var key = parameters[0].Value as string;
            if (string.IsNullOrEmpty(key))
                return;

            SprocketUnit unit = SprocketSession.UnitOf(block);
            if (unit != null)
                unit.Fire(key);
        }
    }
}
