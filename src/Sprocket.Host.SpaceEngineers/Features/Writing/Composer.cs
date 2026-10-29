using Sprocket.Host.SpaceEngineers.Localization;
using Lumel.Styling;
using Lumel.Input;
using Lumel.Layout;
using Lumel;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    /// <summary>Where rich text is written: its markup in a box, the way to see it as it will read, and the way to the guide to the marks it can use.</summary>
    public partial class Composer : VStack
    {
        /// <summary>A composer whose box and preview are <paramref name="high" /> tall, previewing in <paramref name="reads" />.</summary>
        /// <param name="high">How tall the box and the preview are.</param>
        /// <param name="reads">The style the text is read in.</param>
        public Composer(float high, IStyle<RichText> reads)
        {
            CreateView(high, reads);
            _preview.Command = new RelayCommand(() => Preview(!_previewing.IsVisible));
        }

        /// <summary>The markup written, which setting replaces, leaving the box as a new piece of writing.</summary>
        public string Text
        {
            get { return _box.Text; }
            set
            {
                var said = value ?? "";
                if (_box.Text == said)
                    return;

                _box.Text = said;
                if (_previewing.IsVisible)
                    _previewed.Markdown = said;
            }
        }

        /// <summary>What the empty box shows.</summary>
        public string Hint
        {
            get { return _box.Hint; }
            set { _box.Hint = value; }
        }

        /// <summary>The box the markup is typed in, which holds the keyboard while it is being written.</summary>
        public TextArea Box => _box;

        /// <summary>What the button for the guide to the marks does.</summary>
        public ICommand Guides
        {
            get { return _markup.Command; }
            set { _markup.Command = value; }
        }

        /// <summary>Whether the text is shown as it will read rather than as it is written.</summary>
        public bool IsPreviewing => _previewing.IsVisible;

        /// <summary>Gives the buttons their words, from <paramref name="texts" />.</summary>
        public void Show(Texts texts)
        {
            _markup.Text = texts.WritingMarkup;
            _markup.Tip = Tip.For(_markup.Tip, texts.Codex.WritingMarkupAbout);
            _preview.Text = _previewing.IsVisible ? texts.WritingEdit : texts.WritingPreview;
            _preview.Tip = Tip.For(_preview.Tip, _previewing.IsVisible ? null : texts.Codex.WritingPreviewAbout);
        }

        /// <summary>Shows the text as it will read in place of its box, or the box again.</summary>
        public void Preview(bool showing)
        {
            if (showing)
                _previewed.Markdown = _box.Text;

            _previewing.IsVisible = showing;
            _box.IsVisible = !showing;
            _preview.Set(LookStates.Chosen, showing);
        }
    }
}
