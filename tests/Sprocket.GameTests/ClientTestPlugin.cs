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
    /// writes pictures to SPROCKET_CLIENTTEST_SHOTS and its report to SPROCKET_CLIENTTEST_REPORT, and closes the game. With
    /// SPROCKET_CLIENTTEST_JOIN, a server's address, it joins that server instead and runs the tests of a client joined to it
    /// (Run-JoinedTests.ps1). Without a report to write it does nothing.
    /// </summary>
    public sealed class ClientTestPlugin : IPlugin
    {
        GameTestReport _report;
        string _world;
        string _join;
        string _blueprints;
        string _shots;
        int _menuUpdates;
        int _loadUpdates;
        bool _loading;
        IEnumerator<int> _steps;
        readonly EditorInput _input = new EditorInput();
        int _waiting;
        bool _odd;
        bool _respawned;

        /// <inheritdoc />
        public void Init(object gameInstance)
        {
            var report = Environment.GetEnvironmentVariable("SPROCKET_CLIENTTEST_REPORT");
            if (string.IsNullOrEmpty(report))
                return;

            _report = new GameTestReport(report);
            _world = Environment.GetEnvironmentVariable("SPROCKET_CLIENTTEST_WORLD");
            _join = Environment.GetEnvironmentVariable("SPROCKET_CLIENTTEST_JOIN");
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

            _input.Hold();

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
                // An offline world pauses while a screen is open, and counts towards ready only while it runs, so the respawn
                // screen a new player stands at is answered here: its own request spawns them in a space suit.
                if (!_respawned && MySession.Static != null && MySession.Static.LocalCharacter == null && MyGuiScreenMedicals.Static != null)
                {
                    _respawned = true;
                    MyPlayerCollection.RespawnRequest(true, false, 0, null, 0, null, Color.White);
                }

                // The screen stays open until the character it asked for is there, and closing it sooner leaves it open.
                if (MySession.Static != null && MySession.Static.LocalCharacter != null && MyGuiScreenMedicals.Static != null)
                    MyGuiScreenMedicals.Close();

                // Three minutes is many times what the test world takes; past that the game is waiting on something, such as a
                // message box, and says so in its log.
                _loadUpdates++;
                if (_loadUpdates < 180 * PluginUpdates.PerSecond)
                    return;

                _report.Fail("client world", $"the world in {_world} did not load within three minutes: a session {(MySession.Static != null ? "is" : "is not")} open, its character is {(MySession.Static?.LocalCharacter == null ? "missing" : "there")}, the respawn screen is {(MyGuiScreenMedicals.Static == null ? "closed" : "open")}, and a respawn {(_respawned ? "was" : "was not")} asked for");
                Close();
                return;
            }

            if (_steps == null)
            {
                if (!string.IsNullOrEmpty(_join))
                {
                    // A joined client loads Sprocket from the Workshop download the runner put the local build in.
                    _report.Pass("client world", $"joined the server at {_join}");
                    _steps = Joined();
                }
                else
                {
                    _report.Pass("client world", $"the world in {_world} loaded");
                    var local = MySession.Static.Mods.Exists(mod => mod.PublishedFileId == 0 && mod.Name.StartsWith("Sprocket Automation"));
                    _report.Check("client mod", local, local ? "Sprocket is the local copy built from this repository" : "Sprocket is not the local copy, so the tests run the published build");
                    _steps = Steps();
                }
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

        /// <summary>Waits ten seconds at the main menu, then joins the test server or loads the test world, or closes the game where it has neither.</summary>
        void Menu()
        {
            if (MySession.Static != null)
                return;

            _menuUpdates++;
            if (_menuUpdates < 10 * PluginUpdates.PerSecond)
                return;

            _report.Pass("client start", "the game reached its main menu with the plugin loaded");
            if (string.IsNullOrEmpty(_world) && string.IsNullOrEmpty(_join))
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
            if (!string.IsNullOrEmpty(_join))
                MyJoinGameHelper.JoinServer(_join);
            else
                MySessionLoader.LoadSingleplayerSession(_world);
        }

        /// <summary>The tests of a client joined to the dedicated server, which stands in for the other players.</summary>
        IEnumerator<int> Joined()
        {
            // Five seconds of a running world, and the dialogs a new user data folder opens closed.
            yield return 5 * PluginUpdates.PerSecond;
            CloseDialogs();
            yield return PluginUpdates.PerSecond;

            // A server is ready before the player has a body: the respawn screen it shows is answered with a space suit, as a new
            // player answers it, and closed once the character stands; a minute is many times what that takes.
            if (MySession.Static.LocalCharacter == null)
                MyPlayerCollection.RespawnRequest(true, false, 0, null, 0, null, Color.White);
            for (var waited = 0; waited < 60 && MySession.Static.LocalCharacter == null; waited++)
                yield return PluginUpdates.PerSecond;

            if (MyGuiScreenMedicals.Static != null)
                MyGuiScreenMedicals.Close();
            _report.Check("joined character", MySession.Static.LocalCharacter != null, MySession.Static.LocalCharacter != null
                ? "the player stands in a space suit" : "the player has no character a minute after asking to respawn");
            yield return 2 * PluginUpdates.PerSecond;

            var notice = new TerminalNoticeClientTest(_shots, _report, _input).Run();
            while (notice.MoveNext())
                yield return notice.Current;
        }

        IEnumerator<int> Steps()
        {
            // Five seconds of a running world before anything is pasted into it.
            yield return 5 * PluginUpdates.PerSecond;

            CloseDialogs();
            yield return PluginUpdates.PerSecond;

            _report.Check("client character", MySession.Static.LocalCharacter != null, MySession.Static.LocalCharacter == null
                ? "the player has no character after the respawn" : $"the player stands at {MySession.Static.LocalCharacter.PositionComp.GetPosition()}");

            var shot = new SceneShotClientTest(_blueprints, _shots, _report).Run();
            while (shot.MoveNext())
                yield return shot.Current;

            var kept = new KeptCountClientTest(_blueprints, _shots, _report).Run();
            while (kept.MoveNext())
                yield return kept.Current;

            var tour = new EditorTour(_shots, _report, _input);
            var editor = new EditorShotClientTest(tour, _report).Run("Kept Automation");
            while (editor.MoveNext())
                yield return editor.Current;

            var move = new MoveItemsClientTest(_blueprints, _report, tour).Run();
            while (move.MoveNext())
                yield return move.Current;
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
