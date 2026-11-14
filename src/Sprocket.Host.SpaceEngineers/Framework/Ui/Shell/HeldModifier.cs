
namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>
    /// What was being held down when something was pressed, said as what it means rather than as
    /// which key it was.
    /// </summary>
    public enum HeldModifier
    {
        /// <summary>Nothing held: this one, and nothing else.</summary>
        Alone,

        /// <summary>This one as well, leaving the rest alone.</summary>
        Also,

        /// <summary>Everything from the last one taken to this one.</summary>
        Through
    }
}
