using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    partial class SequenceBar
    {
        Glyph _light;
        Label _status;
        Label _problem;
        Label _lastRun;
        Label _skipped;
        Glyph _run;
        Glyph _undo;
        Glyph _redo;
        Glyph _delete;
        Switch _switch;

        /// <summary>Makes the state on the left, the news of what was skipped, and the tools on the right.</summary>
        void CreateView()
        {
            Spacing = 16f;

            // The light leads the state, because it is the same fact said faster: a green flash is what a sequence
            // firing looks like from across the room.
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new Glyph { Symbol = Symbols.Disc, Style = Styles.Light, Placement = Fixed.Size(16f, 16f) }.As(out _light),
                    new Label { Style = Styles.Status }.As(out _status),
                    new Label { Style = Styles.Status, IsVisible = false }.As(out _problem),
                    new Label { Style = Styles.Aside }.As(out _lastRun)
                }
            });

            Children.Add(new HStack
            {
                Placement = Flex.Grow,
                Children = { new Label { Style = Styles.Skipped, IsVisible = false }.As(out _skipped) }
            });

            // The switch is last, and that is the point of the order: the row stands against the right edge, so a
            // control's place depends only on what stands to its right, and the last one cannot be moved by anything.
            // The run mark is first because it is the one control that comes and goes, and first is the one place where
            // going moves nothing else.
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new Glyph { Symbol = Symbols.Play, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f) }.As(out _run),
                    new Glyph { Symbol = Symbols.Undo, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f) }.As(out _undo),
                    new Glyph { Symbol = Symbols.Redo, Style = Styles.Icon, Placement = Fixed.Size(32f, 32f) }.As(out _redo),
                    new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = Fixed.Size(32f, 32f) }.As(out _delete),
                    new Switch { Style = Styles.Switch }.As(out _switch)
                }
            });
        }
    }
}
