using Sprocket.Host.SpaceEngineers.Framework.Text;
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;

// IMyTerminalBlock is declared in both the ModAPI and the Ingame namespace, so the Ingame one cannot be imported whole.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Wiring
{
    /// <summary>
    /// The action Sprocket puts into a block's toolbar, on that same block, so pressing the slot starts a sequence.
    /// </summary>
    /// <remarks>
    /// Offered on every block, on every machine, through the game's action getter rather than registered per type: a type
    /// copies the actions of the types it derives from once, when it is first used, so an action registered on a base later
    /// never reaches the types already loaded. A slot stores this action's id, and a machine without it draws the slot as
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

        /// <summary>What a slot of <paramref name="block" />'s toolbar holds: this action, on that block, with <paramref name="caption" /> under its icon.</summary>
        /// <remarks>A null caption leaves the action's own text under the icon.</remarks>
        public static MyToolbarItemInfo Slot(long block, string caption)
        {
            return MyToolbarItemInfo.BlockAction(block, ActionId, caption);
        }

        // The world's action, offered to every block while the world is loaded. Static because the action is one for every
        // block and the game calls the getter's handler and the action's own without a Sprocket to hand; made as a world
        // loads and let go as it unloads.
        static IMyTerminalAction _action;

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

        /// <summary>Makes the action, in <paramref name="texts" />, handing each press to <paramref name="pressed" />, and offers it to every block until <see cref="End" />.</summary>
        /// <remarks>Called as a world loads. Making it touches no type's list of actions, so it is safe before any block exists.</remarks>
        public static void Begin(Texts texts, Action<IMyTerminalBlock, string> pressed)
        {
            if (_action != null || MyAPIGateway.TerminalControls == null)
                return;

            IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(ActionId);
            action.Name = new StringBuilder(texts.TerminalAction);

            // The fallback text for a slot Sprocket has not labelled, in the language the texts are in when it is drawn.
            action.Writer = delegate(IMyTerminalBlock _, StringBuilder text) { text.Append(texts.TerminalOpen); };

            // The value fixes the parameter's type, and is what the action receives when run with none; an empty key resolves to
            // nothing.
            action.ParameterDefinitions.Add(TerminalActionParameter.Get(string.Empty));
            action.ActionWithParameters = (block, parameters) => Run(block, parameters, pressed);

            action.InvalidToolbarTypes = EveryList();

            _action = action;
            MyAPIGateway.TerminalControls.CustomActionGetter += Offer;
        }

        /// <summary>Stops offering the action, as the world unloads.</summary>
        public static void End()
        {
            if (_action == null)
                return;

            if (MyAPIGateway.TerminalControls != null)
                MyAPIGateway.TerminalControls.CustomActionGetter -= Offer;

            _action = null;
        }

        /// <summary>Adds the action to the actions the game gathered for <paramref name="block" />.</summary>
        /// <remarks>
        /// Asked whenever the game gathers a block's actions: for each toolbar slot on screen every frame, and as a slot is
        /// pressed. One add of the one action, and nothing else.
        /// </remarks>
        static void Offer(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            actions.Add(_action);
        }

        /// <summary>Hands the key a slot carries, with <paramref name="block" />, the block its action is on, to <paramref name="pressed" />.</summary>
        /// <remarks>
        /// Runs where the toolbar ran its slot: on the server for a button panel, whose press the game sends there, and on the
        /// pressing player's machine for a ship's toolbar, which the game runs there and never syncs.
        /// </remarks>
        static void Run(IMyTerminalBlock block, ListReader<TerminalActionParameter> parameters, Action<IMyTerminalBlock, string> pressed)
        {
            if (block == null || parameters.Count == 0)
                return;

            // The value's type is whatever was written, not what was declared, so a slot edited by hand may hold another type.
            var key = parameters[0].Value as string;
            if (string.IsNullOrEmpty(key))
                return;

            pressed(block, key);
        }
    }
}
