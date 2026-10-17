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
    /// <summary>
    /// Sprocket's editor, as a screen the game owns.
    /// </summary>
    /// <remarks>
    /// Three things meet here and none of them knows about the other two: core decides what is on screen,
    /// <see cref="EditorView" /> decides how this target draws it, and the game supplies a world and a
    /// clock. The simulator's <c>EditorScene</c> is the same three with a made-up ship in the third place,
    /// which is why the two can be kept honest against each other. What any screen does with the pointer
    /// and the keys is <see cref="GameScreen" />'s; what is here is only Sprocket's.
    /// </remarks>
    public sealed class SprocketScreen : GameScreen
    {
        readonly SprocketUnit _unit;
        readonly IMyTerminalBlock _opened;
        readonly Action _closed;

        /// <summary>
        /// The editor on <paramref name="unit" />, opened from <paramref name="opened" />, telling <paramref name="closed" /> each
        /// time it closes.
        /// </summary>
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

            // The toolbars, which nothing reads unless something asks: a slot may have been re-pointed since this
            // screen was last open, by this player or another one.
            _unit.World.LookAgain();
        }

        /// <summary>Hides the game's HUD while this is open, and gives it back as it closes.</summary>
        /// <remarks>
        /// Forced to its minimum rather than switched off, which leaves the player's own HUD setting
        /// alone: lifting the force puts back whatever they had. Player 0 is this machine's own player,
        /// done here and nowhere else, so no server is asked.
        /// </remarks>
        static void Hud(bool shown)
        {
            MyVisualScriptLogicProvider.ShowHud(shown, 0);
        }

        /// <summary>
        /// Writes the program back to the block on the way out.
        /// </summary>
        /// <remarks>
        /// Closing the editor is the moment somebody has finished saying something, so it is the cheapest
        /// honest place to put the program where a world save will find it.
        /// </remarks>
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

        /// <summary>
        /// Puts the status app on the screen of the programmable block the editor was opened from, where the screen is showing nothing.
        /// </summary>
        /// <remarks>
        /// Somebody who has just built their first rule has a block with automation in it and a blank
        /// panel on the front of it, and the connection between the two is not obvious from either. So
        /// the way out of the editor is where it is offered, once, by doing it.
        /// <para>
        /// <b>Only a surface set to nothing at all.</b> Anything else is a choice somebody made - a
        /// clock, an image, their own text - and taking that over would be the mod deciding it matters
        /// more than they do. A block with nothing built in it is left alone too: a status panel that
        /// says "nothing built yet" is worse than the blank screen it replaced.
        /// </para>
        /// </remarks>
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
