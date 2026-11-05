using System;
using Lumel;
using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A color on one line, shown as the color itself, its panel hanging open under it when pressed.</summary>
    /// <remarks>
    /// The panel gives the field its color when Done is pressed under it, or Enter; put away any other way, it gives nothing,
    /// and the field goes on showing the color it held.
    /// </remarks>
    public class ColorField : ColorPicker, IField
    {
        string _text = "";

        /// <summary>A field holding no color, its panel offering what <paramref name="offer" /> holds, its notes in <paramref name="texts" />.</summary>
        public ColorField(ColorOffer offer, Texts texts)
        {
            Style = Styles.ColorBox;
            Panel.Style = Styles.ColorPanel;
            Focusable = true;
            DoneText = texts.ChromeDone;
            SetPalette(offer.Paint);
            Recent = offer.Recent;
            Entry = offer.Entry;
            EntryChanged += () => offer.Entry = Entry;

            Panel.SpectrumTip = CodexTip.For(null, texts.Codex.ColorPlaneAbout);
            Panel.HueTip = CodexTip.For(null, texts.Codex.ColorHue);
            Panel.SaturationTip = CodexTip.For(null, texts.Codex.ColorSaturation);
            Panel.ValueTip = CodexTip.For(null, texts.Codex.ColorBrightness);
            Panel.CodeTip = CodexTip.For(null, texts.Codex.ColorCodeAbout);

            ColorChanged += delegate
            {
                _text = Color.HasValue ? ColorChannels.Code(Color.Value) : "";
                Shown();

                var edited = Edited;
                if (edited != null)
                    edited();
            };

            Shown();
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                Color color;
                byte alpha;
                if (ColorChannels.TryRead(_text, out color, out alpha))
                {
                    color.A = alpha;
                    Color = color;
                }
                else
                    Color = null;

                Shown();
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set
            {
                Set(LookStates.Wrong, value);
                Panel.SetCodeState(LookStates.Wrong, value);
            }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <summary>Has the code of the color it holds said under the pointer, where it holds one.</summary>
        void Shown()
        {
            Tip = CodexTip.For(Tip, _text.Length > 0 ? _text : null);
        }
    }
}
