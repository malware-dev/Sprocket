using System;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    /// <summary>Where rich text is written: its markup in a box, the way to see it as it will read, and a guide to the marks it can use.</summary>
    /// <remarks>
    /// The box and its preview take turns in one room held to one height, so swapping them moves nothing under them. The guide
    /// hangs from its button in a window, since a box as narrow as a slot's has no room beside or under it for a guide as wide
    /// as its examples, and it is drawn by the same rich text as the preview, so it shows exactly what the preview will. Whoever holds it gives it its
    /// words through <see cref="Show" /> and reads what was written from <see cref="Text" />; the elements it is drawn with are
    /// declared in <c>Composer.View.cs</c>.
    /// </remarks>
    public partial class Composer : VStack
    {
        readonly Func<WindowLayer> _windows;
        string _examples;

        /// <summary>A composer whose box and preview are <paramref name="high" /> tall, previewing in <paramref name="reads" />.</summary>
        /// <param name="high">How tall the box and the preview are.</param>
        /// <param name="reads">The style the text is read in wherever it is shown, so the preview is what will be seen.</param>
        /// <param name="windows">Where the guide opens, read when it is opened, so they may be given after the composer is made.</param>
        public Composer(float high, IStyle reads, Func<WindowLayer> windows)
        {
            _windows = windows;
            CreateView(high, reads);
            _markup.Command = new Relay(Guide);
            _preview.Command = new Relay(delegate { Preview(!_previewing.IsVisible); });
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
                    _previewed.Document = Markdown.Parse(said);
            }
        }

        /// <summary>What the empty box shows.</summary>
        public string Hint
        {
            get { return _box.Hint; }
            set { _box.Hint = value; }
        }

        /// <summary>The box the markup is typed in, which holds the keyboard while it is being written.</summary>
        public EditBox Box => _box;

        /// <summary>Whether the text is shown as it will read rather than as it is written.</summary>
        public bool IsPreviewing => _previewing.IsVisible;

        /// <summary>Gives the buttons and the guide their words, from <paramref name="texts" />.</summary>
        /// <remarks>The guide's examples are parsed again only when the words have changed.</remarks>
        public void Show(Texts texts)
        {
            _markup.Text = texts.WritingMarkup;
            _markup.Tip = Tip.For(_markup.Tip, texts.WritingMarkupAbout);
            _preview.Text = _previewing.IsVisible ? texts.WritingEdit : texts.WritingPreview;
            _preview.Tip = Tip.For(_preview.Tip, _previewing.IsVisible ? null : texts.WritingPreviewAbout);
            _guideNote.Text = texts.WritingExamplesNote;

            if (ReferenceEquals(_examples, texts.WritingExamples))
                return;

            _examples = texts.WritingExamples;
            _examplesList.Children.Clear();

            var examples = _examples.Replace("\r", "").Split('\n');
            for (var i = 0; i < examples.Length; i++)
                _examplesList.Children.Add(Example(examples[i]));
        }

        /// <summary>Opens the guide to the marks under its button, or closes it where it is open.</summary>
        public void Guide()
        {
            var layer = _windows();
            if (layer == null)
                return;

            if (layer.IsOpen(_guide))
                layer.Close(_guide);
            else
                layer.Open(_guide);
        }

        /// <summary>Shows the text as it will read in place of its box, or the box again.</summary>
        public void Preview(bool showing)
        {
            if (showing)
                _previewed.Document = Markdown.Parse(_box.Text);

            _previewing.IsVisible = showing;
            _box.IsVisible = !showing;
            _preview.IsPressed = showing;
        }
    }
}
