using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>
    /// How the player at the screen stands, at the editor's foot: health and suit energy always, oxygen where the world has it,
    /// food where it has hunger, each a bar that turns amber when low and blinks red when nearly gone.
    /// </summary>
    public partial class VitalsStrip : HStack
    {
        // Under a quarter a bar shows low; under a tenth it blinks.
        const float Low = 0.25f;
        const float Critical = 0.1f;

        // Which half of a blink is shown: flipped on each reading, so a bar nearly gone blinks at the pace it is read.
        bool _lit;

        /// <summary>A strip with nothing read yet, shown once there is a player to read.</summary>
        public VitalsStrip()
        {
            IsVisible = false;
            CreateView();
        }

        /// <summary>Shows how <paramref name="vitals" /> stand, in the words of <paramref name="texts" />; hidden where there is no player to read.</summary>
        /// <remarks>Called twice a second, never every frame: the bars move slowly, and reading them asks the game.</remarks>
        public void Show(IVitals vitals, Texts texts)
        {
            IsVisible = vitals.Present;
            if (!vitals.Present)
                return;

            _lit = !_lit;
            Meter(_health, _healthWord, _healthBar, texts.VitalsHealth, vitals.Health, true);
            Meter(_energy, _energyWord, _energyBar, texts.VitalsEnergy, vitals.Energy, true);
            Meter(_oxygen, _oxygenWord, _oxygenBar, texts.VitalsOxygen, vitals.Oxygen, vitals.HasOxygen);
            Meter(_food, _foodWord, _foodBar, texts.VitalsFood, vitals.Food, vitals.HasFood);
        }

        /// <summary>Shows <paramref name="share" /> on <paramref name="bar" />, called <paramref name="said" />, where <paramref name="shown" />, and <paramref name="meter" /> not at all otherwise.</summary>
        void Meter(HStack meter, Label word, ProgressBar bar, string said, float share, bool shown)
        {
            meter.IsVisible = shown;
            if (!shown)
                return;

            word.Text = said;
            bar.Value = share;
            var blinking = share < Critical && _lit;
            bar.Set(LookStates.Broken, blinking);
            bar.Set(LookStates.Warned, share < Low && !blinking);
        }
    }
}
