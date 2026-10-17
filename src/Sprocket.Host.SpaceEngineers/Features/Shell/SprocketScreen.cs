using System;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Screens;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRageMath;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using VRage.Game.GUI.TextPanel;
using Sprocket.Host.SpaceEngineers.Features.Look;
using IngameSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IngameSurfaces = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>Sprocket's editor, as a screen the game owns.</summary>
    public sealed class SprocketScreen : GameScreen
    {
        readonly SprocketUnit _unit;
        readonly IMyTerminalBlock _opened;
        readonly Action _closed;

        /// <summary>The editor on <paramref name="unit" />, opened from <paramref name="opened" />, telling <paramref name="closed" /> each time it closes.</summary>
        public SprocketScreen(SprocketUnit unit, IMyTerminalBlock opened, Action closed)
            : this(unit, opened, closed, new GameScreenSpace())
        {
        }

        SprocketScreen(SprocketUnit unit, IMyTerminalBlock opened, Action closed, IScreenSpace screen)
            : base("Sprocket", new Vector2(EditorView.ScreenWidth, EditorView.ScreenHeight), new EditorView(unit.Editor), screen,
                Materials.Fill, new MarkImages(new ScreenSpaceImages(screen)))
        {
            _unit = unit;
            _opened = opened;
            _closed = closed;
            unit.Editor.Input.Clipboard = new ClipboardFile(new GlobalFiles(), GameLog.Say, unit.Editor.Pasted);
        }

        /// <inheritdoc />
        protected override void OnOpened()
        {
            base.OnOpened();

            _unit.Showing = true;
            Hud(false);
            _unit.Editor.Opened();

            // Toolbars are read only when asked, and a slot may have been re-pointed since this screen was last open.
            _unit.World.LookAgain();
        }

        /// <summary>Hides the game's HUD while this is open, and gives it back as it closes.</summary>
        /// <remarks>Leaves the player's own HUD setting alone. Player 0 is this machine's own player, so no server is asked.</remarks>
        static void Hud(bool shown)
        {
            MyVisualScriptLogicProvider.ShowHud(shown, 0);
        }

        /// <summary>Writes the program back to the block on the way out.</summary>
        protected override void OnClosed(bool isUnloading)
        {
            _unit.Showing = false;
            Hud(true);

            _unit.Persist();

            if (!isUnloading)
                Offer();

            if (_closed != null)
                _closed();
        }

        /// <summary>Puts the status app on the screen of the programmable block the editor was opened from, where the screen is showing nothing.</summary>
        void Offer()
        {
            if (_unit.Editor.Program.Systems.Count == 0)
                return;

            var provider = _opened as IngameSurfaces;
            if (provider == null)
                return;

            if (provider.SurfaceCount == 0)
                return;

            IngameSurface screen = provider.GetSurface(0);
            if (screen == null || screen.ContentType != ContentType.NONE)
                return;

            screen.ContentType = ContentType.SCRIPT;
            screen.Script = StatusApp.Id;
        }
    }
}
