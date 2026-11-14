using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Look
{
    /// <summary>The sounds a control plays.</summary>
    /// <remarks>Each names a cue in the game's audio definitions, the ones its own menus play.</remarks>
    public static class ControlCues
    {
        /// <summary>The pointer coming onto something a press would run: HudMouseOver.</summary>
        public static readonly Sound Hover = new Sound("HudMouseOver");

        /// <summary>A press running what it was pressed on: HudMouseClick.</summary>
        public static readonly Sound Click = new Sound("HudMouseClick");
    }
}
