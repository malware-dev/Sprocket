using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SpaceEngineers.Game.GUI;
using VRageMath;
using VRage.Plugins;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The game client plugin that runs Sprocket's client tests, loaded by Run-ClientTests.ps1 through SevrLoader. At the main menu
    /// it loads the world in SPROCKET_CLIENTTEST_WORLD, runs the tests in it with the blueprints in SPROCKET_GAMETEST_BLUEPRINTS,
    /// writes pictures to SPROCKET_CLIENTTEST_SHOTS and its report to SPROCKET_CLIENTTEST_REPORT, and closes the game. Without a
    /// report to write it does nothing.
    /// </summary>
    public sealed class ClientTestPlugin : IPlugin
    {
        GameTestReport _report;
        string _world;
        string _blueprints;
        string _shots;
        int _menuUpdates;
        int _loadUpdates;
        bool _loading;
        IEnumerator<int> _steps;
        int _waiting;
        bool _odd;

        /// <inheritdoc />
        public void Init(object gameInstance)
        {
            var report = Environment.GetEnvironmentVariable("SPROCKET_CLIENTTEST_REPORT");
            if (string.IsNullOrEmpty(report))
                return;

            _report = new GameTestReport(report);
            _world = Environment.GetEnvironmentVariable("SPROCKET_CLIENTTEST_WORLD");
            _blueprints = Environment.GetEnvironmentVariable("SPROCKET_GAMETEST_BLUEPRINTS");
            _shots = Environment.GetEnvironmentVariable("SPROCKET_CLIENTTEST_SHOTS");
            _report.Note($"client plugin loaded, user data in {VRage.FileSystem.MyFileSystem.UserDataPath}");
        }

        /// <inheritdoc />
        /// <remarks>Called by the game once a frame. It does nothing without a report to write, and nothing after the tests end.</remarks>
        public void Update()
        {
            if (_report == null)
                return;

            // The client calls a plugin sixty times a second, measured (5,400 updates in 90 s), and the tests count in the server's
            // thirty (PluginUpdates), so every other call is let by.
            _odd = !_odd;
            if (_odd)
                return;

            if (!_loading)
            {
                Menu();
                return;
            }

            if (MySession.Static == null || !MySession.Static.Ready)
            {
                // Three minutes is many times what the test world takes; past that the game is waiting on something, such as a
                // message box, and says so in its log.
                _loadUpdates++;
                if (_loadUpdates < 180 * PluginUpdates.PerSecond)
                    return;

                _report.Fail("client world", $"the world in {_world} did not load within three minutes; the game's log says why");
                Close();
                return;
            }

            if (_steps == null)
            {
                _report.Pass("client world", $"the world in {_world} loaded");
                _steps = Steps();
            }

            if (_waiting > 0)
            {
                _waiting--;
                return;
            }

            bool more;
            try
            {
                more = _steps.MoveNext();
            }
            catch (Exception exception)
            {
                _report.Fail("client run", exception.ToString());
                more = false;
            }

            if (more)
            {
                _waiting = _steps.Current;
                return;
            }

            Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>Waits ten seconds at the main menu, then loads the test world, or closes the game where there is none.</summary>
        void Menu()
        {
            if (MySession.Static != null)
                return;

            _menuUpdates++;
            if (_menuUpdates < 10 * PluginUpdates.PerSecond)
                return;

            _report.Pass("client start", "the game reached its main menu with the plugin loaded");
            if (string.IsNullOrEmpty(_world))
            {
                Close();
                return;
            }

            // The test world turns on scripts and block limits, which the game loads only in experimental mode, and asks about
            // in a message box otherwise. The setting is the test user data's own (Run-ClientTests.ps1 -AppData).
            MySandboxGame.Config.ExperimentalMode = true;

            // A new user data folder is greeted by a welcome dialog until its config says it has been seen.
            MySandboxGame.Config.WelcomScreenCurrentStatus = MyConfig.WelcomeScreenStatus.AlreadySeen;
            CloseDialogs();
            _loading = true;
            MySessionLoader.LoadSingleplayerSession(_world);
        }

        IEnumerator<int> Steps()
        {
            // Five seconds of a running world before anything is pasted into it.
            yield return 5 * PluginUpdates.PerSecond;

            // A player new to the world stands at the respawn screen, which covers everything; the screen's own request spawns
            // them in a space suit.
            if (MySession.Static.LocalCharacter == null)
            {
                MyPlayerCollection.RespawnRequest(true, false, 0, null, 0, null, Color.White);
                yield return 5 * PluginUpdates.PerSecond;
                MyGuiScreenMedicals.Close();
                yield return PluginUpdates.PerSecond;
            }

            CloseDialogs();
            yield return PluginUpdates.PerSecond;

            _report.Check("client character", MySession.Static.LocalCharacter != null, MySession.Static.LocalCharacter == null
                ? "the player has no character after the respawn" : $"the player stands at {MySession.Static.LocalCharacter.PositionComp.GetPosition()}");

            var shot = new SceneShotClientTest(_blueprints, _shots, _report).Run();
            while (shot.MoveNext())
                yield return shot.Current;
        }

        /// <summary>Closes the welcome and the consent dialogs the main menu opens for new user data, answering neither.</summary>
        /// <remarks>An open dialog pauses a single player world. The consent is the player's own to give, so it is closed unanswered.</remarks>
        static void CloseDialogs()
        {
            MyScreenManager.GetFirstScreenOfType<MyGuiScreenWelcomeScreen>()?.CloseScreen();
            MyScreenManager.GetFirstScreenOfType<MyGuiScreenGDPR>()?.CloseScreen();
        }

        void Close()
        {
            _report.Finish();
            _report = null;
            MySandboxGame.ExitThreadSafe();
        }
    }
}
