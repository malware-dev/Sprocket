using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Writing
{
    /// <summary>Text written over several lines on a page of its own: a screen's text or a block's Custom Data, which a form has no room for.</summary>
    public sealed partial class TextPage : EditorPage
    {
        readonly string _title;
        readonly string _given;
        readonly int _longest;
        readonly Action<string> _save;

        /// <summary>
        /// A page titled <paramref name="title" /> holding <paramref name="given" />, up to <paramref name="longest" /> characters
        /// or any number for zero, that hands what it holds to <paramref name="save" /> when it is saved.
        /// </summary>
        public TextPage(SprocketEditor editor, string title, string given, int longest, Action<string> save) : base(editor)
        {
            _title = title;
            _given = given ?? "";
            _longest = longest;
            _save = save;
        }

        /// <inheritdoc />
        public override string Title => _title;

        /// <summary>The box is the whole of the page and scrolls both ways by itself.</summary>
        public override bool ScrollsItself => true;

        /// <summary>Finished with Done, which saves the text, as well as left by going back.</summary>
        public override bool Edits => true;

        /// <summary>The box the text is written in, where the view has been made.</summary>
        public TextArea Box => _box;

        /// <summary>How many characters it holds of the most it may, as the page says it.</summary>
        public string Counted => _count == null ? "" : _count.Text;

        /// <inheritdoc />
        public override void Build()
        {
        }

        /// <summary>Goes back to the form and saves the text there.</summary>
        /// <remarks>Back first, so the form hears the text while its own page is the one showing, as it does any other change.</remarks>
        public override void Primary()
        {
            var text = _box != null ? _box.Text : null;
            Editor.Pop();

            if (text != null)
                _save(text);
        }

        /// <summary>Asks whether to save the text, where it has changed; both answers go back.</summary>
        public override bool Leaving()
        {
            if (_box == null || _box.Text == _given)
                return true;

            Editor.Ask(Editor.Texts.PickSaveQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickSaveThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, () => Editor.Pop());
            return false;
        }

        /// <summary>Fills the box, holds it to the most the block takes, and gives it the keyboard.</summary>
        protected override void ViewCreated()
        {
            _box.MaxLength = _longest;
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
