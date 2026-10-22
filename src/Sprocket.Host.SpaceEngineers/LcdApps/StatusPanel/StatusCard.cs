using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>One installation on the status panel that wants looking at, in its share of a column: its mark, its logic system and its name close together, and what is wrong.</summary>
    public partial class StatusCard : Grid
    {
        /// <summary>A card showing nothing yet.</summary>
        public StatusCard()
        {
            CreateView();
        }

        /// <summary>The card itself, without the room around it.</summary>
        public VStack Body => _body;

        /// <summary>Shows <paramref name="item" /> of <paramref name="status" /> whole, in <paramref name="look" />.</summary>
        public void Show(StatusLook look, ConstructStatus status, StatusItem item)
        {
            var size = look.Size;
            _mark.Style = size.Mark;
            _mark.Symbol = StatusLook.Mark(item.Level);
            var shade = look.Shade(item.Level);
            _mark.Tint = look.IsFresh(item) ? Color.Lerp(shade, look.Ink, look.Pulse) : shade;
            _underMark.Placement = FixedSize.High(size.UnderMark);

            var over = Over(look.Texts, item);
            _system.Style = size.Label;
            _system.Color = look.Quiet;
            _system.Text = over;
            _system.IsVisible = over.Length > 0;
            _underLabel.Placement = FixedSize.High(size.UnderLabel);
            _underLabel.IsVisible = _system.IsVisible;

            _name.Style = size.Name;
            _name.Color = look.Ink;
            _name.Text = Title(look.Texts, item);
            _underName.Placement = FixedSize.High(size.UnderName);
            _underName.IsVisible = true;

            _sentence.Style = size.Sentence;
            _sentence.Color = look.Ink;
            _sentence.Text = Said(look.Texts, status, item);
            _sentence.IsVisible = true;
        }

        /// <summary>Leaves out, until the card is no taller than <paramref name="high" /> laid <paramref name="wide" /> across, the sentence, the logic system, and then the name's size.</summary>
        public void Fit(IGraphics graphics, float wide, float high)
        {
            if (StatusLook.Tall(graphics, _body, wide) > high)
            {
                _sentence.IsVisible = false;
                _underName.IsVisible = false;
            }

            if (StatusLook.Tall(graphics, _body, wide) > high)
            {
                _system.IsVisible = false;
                _underLabel.IsVisible = false;
            }

            if (StatusLook.Tall(graphics, _body, wide) > high)
                _name.Style = _sentence.Style;
        }

        /// <summary>What the installation is called: its own name where it is one of a blueprint's, and its logic system's otherwise.</summary>
        static string Title(Texts texts, StatusItem item)
        {
            if (item.Level == StatusLevel.Stranded)
                return texts.StrandedHeading;

            if (item.Level == StatusLevel.Paused)
                return texts.PausedHeading;

            return item.Blueprint
                ? SystemSummary.DescribeInstance(texts, item.Installation, item.Position)
                : SystemSummary.Describe(texts, item.Logic, item.Number);
        }

        /// <summary>The logic system an installation of a blueprint belongs to, or nothing where its title already names it.</summary>
        static string Over(Texts texts, StatusItem item)
        {
            return item.Blueprint ? SystemSummary.Describe(texts, item.Logic, item.Number) : "";
        }

        /// <summary>What is wrong with the installation, naming the step and the block where they are known.</summary>
        static string Said(Texts texts, ConstructStatus status, StatusItem item)
        {
            if (item.Level == StatusLevel.Stranded)
                return texts.StrandedBanner;

            if (item.Level == StatusLevel.Paused)
                return Template.Fill(texts.PausedBanner, "construct", status.Construct);

            if (item.Level == StatusLevel.Off)
                return texts.PanelSwitchedOff;

            var fault = item.Fault;
            if (item.Halted)
                return SequenceText.Stopped(texts, new Halt(fault.Problem, 0, fault.Step, fault.Because, fault.Which), item.Sequence, item.Row, false);

            return SequenceText.Failing(texts, fault, item.Sequence, item.Alone, item.Row);
        }
    }
}
