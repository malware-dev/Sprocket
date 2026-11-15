using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>Whether a step enabling or disabling a state enables it, disables it or toggles it, chosen in a menu under its verb.</summary>
    public static class SwitchQuestion
    {
        /// <summary>
        /// Offers enable and disable in a menu under the part pressed, then toggle where the switch <paramref name="flips" />,
        /// the one <paramref name="step" /> does marked; <paramref name="chosen" /> is given the value another one chosen gives
        /// the step, toggle as the word the selector gives it.
        /// </summary>
        public static void Offer(SprocketEditor editor, Act step, bool flips, Action<IList<Literal>> chosen)
        {
            var enables = step.Arguments[0].Flag;
            editor.AskHere("");
            foreach (var choice in new[] { true, false })
            {
                var now = choice;
                editor.Answer(now ? SentenceText.EnableWord : SentenceText.DisableWord, delegate
                {
                    if (now != enables)
                        chosen(new[] { Literal.Of(now) });
                }, now == enables);
            }

            if (flips)
                editor.Answer(EntityAction.Toggle, () => chosen(new[] { Literal.Of(EntityAction.Toggle) }), false);
        }
    }
}
