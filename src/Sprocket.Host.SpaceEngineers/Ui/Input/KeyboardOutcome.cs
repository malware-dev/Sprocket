namespace Sprocket.Host.SpaceEngineers.Ui.Input
{
    /// <summary>What a pass over the keyboard did.</summary>
    public struct KeyboardOutcome
    {
        /// <summary>The text or the caret moved, so anything watching should look again.</summary>
        public bool Changed;

        /// <summary>Enter was pressed.</summary>
        public bool Commit;

        /// <summary>Tab was pressed: move to the next field of a form.</summary>
        public bool Onward;
    }
}