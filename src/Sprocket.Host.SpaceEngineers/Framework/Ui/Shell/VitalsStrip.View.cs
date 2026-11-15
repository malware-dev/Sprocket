using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    partial class VitalsStrip
    {
        HStack _health;
        Label _healthWord;
        ProgressBar _healthBar;
        HStack _energy;
        Label _energyWord;
        ProgressBar _energyBar;
        HStack _oxygen;
        Label _oxygenWord;
        ProgressBar _oxygenBar;
        HStack _food;
        Label _foodWord;
        ProgressBar _foodBar;

        /// <summary>Makes a word and a bar for each of health, energy, oxygen and food.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Sections;
            Children.Add(Meter(out _healthWord, out _healthBar).As(out _health));
            Children.Add(Meter(out _energyWord, out _energyBar).As(out _energy));
            Children.Add(Meter(out _oxygenWord, out _oxygenBar).As(out _oxygen));
            Children.Add(Meter(out _foodWord, out _foodBar).As(out _food));
        }

        /// <summary>A word and a bar beside it.</summary>
        static HStack Meter(out Label word, out ProgressBar bar)
        {
            return new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Label { Style = Styles.Aside, Align = Align.Center }.As(out word),
                    new ProgressBar { Style = Styles.Vital, Align = Align.Center, Placement = FixedSize.Size(96f, 8f) }.As(out bar)
                }
            };
        }
    }
}
