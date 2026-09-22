namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// One piece of a screen's heading: some words, and whether they are somebody's to change.
    /// </summary>
    /// <remarks>
    /// One string and one rename beside it is enough while a screen is about one thing. A sequence is
    /// not: it is <i>when this happens</i> and <i>then this runs</i>, so a name replacing the whole line
    /// would cost anybody who wanted a better word for what a sequence <b>does</b> the trigger that
    /// starts it, recoverable only by typing the trigger into the name by hand.
    /// <para>
    /// So a heading is several of these, in order, and each one answers for itself whether it can be
    /// renamed. There is deliberately no joined-up version of the whole thing on
    /// <see cref="EditorScreen" />: a target handed one string would draw one string, and the one thing a
    /// heading has to say now is <b>which part of it can be changed</b>.
    /// </para>
    /// <para>
    /// Words between the parts are the target's, not core's - an arrow, a dot, a line break on something
    /// narrow. A heading that genuinely needs a connective word can carry it as a part with no rename
    /// command, which is the same mechanism rather than a second one.
    /// </para>
    /// </remarks>
    public sealed class HeadingPart : Observable
    {
        public HeadingPart(string text)
        {
            Text = text ?? "";
            Name = "";
            Prompt = "";
            Lead = "";
        }

        /// <summary>
        /// Words that come before this part and belong to it, or empty for none.
        /// </summary>
        /// <remarks>
        /// <i>Automation for</i> <b>Hangar Bay Alpha</b> is one named thing with a phrase in front of
        /// it, not two things. The phrase is core's because it says what the screen is about; the
        /// name is somebody's to change and the phrase is not, so they cannot be the same part.
        /// <para>
        /// It also settles the punctuation. A target puts its own mark between parts - an arrow, a dot -
        /// but only where the next part has no words of its own to lead with, because a connective and a
        /// connective are one connective too many.
        /// </para>
        /// </remarks>
        public string Lead
        {
            get { return _lead; }
            set { Set(ref _lead, value, nameof(Lead)); }
        }

        string _lead;

        /// <summary>
        /// What this part reads as. Never empty.
        /// </summary>
        /// <remarks>
        /// The name where somebody gave one, and whatever the thing says about itself where they did not.
        /// A part always has something to read, which is what makes naming optional rather than a step
        /// somebody is quietly expected to complete.
        /// </remarks>
        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, nameof(Text)); }
        }

        string _text;

        /// <summary>
        /// What somebody actually typed, or empty where nobody has.
        /// </summary>
        /// <remarks>
        /// <b>Not the same as <see cref="Text" /> and the difference is load-bearing.</b> A target opens
        /// its rename box on this; opening it on the text would hand somebody a generated sentence and
        /// call it their name, which they then have to delete before they can answer.
        /// </remarks>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value, nameof(Name)); }
        }

        string _name;

        /// <summary>Where a new name for this part is sent, or zero when it cannot be renamed.</summary>
        public int RenameCommand
        {
            get { return _renameCommand; }
            set { Set(ref _renameCommand, value, nameof(RenameCommand)); }
        }

        int _renameCommand;

        public bool CanRename => RenameCommand != 0;

        /// <summary>What to ask when offering the rename.</summary>
        public string Prompt
        {
            get { return _prompt; }
            set { Set(ref _prompt, value, nameof(Prompt)); }
        }

        string _prompt;

        /// <summary>
        /// Whether an empty name is an answer here, or is not allowed to be given.
        /// </summary>
        /// <remarks>
        /// Nearly everywhere an empty name means "go back to whatever this thing says about itself", which
        /// is what makes naming optional. Where there is nothing for it to say, clearing the name has to be
        /// refused instead - and a target has to be told which of the two it is, or it offers a commit that
        /// core will silently ignore.
        /// </remarks>
        public bool NameRequired
        {
            get { return _nameRequired; }
            set { Set(ref _nameRequired, value, nameof(NameRequired)); }
        }

        bool _nameRequired;
    }
}
