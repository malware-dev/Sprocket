using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System.Collections.Generic;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar
{
    /// <summary>What a slot chosen in the picker holds, under its row: its blocks one under another, and beside them the signals or screens they answer with.</summary>
    public partial class SlotHeldRow : HStack
    {
        /// <summary>A row holding nothing yet.</summary>
        public SlotHeldRow()
        {
            CreateView();
        }

        /// <summary>Says <paramref name="blocks" />, and beside them <paramref name="answers" />, or nothing beside them where there are none.</summary>
        public void Show(IList<Mention> blocks, IList<Mention> answers)
        {
            _blocks.Say(blocks);
            _answers.Say(answers);
            _answers.IsVisible = answers.Count != 0;
        }
    }
}
