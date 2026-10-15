using System;
using System.Globalization;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    /// <summary>Text written over several lines on a page of its own: a screen's text or a block's Custom Data, which a form has no room for.</summary>
    /// <remarks>
    /// Opened from the field in a step's form, and gives what it holds back to that field once, on Done, rather than on every
    /// key, since the text can run to a hundred thousand characters. Going back after a change asks whether to keep it, as
    /// leaving the picker does. Held to what the block holds, with the count of characters beside the most it may be. The
    /// box is declared in <c>TextPage.View.cs</c>.
    /// </remarks>
    public sealed partial class TextPage : EditorPage
    {
        readonly string _title;
        readonly string _given;
        readonly int _longest;
        readonly Action<string> _keep;

        /// <summary>
        /// A page titled <paramref name="title" /> holding <paramref name="given" />, up to <paramref name="longest" /> characters
        /// or any number for zero, that hands what it holds to <paramref name="keep" /> when it is kept.
        /// </summary>
        public TextPage(SprocketEditor editor, string title, string given, int longest, Action<string> keep) : base(editor)
        {
            _title = title;
            _given = given ?? "";
            _longest = longest;
            _keep = keep;
        }

        /// <inheritdoc />
        public override string Title => _title;

        /// <summary>The box is the whole of the page and scrolls both ways by itself.</summary>
        public override bool ScrollsItself => true;

        /// <summary>Finished with Done, which keeps the text, as well as left by going back.</summary>
        public override bool Edits => true;

        /// <summary>The box the text is written in, where the view has been made.</summary>
        public EditBox Box => _box;

        /// <summary>How many characters it holds of the most it may, as the page says it.</summary>
        public string Counted => _count == null ? "" : _count.Text;

        /// <inheritdoc />
        /// <remarks>Nothing on it follows the program; what it shows is what is typed.</remarks>
        public override void Build()
        {
        }

        /// <summary>Keeps the text and goes back to the form.</summary>
        public override void Primary()
        {
            if (_box != null)
                _keep(_box.Text);

            Editor.Pop();
        }

        /// <summary>Asks whether to keep the text, where it has changed; both answers go back.</summary>
        public override bool Leaving()
        {
            if (_box == null || _box.Text == _given)
                return true;

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickKeepThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, delegate { Editor.Pop(); });
            return false;
        }

        /// <summary>Fills the box, holds it to the most the block takes, and gives it the keyboard.</summary>
        protected override void ViewCreated()
        {
            _box.Longest = _longest;
            _box.Text = _given;
            _box.Edited += Count;
            Count();

            if (Editor.Input != null)
                Editor.Input.Focus(_box);
        }

        /// <summary>Says how many characters the box holds of the most it may, where there is a most.</summary>
        void Count()
        {
            _count.IsVisible = _longest > 0;
            if (_longest <= 0)
                return;

            var said = Template.Fill(Editor.Texts.WritingCount, "count", _box.Length.ToString(CultureInfo.InvariantCulture));
            _count.Text = Template.Fill(said, "most", _longest.ToString(CultureInfo.InvariantCulture));
        }
    }
}
