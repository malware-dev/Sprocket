using Phosphor;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Reactor.Host.SpaceEngineers
{
    /// <summary>
    /// The mod's entry point. Nothing of Reactor's editor is wired to it yet; what it establishes is the route
    /// in - a button on the programmable block that shuts the terminal and hands the editor a world it can
    /// take input from.
    /// <para>
    /// It updates every tick even on a dedicated server, which draws nothing and has no terminal. That is not
    /// waste: input suspension is applied server-side and has to be released for a player whose client went
    /// away, and nobody else is left to do it.
    /// </para>
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ReactorSession : MySessionComponentBase
    {
        private InputSuspension m_suspension;
        private TextEditor m_editor;
        private ScreenSpaceGraphics m_graphics;
        private TextBox m_box;
        private MousePointer m_pointer;

        /// <summary>
        /// The editor draws nothing until somebody opens it, so adding the mod to a world changes nothing a
        /// player can see. It is a thing you open, never something that greets you.
        /// </summary>
        private bool m_open;

        /// <summary>
        /// The block the editor was opened from. Held because the terminal is gone by the time we draw, so
        /// InteractedEntity has stopped answering.
        /// </summary>
        private IMyTerminalBlock m_block;

        private bool m_started;

        /// <summary>
        /// Set when the button is pressed, cleared once the terminal has actually gone. Opening cannot happen
        /// in the same breath as the press: the terminal is still up and its cursor still showing, and the
        /// abandon sweep would see that and throw the edit away before the first frame was drawn.
        /// </summary>
        private bool m_opening;

        private int m_openingSince;

        public override void BeforeStart()
        {
            if (m_started)
                return;

            m_started = true;

            // Built on every machine, server included: the server is the only place a blacklist can be
            // applied or lifted, and it has to hear the messages to do either.
            m_suspension = new InputSuspension();

            // True because this target is pointed at with a mouse: the movement that would turn the player's
            // head is the movement that moves the pointer. A text panel target would pass false, since there
            // looking is the only way to aim at it.
            m_editor = new TextEditor(m_suspension, true);

            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
                return;

            m_graphics = new ScreenSpaceGraphics(MyScreenSpaceLayer.AboveHud);
            m_pointer = new MousePointer();

            IMyTerminalControlButton open = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("Reactor_Open");

            open.Title = MyStringId.GetOrCompute("Reactor");
            open.Tooltip = MyStringId.GetOrCompute("Open the Reactor editor.");
            open.SupportsMultipleBlocks = false;
            open.Visible = block => true;
            open.Enabled = block => true;
            open.Action = Open;

            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(open);
        }

        protected override void UnloadData()
        {
            // The blacklist is process state rather than world state, so anything left standing would follow
            // the player into whatever world they load next.
            if (m_suspension != null)
                m_suspension.Dispose();

            m_suspension = null;
            m_editor = null;
            m_graphics = null;
            m_box = null;
        }

        /// <summary>
        /// Closes the terminal before showing anything, because input is the constraint. A mod can intercept
        /// input aimed at the game world; there is no equivalent for the terminal, so an editor left inside it
        /// could be drawn but never operated. The world staying visible behind it is a bonus, not the reason.
        /// </summary>
        private void Open(IMyTerminalBlock block)
        {
            m_block = block;
            m_opening = true;
            m_openingSince = MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;

            MyAPIGateway.Gui.CloseTerminalScreen();
        }

        /// <summary>
        /// Finishes what <see cref="Open" /> asked for, once the terminal has closed and taken its cursor
        /// with it. Gives up after a couple of seconds rather than waiting on something that is not coming.
        /// </summary>
        private void ContinueOpening()
        {
            int now = MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;

            if (now - m_openingSince > 120)
            {
                // Logged rather than shown: it means the game did not do something it was asked to, which is
                // a thing to find in a log rather than a thing to bother a player about.
                m_opening = false;
                MyLog.Default.WriteLineAndConsole("[Reactor] terminal did not close; not opening");
                return;
            }

            if (MyAPIGateway.Gui == null
                || MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.None
                || MyAPIGateway.Gui.IsCursorVisible)
                return;

            m_opening = false;
            m_open = true;

            if (m_box == null)
            {
                m_box = new TextBox();
                m_box.Scale = 0.7f;
                m_box.Background = new Color(20, 34, 48, 235);
                m_box.Committed += OnNameCommitted;
            }

            m_box.Text = m_block == null ? "" : m_block.CustomName;
            m_editor.Begin(m_box);

            // Starts in the middle rather than wherever it was left, so it is always somewhere the player is
            // already looking when the UI appears.
            m_pointer.Center(m_graphics.ScreenBounds);
        }

        /// <summary>
        /// Writes the name back to the block. The game's own word for it, edited in place, which is what
        /// docs/Editor.md says naming is; and the setter syncs, so a client renaming it is seen by everyone.
        /// </summary>
        private void OnNameCommitted(TextBox box)
        {
            if (m_block != null)
                m_block.CustomName = box.Text;
        }

        public override void UpdateAfterSimulation()
        {
            if (m_editor != null)
                m_editor.Update();

            if (m_opening)
                ContinueOpening();
        }

        public override void HandleInput()
        {
            if (!m_open || m_editor == null)
                return;

            m_pointer.Update(m_graphics.ScreenBounds);
            m_editor.HandleInput();

            // The edit ending for any reason takes the overlay with it, for now. Once there is more than one
            // thing on this screen that will become a focus question rather than a session one.
            if (!m_editor.IsEditing)
            {
                m_open = false;
                m_block = null;
            }
        }

        public override void Draw()
        {
            if (!m_open)
                return;

            m_graphics.Begin();

            Vector2 size = m_graphics.Size;
            const float width = 520f;

            m_box.Measure(m_graphics, new Vector2(width, size.Y));
            m_box.Arrange(new RectangleF((size.X - width) * 0.5f, size.Y * 0.3f, width, m_box.DesiredSize.Y));
            m_box.Draw(m_graphics);

            // Last, so it is over everything the UI drew.
            m_pointer.Draw(m_graphics);
        }
    }
}
